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
| [osu.Game.Rulesets.Osu](condition-coverage-osu.Game.Rulesets.Osu.md) | 1617 | 896/3234 | 400 | 1121 | 27.7% |
| **Tổng** | 1617 | 896/3234 | 400 | 1121 | 27.7% |

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
| [osu.Game.Rulesets.Osu](condition-coverage-osu.Game.Rulesets.Osu.md) | 1617 | 1018 | 43 | 0 | 0 | 556 | 825/1112 | 366 | 97 | 74.2% | 27.7% |
| **Tổng** | 1617 | 1018 | 43 | 0 | 0 | 556 | 825/1112 | 366 | 97 | 74.2% | 27.7% |
