# Branch coverage — Cấp Solution (`osu.sln`)

Công cụ: Coverlet (`coverlet.collector` 10.1.0, collector `XPlat Code Coverage`, định dạng Cobertura), cấu hình `coverlet.runsettings`.
Lệnh: `dotnet test osu.sln --settings coverlet.runsettings --collect:"XPlat Code Coverage"` (mỗi project test ra một `coverage.cobertura.xml`).
Bộ test: 6 project test (Tournament 303, Catch 337, Mania 377, Taiko 288, Osu 414, Game 2450), tất cả pass (đúng bộ test/commit của lần đo condition coverage).
Loại trừ (giống cả 4 cấp): assembly `*.Tests`, `osu.Framework*`, `osu.Game.Resources`, NUnit, Moq; file `*.g.cs`, `*.Designer.cs`, `obj/`; attribute `GeneratedCodeAttribute`, `ExcludeFromCodeCoverageAttribute`.

## Kết quả: từng project (package) và tổng solution

| Đối tượng | branches-covered | branches-valid | branch-rate | % |
|---|---|---|---|---|
| osu.Game | 5895 | 35986 | 0.1638 | 16.38% |
| osu.Game.Rulesets.Catch | 546 | 1379 | 0.3959 | 39.59% |
| osu.Game.Rulesets.Mania | 905 | 2138 | 0.4233 | 42.33% |
| osu.Game.Rulesets.Osu | 1200 | 4650 | 0.2581 | 25.81% |
| osu.Game.Rulesets.Taiko | 624 | 1610 | 0.3876 | 38.76% |
| osu.Game.Tournament | 324 | 1308 | 0.2477 | 24.77% |
| **Tổng solution** | 9494 | 47071 | 0.2017 | 20.17% |

## Ghi chú
- `branches-covered/branches-valid` cộng từ các `<line branch="True" condition-coverage="x% (c/v)">` trong method. Cobertura của Coverlet chỉ có thuộc tính `branch-rate` ở package nên solution được cộng dồn từ method của các project.
- Một assembly (ví dụ `osu.Game`) được nhiều project test đo; các file được gộp theo từng line, lấy số branch phủ lớn nhất giữa các lần chạy (không đếm trùng `branches-valid`).
- Script tạo báo cáo: `tools/branch_coverage_report.py`.

## Logic thuần (pure logic) — branch coverage

Dùng **cùng phân loại** với `condition-coverage.md` của nhánh này, không chỉnh để số đẹp hơn. Phiên bản phân loại: **4 nhóm (UI, Input, Host/IO, TestInfra; DI, Database/Online và type không dựng được bị ẩn như `--hide-skipped true`)**.
Phân loại do chính `tools/ConditionCoverage` xuất (`list-excluded`, dùng cùng `selfReason`/`reasonOfMember` với `instrument`): mỗi type/member bị loại là một span `file:dòng đầu–dòng cuối` (type loại cả type là một span). Mỗi dòng branch trong Cobertura được khớp **chỉ theo file + số dòng** (không theo tên method, vì lambda/async state machine sinh dòng riêng); dòng nằm trong span thì bị loại khỏi logic thuần; span type hẹp nhất thắng, rồi tới span member. Đã đối chiếu: với mọi điều kiện trong map `instrument`, nhóm tra từ span trùng 100% với `Exclusion`. Đây vẫn là heuristic theo type, không phải phân tích luồng dữ liệu.

Cùng bộ test và cùng commit với báo cáo Whole ở trên (không chạy lại test). Lệnh: `dotnet tools/ConditionCoverage/bin/Debug/net10.0/ConditionCoverage.dll list-excluded --project <csproj> --out <json>; python3 tools/branch_coverage_report.py <cobertura> solution x --exclude <json,...> --groups 4 --expect 9494/47071`

### Whole (toàn bộ, giữ nguyên số ở trên)

