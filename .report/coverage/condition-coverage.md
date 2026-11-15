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

## Solution — chỉ tính logic thuần (loại code phụ thuộc ngoài)

Logic thuần = điều kiện nằm trong code **không phụ thuộc** vào thứ ngoài code đó (không cần UI, input thật, game host, IO/audio/texture, database/online, hay DI của framework). Bị loại, theo thứ tự ưu tiên:

| Nhóm loại | Nhận diện (tự động bằng Roslyn) |
|---|---|
| TestInfra | type trong namespace `osu.Game.Tests` (scaffold test nằm trong osu.Game) |
| UI | type dẫn xuất từ `Drawable` (không qua `Component`); hoặc member tham chiếu type dẫn xuất từ `Drawable` (không qua `Component`) |
| Host/IO | type kế thừa `osu.Framework.Game`/`Platform`; hoặc member dùng `osu.Framework.Platform|IO|Audio|Threading`, texture/shader/render, `osu.Game.Skinning|Audio|IO` |
| Input | type kế thừa/triển khai `osu.Framework.Input.*`; hoặc member dùng `osu.Framework.Input`, `osu.Game.Input` |
| Database/Online | member dùng `Realms`, `osu.Game.Database`, `osu.Game.Online`, `System.Net` |
| DI | member có `[Resolved]`/`[BackgroundDependencyLoader]`/`[Cached]` hoặc dùng symbol có `[Resolved]` |

Việc loại làm theo **member** (không loại cả type nếu chỉ một số member phụ thuộc ngoài). Đây là heuristic dựa trên kiểu được tham chiếu, không phải phân tích luồng dữ liệu: member gọi gián tiếp qua method khác vẫn được coi là thuần nếu bản thân nó không nhắc tới type ngoài. Nếu cả target đo (vd. method dùng Realm) phụ thuộc ngoài thì phần logic thuần của target là 0 điều kiện; xem cột "toàn bộ" để đối chiếu.

| Project | Tổng điều kiện | UI | Input | Host/IO | Database/Online | DI | TestInfra | Logic thuần còn lại | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage (logic thuần) | Condition coverage (toàn bộ) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| [osu.Game](condition-coverage-osu.Game.md) | 12721 | 8947 | 144 | 668 | 1066 | 99 | 212 | 1585 | 2557/3170 | 1190 | 218 | 80.7% | 14.9% |
| [osu.Game.Rulesets.Catch](condition-coverage-osu.Game.Rulesets.Catch.md) | 472 | 307 | 0 | 14 | 0 | 0 | 0 | 151 | 286/302 | 136 | 1 | 94.7% | 37.4% |
| [osu.Game.Rulesets.Mania](condition-coverage-osu.Game.Rulesets.Mania.md) | 714 | 324 | 6 | 59 | 0 | 0 | 0 | 325 | 579/650 | 267 | 13 | 89.1% | 45.8% |
| [osu.Game.Rulesets.Osu](condition-coverage-osu.Game.Rulesets.Osu.md) | 1722 | 1082 | 43 | 11 | 0 | 11 | 0 | 575 | 801/1150 | 347 | 121 | 69.7% | 25.3% |
| [osu.Game.Rulesets.Taiko](condition-coverage-osu.Game.Rulesets.Taiko.md) | 576 | 314 | 0 | 19 | 0 | 0 | 0 | 243 | 439/486 | 209 | 13 | 90.3% | 39.8% |
| [osu.Game.Tournament](condition-coverage-osu.Game.Tournament.md) | 381 | 335 | 0 | 15 | 0 | 0 | 0 | 31 | 47/62 | 22 | 6 | 75.8% | 6.2% |
| **Tổng** | 16586 | 11309 | 193 | 786 | 1066 | 110 | 212 | 2910 | 4709/5820 | 2171 | 372 | 80.9% | 18.6% |
