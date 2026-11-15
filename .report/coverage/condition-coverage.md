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
| [osu.Game](condition-coverage-osu.Game.md) | 12721 | 3801/25442 | 1776 | 10696 | 14.9% |
| [osu.Game.Rulesets.Catch](condition-coverage-osu.Game.Rulesets.Catch.md) | 472 | 353/944 | 167 | 286 | 37.4% |
| [osu.Game.Rulesets.Mania](condition-coverage-osu.Game.Rulesets.Mania.md) | 714 | 654/1428 | 298 | 358 | 45.8% |
| [osu.Game.Rulesets.Osu](condition-coverage-osu.Game.Rulesets.Osu.md) | 1722 | 870/3444 | 373 | 1225 | 25.3% |
| [osu.Game.Rulesets.Taiko](condition-coverage-osu.Game.Rulesets.Taiko.md) | 576 | 459/1152 | 219 | 336 | 39.8% |
| [osu.Game.Tournament](condition-coverage-osu.Game.Tournament.md) | 381 | 47/762 | 22 | 356 | 6.2% |
| **Tổng** | 16586 | 6184/33172 | 2855 | 13257 | 18.6% |

## Solution — sau khi loại bỏ code UI framework

Loại các điều kiện nằm trong type dẫn xuất từ `osu.Framework.Graphics.Drawable` (Drawable/Container/Screen/Overlay/Blueprint/HUD…, **không** tính type qua `Component` vì đó là logic không hiển thị), vì chúng cần scene graph, game host, GL/audio nên không unit test thuần được. Phần còn lại là code logic. Lưu ý: phần này vẫn còn code không phải UI nhưng khó unit test (skin transformer, truy cập Realm/mạng, cần game host); chúng chưa bị loại nên coverage của phần logic có thể thấp hơn thực tế.

| Project | Tổng điều kiện | Điều kiện UI (loại) | Điều kiện logic còn lại | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage (logic) | Condition coverage (toàn bộ) |
|---|---|---|---|---|---|---|---|---|
| [osu.Game](condition-coverage-osu.Game.md) | 12721 | 8799 | 3922 | 3797/7844 | 1774 | 1899 | 48.4% | 14.9% |
| [osu.Game.Rulesets.Catch](condition-coverage-osu.Game.Rulesets.Catch.md) | 472 | 244 | 228 | 353/456 | 167 | 42 | 77.4% | 37.4% |
| [osu.Game.Rulesets.Mania](condition-coverage-osu.Game.Rulesets.Mania.md) | 714 | 302 | 412 | 654/824 | 298 | 56 | 79.4% | 45.8% |
| [osu.Game.Rulesets.Osu](condition-coverage-osu.Game.Rulesets.Osu.md) | 1722 | 883 | 839 | 870/1678 | 373 | 342 | 51.8% | 25.3% |
| [osu.Game.Rulesets.Taiko](condition-coverage-osu.Game.Rulesets.Taiko.md) | 576 | 264 | 312 | 459/624 | 219 | 72 | 73.6% | 39.8% |
| [osu.Game.Tournament](condition-coverage-osu.Game.Tournament.md) | 381 | 335 | 46 | 47/92 | 22 | 21 | 51.1% | 6.2% |
| **Tổng** | 16586 | 10827 | 5759 | 6180/11518 | 2853 | 2432 | 53.7% | 18.6% |
