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
