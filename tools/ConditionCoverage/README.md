# ConditionCoverage — công cụ đo Condition Coverage cho C#

Công cụ đo **condition coverage** (độ phủ điều kiện) cho unit test C#/.NET, tự viết bằng **Roslyn**.
Branch coverage đã có coverlet/dotCover; công cụ này bổ sung chỉ số ở mức *từng điều kiện nguyên tử*,
là chỉ số mà các tool .NET miễn phí không cung cấp (dotCover chỉ đo statement; coverlet đo nhánh IL).

## 1. Định nghĩa

- **Decision**: biểu thức boolean điều khiển luồng (`if`, `while`, `do-while`, `for`, `?:`) hoặc một cây `&&`/`||` ở bất kỳ vị trí nào (`return`, gán, đối số…).
- **Điều kiện nguyên tử (condition)**: toán hạng lá của cây `&&`, `||`, `!`, `&`, `|` trong một decision. Ví dụ `if (a > 0 && (b || !c))` có 3 điều kiện: `a > 0`, `b`, `c`.
- **Outcome**: mỗi điều kiện có 2 outcome, `true` và `false`.
- **Condition coverage** = `số outcome đã quan sát / (2 × số điều kiện)`.
- **Phủ đủ**: điều kiện đã từng nhận cả `true` lẫn `false`.
- Điều kiện bị **short-circuit** (không được đánh giá, ví dụ `b` trong `a && b` khi `a == false`) được tính là *chưa quan sát*, không phải `false`. Báo cáo gọi là "chưa được đánh giá" khi điều kiện chưa có outcome nào.

## 2. Cách hoạt động

```
mã nguồn ──(Roslyn rewriter)──▶ mã có probe ──build + chạy test──▶ hits-<asm>-<pid>.txt ──(report)──▶ Markdown
```

1. **Instrument** (`Rewriter.cs`, `Instrumenter.cs`). Mở project bằng `MSBuildWorkspace`, dùng *semantic model* để tìm decision và thay mỗi điều kiện lá `x` bằng `__CondCov_<asm>.C(id, x)`. Probe trả lại đúng giá trị nên **thứ tự đánh giá và short-circuit không đổi**. Các điều kiện chứa khai báo biến (`x is Foo f`, `TryGetValue(k, out var v)`) dùng dạng `((x) ? (T(id) || true) : (F(id) && false))` để compiler giữ được phân tích definite-assignment.
2. **Probe** (`ProbeSource.cs`). Mỗi assembly được sinh một class `internal static` riêng (`__CondCov_<assembly>`, tránh đụng tên qua `InternalsVisibleTo`) chứa hai mảng đếm true/false, cập nhật bằng `Interlocked` (an toàn khi NUnit chạy song song). Khi process kết thúc (`ProcessExit`), probe ghi `hits-<assembly>-<pid>.txt` vào thư mục `CONDCOV_DIR`.
3. **Report** (`Report.cs`). Ghép file map (id → vị trí, namespace, type, method, nội dung điều kiện) với số liệu hits, xuất Markdown theo cấp **solution → project → namespace → type → method**. Ở cấp method, mỗi điều kiện còn thiếu được liệt kê kèm `file:dòng`, nội dung và outcome thiếu.

### Không được đo

| Trường hợp | Lý do |
|---|---|
| Biểu thức hằng (`const`, `true && false`) | Không chạy lúc runtime |
| Attribute, giá trị mặc định của tham số, `enum` | Không chạy lúc runtime |
| Lambda chuyển thành `Expression<T>` (vd. Realm LINQ) | Cây biểu thức được provider dịch sang truy vấn, chèn lời gọi probe sẽ làm hỏng |
| Toán hạng kiểu `bool?` / `dynamic` | Không phải `System.Boolean` |
| Toán tử `&&`/`||` do người dùng định nghĩa | Hai vế không phải `bool` |

### Loại bỏ code UI framework

Mỗi điều kiện được gắn cờ `UiFramework` khi instrument: điều kiện nằm trong (hoặc lồng trong) type dẫn xuất từ `osu.Framework.Graphics.Drawable`
(Drawable, Container, Screen, Overlay, Blueprint, HUD…), **trừ** type đi qua `Component` (`Component` kế thừa `Drawable` nhưng không hiển thị, ví dụ `EditorBeatmap`). Đây là code cần scene graph, game host, GL/audio nên không viết unit test thuần được.
Báo cáo có thêm bảng **"sau khi loại bỏ code UI framework"** ở cấp solution (so sánh coverage toàn bộ và coverage phần logic) và ở cấp namespace của từng project;
bảng type đánh dấu `_(UI)_` cho type bị loại. Giới hạn: code không phải UI nhưng khó unit test (skin transformer, Realm, mạng, cần game host) chưa bị loại. Các bảng còn lại vẫn tính toàn bộ để đối chiếu.

## 3. Cách dùng

> **Luôn instrument trên một bản sao (git worktree), không instrument thư mục đang commit** — tool ghi đè các file `.cs`.

```bash
dotnet build tools/ConditionCoverage -c Debug
TOOL=tools/ConditionCoverage/bin/Debug/net10.0/ConditionCoverage.dll

git worktree add --detach /tmp/wt <branch>
cd /tmp/wt
dotnet restore <TestProject>.csproj

# 1) instrument project cần đo (lặp lại cho từng project; --include/--exclude là regex trên đường dẫn tương đối)
dotnet $TOOL instrument --project <Production>/<Production>.csproj --map /tmp/map-<Production>.json \
                        [--include 'Screens/Edit/EditorBeatmap\.cs']

# 2) build và chạy test, probe ghi số liệu vào CONDCOV_DIR
dotnet build <TestProject>.csproj -p:TreatWarningsAsErrors=false
CONDCOV_DIR=/tmp/hits dotnet test <TestProject>.csproj --no-build

# 3) báo cáo
dotnet $TOOL report --maps /tmp/map-A.json,/tmp/map-B.json --hits /tmp/hits --out .report/coverage \
                    --title "Condition coverage — ..." [--scope '<regex trên Namespace.Type.Member>']
```

Kết quả: `condition-coverage.md` (phương pháp + bảng solution/project) và `condition-coverage-<assembly>.md`
(bảng namespace, type, method và danh sách điều kiện còn thiếu cho từng project).

## 4. Kiểm chứng công cụ

`tools/ConditionCoverage.Tests` biên dịch và chạy mã đã instrument để kiểm tra:

- pattern variable / `out var` vẫn hợp lệ sau khi instrument (không phát sinh lỗi CS0165);
- short-circuit được giữ nguyên và outcome được đếm đúng (điều kiện bị bỏ qua không bị tính là `false`);
- lambda `Expression<T>`, hằng, `bool?`, giá trị mặc định tham số **không** bị instrument;
- điều kiện trong `while`/`for`/`?:`/getter/hàm lồng nhau được gán đúng namespace, type, method.

```bash
dotnet test tools/ConditionCoverage.Tests
```

## 5. Giới hạn

- Đây là condition coverage (từng điều kiện nhận đủ `true`/`false`), **không phải MC/DC** — không kiểm tra từng điều kiện độc lập ảnh hưởng đến kết quả decision.
- Mã sinh bởi source generator và file `*.g.cs`, `*.Designer.cs`, `obj/`, `bin/` bị bỏ qua.
- Số liệu chỉ phản ánh các test được chạy trong lần đo; thời gian chạy test tăng nhẹ do probe.