| Project | branches-covered | branches-valid | % |
|---|---|---|---|
| osu.Game | 5895 | 35986 | 16.38% |
| osu.Game.Rulesets.Catch | 546 | 1379 | 39.59% |
| osu.Game.Rulesets.Mania | 905 | 2138 | 42.33% |
| osu.Game.Rulesets.Osu | 1200 | 4650 | 25.81% |
| osu.Game.Rulesets.Taiko | 624 | 1610 | 38.76% |
| osu.Game.Tournament | 324 | 1308 | 24.77% |
| **Tổng** | 9494 | 47071 | 20.17% |

### Pure logic only (chỉ logic thuần)

| Project | branches-covered | branches-valid | % |
|---|---|---|---|
| osu.Game | 3823 | 4352 | 87.84% |
| osu.Game.Rulesets.Catch | 461 | 471 | 97.88% |
| osu.Game.Rulesets.Mania | 887 | 998 | 88.88% |
| osu.Game.Rulesets.Osu | 1105 | 1426 | 77.49% |
| osu.Game.Rulesets.Taiko | 622 | 698 | 89.11% |
| osu.Game.Tournament | 322 | 327 | 98.47% |
| **Tổng** | 7220 | 8272 | 87.28% |

### Branch bị loại theo nhóm (covered/valid)

| Project | UI | Input | Host/IO | TestInfra | Ngoài phạm vi (ẩn khỏi báo cáo condition) | Tổng bị loại |
|---|---|---|---|---|---|---|
| osu.Game | 80/23529 | 73/304 | 485/707 | 4/631 | 1430/6463 | 2072/31634 |
| osu.Game.Rulesets.Catch | 83/794 | 2/8 | 0/6 | 0/0 | 0/100 | 85/908 |
| osu.Game.Rulesets.Mania | 0/898 | 13/20 | 5/10 | 0/0 | 0/212 | 18/1140 |
| osu.Game.Rulesets.Osu | 95/2798 | 0/100 | 0/6 | 0/0 | 0/320 | 95/3224 |
| osu.Game.Rulesets.Taiko | 0/754 | 2/6 | 0/6 | 0/0 | 0/146 | 2/912 |
| osu.Game.Tournament | 0/921 | 0/0 | 0/4 | 0/0 | 2/56 | 2/981 |
| **Tổng** | 258/29694 | 90/438 | 490/739 | 4/631 | 1432/7297 | 2274/38799 |

Kiểm tra: whole = pure logic + tổng các nhóm bị loại (đúng cho từng project và tổng).


> Lưu ý so sánh: ở bản 4 nhóm, báo cáo condition **không đếm** các điều kiện thuộc nhóm ẩn (cột whole của condition đã trừ chúng, vd. 1617 thay vì 1722 ở project Osu), còn whole của branch vẫn tính đủ. Vì vậy chỉ so được **pure logic với pure logic**.

### So sánh branch với condition (cùng phân loại)

| Chỉ số | Whole | Pure logic |
|---|---|---|
| Branch coverage | 20.17% (9494/47071) | 87.28% (7220/8272) |
| Condition coverage | 18.5% (5304/28652 outcome) | 84.4% (4784/5670 outcome) |

### Vì sao `osu.Game.Rulesets.Osu` là 1241/4650 ở nhánh project nhưng 1200/4650 ở nhánh solution

Xác nhận từ repo: mã production (`osu.Game`, `osu.Game.Rulesets.Osu`) **giống hệt** giữa `testing-project` và `testing-solution` (`git diff` rỗng), nên `branches-valid` bằng nhau (4650). Khác biệt nằm ở **bộ test của project `osu.Game.Rulesets.Osu.Tests`**: nhánh project có 46 file test (commit `c22ea1352c` + `057b957e94`, chạy 775 test), nhánh solution có 39 file test do commit `159ae53b9f` viết lại (chạy 414 test, trộn với 5 project test khác). Ở cấp solution, các lần chạy được gộp theo từng dòng bằng cách lấy `covered` lớn nhất, nên project test khác chỉ có thể làm số Osu tăng chứ không giảm: số thấp hơn đến từ bộ test Osu nhỏ hơn/khác của nhánh solution. Phần "bộ test nào phủ dòng nào" không được truy vết thêm.

