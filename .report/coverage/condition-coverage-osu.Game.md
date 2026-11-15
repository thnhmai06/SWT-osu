# Condition coverage — osu.Game

Tổng: 48 | 95/96 | 47 | 0 | 99.0%

## Namespace

| Namespace | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| osu.Game.Database | 48 | 95/96 | 47 | 0 | 99.0% |

## Namespace — chỉ tính logic thuần

Loại 48 điều kiện phụ thuộc ngoài (UI: 0, Input: 0, Host/IO: 0, Database/Online: 48, DI: 0, TestInfra: 0). Còn lại: 0 | 0/0 | 0 | 0 | 0.0%

| Namespace | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|

## Type

| Type | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| osu.Game.Database.RealmAccess _(loại)_ | 48 | 95/96 | 47 | 0 | 99.0% |

## Method

### osu.Game.Database.RealmAccess

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `applyMigrationsForVersion(Migration, ulong)` | 48 | 95/96 | 47 | 0 | 99.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`applyMigrationsForVersion(Migration, ulong)`**

- `Database/RealmAccess.cs:995` `legacyCollectionImporter.GetAvailableCount(storage).GetResultSafely() > 0` — thiếu `true`

</details>

