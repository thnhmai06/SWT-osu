# Branch coverage — Cấp Method (`RealmAccess.applyMigrationsForVersion`)

Công cụ: Coverlet (`coverlet.collector` 10.1.0, collector `XPlat Code Coverage`, định dạng Cobertura), cấu hình `coverlet.runsettings`.
Lệnh: `dotnet test osu.Game.Tests/osu.Game.Tests.csproj --settings coverlet.runsettings --collect:"XPlat Code Coverage"`
Bộ test: 74 test, tất cả pass (đúng bộ test/commit của lần đo condition coverage).
Loại trừ (giống cả 4 cấp): assembly `*.Tests`, `osu.Framework*`, `osu.Game.Resources`, NUnit, Moq; file `*.g.cs`, `*.Designer.cs`, `obj/`; attribute `GeneratedCodeAttribute`, `ExcludeFromCodeCoverageAttribute`.

## Kết quả

| Đối tượng | branches-covered | branches-valid | branch-rate | % |
|---|---|---|---|---|
| osu.Game.Database.RealmAccess.applyMigrationsForVersion(Realms.Migration,System.UInt64) | 175 | 176 | 0.9943 | 99.43% |
| **Tổng method `applyMigrationsForVersion`** | 175 | 176 | 0.9943 | 99.43% |

## Ghi chú
- `branches-covered/branches-valid` cộng từ các `<line branch="True" condition-coverage="x% (c/v)">` trong method (khớp `branch-rate` ở mức method/class của file). Cobertura của Coverlet chỉ có thuộc tính `branch-rate` ở package nên các mức class/project/solution được cộng từ method.
- Script tạo báo cáo: `tools/branch_coverage_report.py`.

## Logic thuần (pure logic) — branch coverage

Dùng **cùng phân loại** với `condition-coverage.md` của nhánh này, không chỉnh để số đẹp hơn. Phiên bản phân loại: **8 nhóm (UI, Input, Host/IO, Database/Online, DI, TestInfra; không dùng `--hide-skipped`)**.
Phân loại do chính `tools/ConditionCoverage` xuất (`list-excluded`, dùng cùng `selfReason`/`reasonOfMember` với `instrument`): mỗi type/member bị loại là một span `file:dòng đầu–dòng cuối` (type loại cả type là một span). Mỗi dòng branch trong Cobertura được khớp **chỉ theo file + số dòng** (không theo tên method, vì lambda/async state machine sinh dòng riêng); dòng nằm trong span thì bị loại khỏi logic thuần; span type hẹp nhất thắng, rồi tới span member. Đã đối chiếu: với mọi điều kiện trong map `instrument`, nhóm tra từ span trùng 100% với `Exclusion`. Đây vẫn là heuristic theo type, không phải phân tích luồng dữ liệu.

Cùng bộ test và cùng commit với báo cáo Whole ở trên (không chạy lại test). Lệnh: `dotnet tools/ConditionCoverage/bin/Debug/net10.0/ConditionCoverage.dll list-excluded --project <csproj> --out <json>; python3 tools/branch_coverage_report.py <cobertura> method applyMigrationsForVersion --exclude <json,...> --groups 8 --expect 175/176`

### Whole (toàn bộ, giữ nguyên số ở trên)

| Project | branches-covered | branches-valid | % |
|---|---|---|---|
| osu.Game | 175 | 176 | 99.43% |
| **Tổng** | 175 | 176 | 99.43% |

### Pure logic only (chỉ logic thuần)

| Project | branches-covered | branches-valid | % |
|---|---|---|---|
| osu.Game | 0 | 0 | 0/0 (không có branch) |
| **Tổng** | 0 | 0 | 0/0 (không có branch) |

### Branch bị loại theo nhóm (covered/valid)

| Project | UI | Input | Host/IO | Database/Online | DI | TestInfra | Tổng bị loại |
|---|---|---|---|---|---|---|---|
| osu.Game | 0/0 | 0/0 | 175/176 | 0/0 | 0/0 | 0/0 | 175/176 |
| **Tổng** | 0/0 | 0/0 | 175/176 | 0/0 | 0/0 | 0/0 | 175/176 |

Kiểm tra: whole = pure logic + tổng các nhóm bị loại (đúng cho từng project và tổng).


### So sánh branch với condition (cùng phân loại)

| Chỉ số | Whole | Pure logic |
|---|---|---|
| Branch coverage | 99.43% (175/176) | 0/0 (không có branch) |
| Condition coverage | 99.0% (95/96 outcome) | 0/0 (0 điều kiện thuần) |

