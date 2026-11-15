# Condition coverage — RealmAccess.applyMigrationsForVersion (testing-method)

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
| [osu.Game](condition-coverage-osu.Game.md) | 48 | 95/96 | 47 | 0 | 99.0% |
| **Tổng** | 48 | 95/96 | 47 | 0 | 99.0% |

## Solution — sau khi loại bỏ code UI framework

Loại các điều kiện nằm trong type dẫn xuất từ `osu.Framework.Graphics.Drawable` (Drawable/Container/Screen/Overlay/Blueprint/HUD…, **không** tính type qua `Component` vì đó là logic không hiển thị), vì chúng cần scene graph, game host, GL/audio nên không unit test thuần được. Phần còn lại là code logic. Lưu ý: phần này vẫn còn code không phải UI nhưng khó unit test (skin transformer, truy cập Realm/mạng, cần game host); chúng chưa bị loại nên coverage của phần logic có thể thấp hơn thực tế.

| Project | Tổng điều kiện | Điều kiện UI (loại) | Điều kiện logic còn lại | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage (logic) | Condition coverage (toàn bộ) |
|---|---|---|---|---|---|---|---|---|
| [osu.Game](condition-coverage-osu.Game.md) | 48 | 0 | 48 | 95/96 | 47 | 0 | 99.0% | 99.0% |
| **Tổng** | 48 | 0 | 48 | 95/96 | 47 | 0 | 99.0% | 99.0% |
