# Condition coverage — osu.Game

Tổng: 48 | 95/96 | 47 | 0 | 99.0%

## Namespace

| Namespace | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| osu.Game.Database | 48 | 95/96 | 47 | 0 | 99.0% |

## Namespace — sau khi loại bỏ code UI framework

Loại 0 điều kiện trong type UI (dẫn xuất từ `osu.Framework.Graphics.Drawable`, không qua `Component`). Còn lại: 48 | 95/96 | 47 | 0 | 99.0%

| Namespace | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| osu.Game.Database | 48 | 95/96 | 47 | 0 | 99.0% |

## Type

| Type | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| osu.Game.Database.RealmAccess | 48 | 95/96 | 47 | 0 | 99.0% |

## Method

### osu.Game.Database.RealmAccess

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `applyMigrationsForVersion(Migration, ulong)` | 48 | 95/96 | 47 | 0 | 99.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`applyMigrationsForVersion(Migration, ulong)`**

- `Database/RealmAccess.cs:995` `legacyCollectionImporter.GetAvailableCount(storage).GetResultSafely() > 0` — thiếu `true`

</details>

