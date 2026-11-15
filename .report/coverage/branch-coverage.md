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
- Script tạo báo cáo: `.report/coverage/branch-coverage.py`.
- Assembly `osu.Game` bị Coverlet đo kèm (tham chiếu bởi project test) nhưng nằm ngoài phạm vi cấp này.
