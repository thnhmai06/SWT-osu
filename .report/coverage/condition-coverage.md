# Condition coverage — solution osu.sln (testing-solution)

## Phương pháp

- Công cụ: `tools/ConditionCoverage` (Roslyn). Mỗi *điều kiện nguyên tử* (toán hạng lá của cây `&&`, `||`, `!`, `&`, `|` trong `if`/`while`/`do`/`for`/`?:` hoặc biểu thức logic bất kỳ) được bọc probe ghi nhận kết quả `true`/`false`.
- **Condition coverage = số outcome đã quan sát / (2 × số điều kiện)**: mỗi điều kiện cần từng nhận `true` và từng nhận `false`.
- Điều kiện bị **short-circuit** (không được đánh giá) được tính là *chưa quan sát*, không tính là `false`.
- Không đo: điều kiện hằng, trong attribute/giá trị mặc định tham số, và trong lambda chuyển thành `Expression<T>` (giữ nguyên để LINQ provider dịch được).
- Branch coverage không nằm trong báo cáo này (dùng coverlet/dotCover); đây là chỉ số bổ sung ở mức điều kiện.
- Số file hits đã gộp: 12.

## Solution

| Project | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| [osu.Game](condition-coverage-osu.Game.md) | 10688 | 2921/21376 | 1371 | 9138 | 13.7% |
| [osu.Game.Rulesets.Catch](condition-coverage-osu.Game.Rulesets.Catch.md) | 442 | 353/884 | 167 | 256 | 39.9% |
| [osu.Game.Rulesets.Mania](condition-coverage-osu.Game.Rulesets.Mania.md) | 679 | 654/1358 | 298 | 323 | 48.2% |
| [osu.Game.Rulesets.Osu](condition-coverage-osu.Game.Rulesets.Osu.md) | 1617 | 870/3234 | 373 | 1120 | 26.9% |
| [osu.Game.Rulesets.Taiko](condition-coverage-osu.Game.Rulesets.Taiko.md) | 539 | 459/1078 | 219 | 299 | 42.6% |
| [osu.Game.Tournament](condition-coverage-osu.Game.Tournament.md) | 361 | 47/722 | 22 | 336 | 6.5% |
| **Tổng** | 14326 | 5304/28652 | 2450 | 11472 | 18.5% |

## Solution — chỉ tính logic thuần (loại code phụ thuộc ngoài)

Logic thuần = điều kiện nằm trong code **không phụ thuộc** vào thứ ngoài code đó (không cần UI, input thật, game host, IO/audio/texture). Bị loại, theo thứ tự ưu tiên:

| Nhóm loại | Nhận diện (tự động bằng Roslyn) |
|---|---|
| TestInfra | type trong namespace `osu.Game.Tests` (scaffold test nằm trong osu.Game) |
| UI | type dẫn xuất từ `Drawable` (không qua `Component`); hoặc member tham chiếu type dẫn xuất từ `Drawable` (không qua `Component`) |
| Host/IO | type kế thừa `osu.Framework.Game`/`Platform`; hoặc member dùng `osu.Framework.Platform|IO|Audio|Threading`, texture/shader/render, `osu.Game.Skinning|IO` |
| Input | type kế thừa/triển khai `osu.Framework.Input.*`; hoặc member dùng `osu.Framework.Input`, `osu.Game.Input` |

Việc loại làm theo **member** (type tự là UI/Input/Host/TestInfra thì loại cả type). Đây là heuristic dựa trên kiểu được tham chiếu, không phải phân tích luồng dữ liệu: member gọi gián tiếp qua method khác vẫn được coi là thuần nếu bản thân nó không nhắc tới type ngoài.

| Project | Tổng điều kiện | UI | Input | Host/IO | TestInfra | Logic thuần còn lại | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage (logic thuần) | Condition coverage (toàn bộ) |
|---|---|---|---|---|---|---|---|---|---|---|---|
| [osu.Game](condition-coverage-osu.Game.md) | 10688 | 8676 | 120 | 212 | 212 | 1468 | 2524/2936 | 1185 | 129 | 86.0% | 13.7% |
| [osu.Game.Rulesets.Catch](condition-coverage-osu.Game.Rulesets.Catch.md) | 442 | 284 | 0 | 0 | 0 | 158 | 300/316 | 143 | 1 | 94.9% | 39.9% |
| [osu.Game.Rulesets.Mania](condition-coverage-osu.Game.Rulesets.Mania.md) | 679 | 303 | 6 | 0 | 0 | 370 | 644/740 | 294 | 20 | 87.0% | 48.2% |
| [osu.Game.Rulesets.Osu](condition-coverage-osu.Game.Rulesets.Osu.md) | 1617 | 1018 | 43 | 0 | 0 | 556 | 810/1112 | 350 | 96 | 72.8% | 26.9% |
| [osu.Game.Rulesets.Taiko](condition-coverage-osu.Game.Rulesets.Taiko.md) | 539 | 281 | 0 | 0 | 0 | 258 | 459/516 | 219 | 18 | 89.0% | 42.6% |
| [osu.Game.Tournament](condition-coverage-osu.Game.Tournament.md) | 361 | 335 | 0 | 1 | 0 | 25 | 47/50 | 22 | 0 | 94.0% | 6.5% |
| **Tổng** | 14326 | 10897 | 169 | 213 | 212 | 2835 | 4784/5670 | 2213 | 264 | 84.4% | 18.5% |
