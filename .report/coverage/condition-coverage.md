# Condition coverage — osu.Game.Rulesets.Osu (testing-project)

## Phương pháp

- Công cụ: `tools/ConditionCoverage` (Roslyn). Mỗi *điều kiện nguyên tử* (toán hạng lá của cây `&&`, `||`, `!`, `&`, `|` trong `if`/`while`/`do`/`for`/`?:` hoặc biểu thức logic bất kỳ) được bọc probe ghi nhận kết quả `true`/`false`.
- **Condition coverage = số outcome đã quan sát / (2 × số điều kiện)**: mỗi điều kiện cần từng nhận `true` và từng nhận `false`.
- Điều kiện bị **short-circuit** (không được đánh giá) được tính là *chưa quan sát*, không tính là `false`.
- Không đo: điều kiện hằng, trong attribute/giá trị mặc định tham số, và trong lambda chuyển thành `Expression<T>` (giữ nguyên để LINQ provider dịch được).
- Branch coverage không nằm trong báo cáo này (dùng coverlet/dotCover); đây là chỉ số bổ sung ở mức điều kiện.
- Số file hits đã gộp: 1.

## Solution

| Project | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| [osu.Game.Rulesets.Osu](condition-coverage-osu.Game.Rulesets.Osu.md) | 1722 | 896/3444 | 400 | 1226 | 26.0% |
| **Tổng** | 1722 | 896/3444 | 400 | 1226 | 26.0% |

## Solution — chỉ tính logic thuần (loại code phụ thuộc ngoài)

Logic thuần = điều kiện nằm trong code **không phụ thuộc** vào thứ ngoài code đó (không cần UI, input thật, game host, IO/audio/texture, database/online, hay DI của framework). Bị loại, theo thứ tự ưu tiên:

| Nhóm loại | Nhận diện (tự động bằng Roslyn) |
|---|---|
| TestInfra | type trong namespace `osu.Game.Tests` (scaffold test nằm trong osu.Game) |
| UI | type dẫn xuất từ `Drawable` (không qua `Component`); hoặc member tham chiếu type dẫn xuất từ `Drawable` (không qua `Component`) |
| Host/IO | type kế thừa `osu.Framework.Game`/`Platform`; hoặc member dùng `osu.Framework.Platform|IO|Audio|Threading`, texture/shader/render, `osu.Game.Skinning|IO` |
| Input | type kế thừa/triển khai `osu.Framework.Input.*`; hoặc member dùng `osu.Framework.Input`, `osu.Game.Input` |
| Database/Online | member dùng `Realms`, `osu.Game.Database`, `osu.Game.Online`, `System.Net` |
| DI | **type** có member `[Resolved]`/`[BackgroundDependencyLoader]` (cần DI container mới dựng được) → loại cả type; hoặc member có `[Cached]` hoặc dùng symbol có `[Resolved]`; hoặc mọi constructor đều đòi tham số là type DI |
| Database/Online (mở rộng) | type dẫn xuất từ `Realms.*` (`RealmObject`) → loại cả type |
| Host/IO (mở rộng) | type mà **mọi** constructor tường minh đều đòi tham số là thứ ngoài (vd. `Storage`, `IAPIProvider`) → loại cả type. Type static, type không có constructor tường minh, hoặc có ít nhất một constructor nhận tham số thuần/interface nội bộ thì vẫn tính |

Việc loại làm theo **member**; riêng các trường hợp ghi "loại cả type" thì loại toàn bộ type (vì type đó không dựng/kiểm thử được bằng control-flow test thuần). Đây là heuristic dựa trên kiểu được tham chiếu, không phải phân tích luồng dữ liệu: member gọi gián tiếp qua method khác vẫn được coi là thuần nếu bản thân nó không nhắc tới type ngoài. Nếu cả target đo (vd. method dùng Realm) phụ thuộc ngoài thì phần logic thuần của target là 0 điều kiện; xem cột "toàn bộ" để đối chiếu.

| Project | Tổng điều kiện | UI | Input | Host/IO | Database/Online | DI | TestInfra | Logic thuần còn lại | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage (logic thuần) | Condition coverage (toàn bộ) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| [osu.Game.Rulesets.Osu](condition-coverage-osu.Game.Rulesets.Osu.md) | 1722 | 1025 | 43 | 37 | 0 | 61 | 0 | 556 | 825/1112 | 366 | 97 | 74.2% | 26.0% |
| **Tổng** | 1722 | 1025 | 43 | 37 | 0 | 61 | 0 | 556 | 825/1112 | 366 | 97 | 74.2% | 26.0% |
