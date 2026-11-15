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
| Host/IO | type kế thừa `osu.Framework.Game`/`Platform`; hoặc member dùng `osu.Framework.Platform|IO|Audio|Threading`, texture/shader/render, `osu.Game.Skinning|IO` |
| Input | type kế thừa/triển khai `osu.Framework.Input.*`; hoặc member dùng `osu.Framework.Input`, `osu.Game.Input` |
| Database/Online | member dùng `Realms`, `osu.Game.Database`, `osu.Game.Online`, `System.Net` |
| DI | **type** có member `[Resolved]`/`[BackgroundDependencyLoader]` (cần DI container mới dựng được) → loại cả type; hoặc member có `[Cached]` hoặc dùng symbol có `[Resolved]`; hoặc mọi constructor đều đòi tham số là type DI |
| Database/Online (mở rộng) | type dẫn xuất từ `Realms.*` (`RealmObject`) → loại cả type |
| Host/IO (mở rộng) | type mà **mọi** constructor tường minh đều đòi tham số là thứ ngoài (vd. `Storage`, `IAPIProvider`) → loại cả type. Type static, type không có constructor tường minh, hoặc có ít nhất một constructor nhận tham số thuần/interface nội bộ thì vẫn tính |

Việc loại làm theo **member**; riêng các trường hợp ghi "loại cả type" thì loại toàn bộ type (vì type đó không dựng/kiểm thử được bằng control-flow test thuần). Đây là heuristic dựa trên kiểu được tham chiếu, không phải phân tích luồng dữ liệu: member gọi gián tiếp qua method khác vẫn được coi là thuần nếu bản thân nó không nhắc tới type ngoài. Nếu cả target đo (vd. method dùng Realm) phụ thuộc ngoài thì phần logic thuần của target là 0 điều kiện; xem cột "toàn bộ" để đối chiếu.

| Project | Tổng điều kiện | UI | Input | Host/IO | Database/Online | DI | TestInfra | Logic thuần còn lại | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage (logic thuần) | Condition coverage (toàn bộ) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| [osu.Game](condition-coverage-osu.Game.md) | 12721 | 8752 | 120 | 853 | 609 | 707 | 212 | 1468 | 2524/2936 | 1185 | 129 | 86.0% | 14.9% |
| [osu.Game.Rulesets.Catch](condition-coverage-osu.Game.Rulesets.Catch.md) | 472 | 284 | 0 | 30 | 0 | 0 | 0 | 158 | 300/316 | 143 | 1 | 94.9% | 37.4% |
| [osu.Game.Rulesets.Mania](condition-coverage-osu.Game.Rulesets.Mania.md) | 714 | 308 | 6 | 30 | 0 | 0 | 0 | 370 | 644/740 | 294 | 20 | 87.0% | 45.8% |
| [osu.Game.Rulesets.Osu](condition-coverage-osu.Game.Rulesets.Osu.md) | 1722 | 1025 | 43 | 37 | 0 | 61 | 0 | 556 | 810/1112 | 350 | 96 | 72.8% | 25.3% |
| [osu.Game.Rulesets.Taiko](condition-coverage-osu.Game.Rulesets.Taiko.md) | 576 | 281 | 0 | 37 | 0 | 0 | 0 | 258 | 459/516 | 219 | 18 | 89.0% | 39.8% |
| [osu.Game.Tournament](condition-coverage-osu.Game.Tournament.md) | 381 | 335 | 0 | 6 | 0 | 15 | 0 | 25 | 47/50 | 22 | 0 | 94.0% | 6.2% |
| **Tổng** | 16586 | 10985 | 169 | 993 | 609 | 783 | 212 | 2835 | 4784/5670 | 2213 | 264 | 84.4% | 18.6% |
