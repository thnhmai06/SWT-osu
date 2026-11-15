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
