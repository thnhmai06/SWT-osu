# Branch coverage — cấp Project (`osu.Game.Rulesets.Osu`)

Công cụ: Coverlet (`coverlet.collector` 10.1.0, collector `XPlat Code Coverage`, định dạng Cobertura), cấu hình `coverlet.runsettings`.
Lệnh: `dotnet test osu.Game.Rulesets.Osu.Tests/osu.Game.Rulesets.Osu.Tests.csproj --settings coverlet.runsettings --collect:"XPlat Code Coverage"`
Bộ test: 775 test, tất cả pass (đúng bộ test/commit của lần đo condition coverage).
Loại trừ (giống cả 4 cấp): assembly `*.Tests`, `osu.Framework*`, `osu.Game.Resources`, NUnit, Moq; file `*.g.cs`, `*.Designer.cs`, `obj/`; attribute `GeneratedCodeAttribute`, `ExcludeFromCodeCoverageAttribute`.

## Kết quả cấp Project

| Đối tượng | branches-covered | branches-valid | branch-rate | % |
|---|---|---|---|---|
| **osu.Game.Rulesets.Osu** | 1241 | 4650 | 0.2669 | 26.69% |
| _branch-rate trong file (package)_ | | | 0.25989999999999996 | |

## Ghi chú
- `branches-covered/branches-valid` cộng từ các `<line branch="True" condition-coverage="x% (c/v)">` trong method (khớp `branch-rate` ở mức method/class của file). Cobertura của Coverlet chỉ có thuộc tính `branch-rate` ở package, giá trị trong file hơi khác (do Coverlet gộp class) nên liệt kê kèm.
- Script tạo báo cáo: `tools/branch_coverage_report.py`.
- Assembly `osu.Game` bị Coverlet đo kèm (tham chiếu bởi project test) nhưng nằm ngoài phạm vi cấp này.

## Logic thuần (pure logic) — branch coverage

Dùng **cùng phân loại** với `condition-coverage.md` của nhánh này, không chỉnh để số đẹp hơn. Phiên bản phân loại: **4 nhóm (UI, Input, Host/IO, TestInfra; DI, Database/Online và type không dựng được bị ẩn như `--hide-skipped true`)**.
Phân loại do chính `tools/ConditionCoverage` xuất (`list-excluded`, dùng cùng `selfReason`/`reasonOfMember` với `instrument`): mỗi type/member bị loại là một span `file:dòng đầu–dòng cuối` (type loại cả type là một span). Mỗi dòng branch trong Cobertura được khớp **chỉ theo file + số dòng** (không theo tên method, vì lambda/async state machine sinh dòng riêng); dòng nằm trong span thì bị loại khỏi logic thuần; span type hẹp nhất thắng, rồi tới span member. Đã đối chiếu: với mọi điều kiện trong map `instrument`, nhóm tra từ span trùng 100% với `Exclusion`. Đây vẫn là heuristic theo type, không phải phân tích luồng dữ liệu.

Cùng bộ test và cùng commit với báo cáo Whole ở trên (không chạy lại test). Lệnh: `dotnet tools/ConditionCoverage/bin/Debug/net10.0/ConditionCoverage.dll list-excluded --project <csproj> --out <json>; python3 tools/branch_coverage_report.py <cobertura> project osu.Game.Rulesets.Osu --exclude <json,...> --groups 4 --expect 1241/4650`

### Whole (toàn bộ, giữ nguyên số ở trên)

| Project | branches-covered | branches-valid | % |
|---|---|---|---|
| osu.Game.Rulesets.Osu | 1241 | 4650 | 26.69% |
| **Tổng** | 1241 | 4650 | 26.69% |

### Pure logic only (chỉ logic thuần)

| Project | branches-covered | branches-valid | % |
|---|---|---|---|
| osu.Game.Rulesets.Osu | 1131 | 1426 | 79.31% |
| **Tổng** | 1131 | 1426 | 79.31% |

### Branch bị loại theo nhóm (covered/valid)

| Project | UI | Input | Host/IO | TestInfra | Ngoài phạm vi (ẩn khỏi báo cáo condition) | Tổng bị loại |
|---|---|---|---|---|---|---|
| osu.Game.Rulesets.Osu | 110/2798 | 0/100 | 0/6 | 0/0 | 0/320 | 110/3224 |
| **Tổng** | 110/2798 | 0/100 | 0/6 | 0/0 | 0/320 | 110/3224 |

Kiểm tra: whole = pure logic + tổng các nhóm bị loại (đúng cho từng project và tổng).


> Lưu ý so sánh: ở bản 4 nhóm, báo cáo condition **không đếm** các điều kiện thuộc nhóm ẩn (cột whole của condition đã trừ chúng, vd. 1617 thay vì 1722 ở project Osu), còn whole của branch vẫn tính đủ. Vì vậy chỉ so được **pure logic với pure logic**.

### So sánh branch với condition (cùng phân loại)

| Chỉ số | Whole | Pure logic |
|---|---|---|
| Branch coverage | 26.69% (1241/4650) | 79.31% (1131/1426) |
| Condition coverage | 27.7% (896/3234 outcome) | 74.2% (825/1112 outcome) |

### Vì sao `osu.Game.Rulesets.Osu` là 1241/4650 ở nhánh project nhưng 1200/4650 ở nhánh solution

Xác nhận từ repo: mã production (`osu.Game`, `osu.Game.Rulesets.Osu`) **giống hệt** giữa `testing-project` và `testing-solution` (`git diff` rỗng), nên `branches-valid` bằng nhau (4650). Khác biệt nằm ở **bộ test của project `osu.Game.Rulesets.Osu.Tests`**: nhánh project có 46 file test (commit `c22ea1352c` + `057b957e94`, chạy 775 test), nhánh solution có 39 file test do commit `159ae53b9f` viết lại (chạy 414 test, trộn với 5 project test khác). Ở cấp solution, các lần chạy được gộp theo từng dòng bằng cách lấy `covered` lớn nhất, nên project test khác chỉ có thể làm số Osu tăng chứ không giảm: số thấp hơn đến từ bộ test Osu nhỏ hơn/khác của nhánh solution. Phần "bộ test nào phủ dòng nào" không được truy vết thêm.

