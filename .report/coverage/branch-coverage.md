# Branch coverage — Cấp Class (`EditorBeatmap`)

Công cụ: Coverlet (`coverlet.collector` 10.1.0, collector `XPlat Code Coverage`, định dạng Cobertura), cấu hình `coverlet.runsettings`.
Lệnh: `dotnet test osu.Game.Tests/osu.Game.Tests.csproj --settings coverlet.runsettings --collect:"XPlat Code Coverage"`
Bộ test: 72 test, tất cả pass (đúng bộ test/commit của lần đo condition coverage).
Loại trừ (giống cả 4 cấp): assembly `*.Tests`, `osu.Framework*`, `osu.Game.Resources`, NUnit, Moq; file `*.g.cs`, `*.Designer.cs`, `obj/`; attribute `GeneratedCodeAttribute`, `ExcludeFromCodeCoverageAttribute`.

## Kết quả

| Đối tượng | branches-covered | branches-valid | branch-rate | % |
|---|---|---|---|---|
| osu.Game.Screens.Edit.EditorBeatmap | 78 | 78 | 1.0000 | 100.00% |
| **Tổng class `EditorBeatmap` (gồm nested)** | 78 | 78 | 1.0000 | 100.00% |

## Ghi chú
- `branches-covered/branches-valid` cộng từ các `<line branch="True" condition-coverage="x% (c/v)">` trong method (khớp `branch-rate` ở mức method/class của file). Cobertura của Coverlet chỉ có thuộc tính `branch-rate` ở package nên các mức class/project/solution được cộng từ method.
- Script tạo báo cáo: `tools/branch_coverage_report.py`.
