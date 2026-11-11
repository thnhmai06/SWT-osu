# Plan: Control-flow unit tests cho `RealmAccess.applyMigrationsForVersion` (Lv1)

## Context
- TARGET `RealmAssets.applyMigrationsForVersion (osu.Game)` không khớp class nào. User đã chọn resolve sang
  `private void RealmAccess.applyMigrationsForVersion(Migration migration, ulong targetVersion)`
  — `osu.Game/Database/RealmAccess.cs:835-1326`, project `osu.Game` (in scope). Không có candidate khác.
- Repo không còn test project (commit HEAD `1442c46184 remove unused projects` xoá `osu.Game.Tests`).
  User đã chọn: tạo project mới + test dùng Realm thật trong thư mục tạm.
- Không build/run (theo task_boundary). Không tạo `.testagent/` (focused scope). Báo cáo theo 6 mục user yêu cầu (thay cho bảng Requirement|Evidence của skill).

## Files tạo mới (không sửa production, không sửa osu.sln)
1. `osu.Game.Tests/osu.Game.Tests.csproj` — tối giản: import `..\osu.TestProject.props` (đã ref osu.Game + link VisualTestRunner),
   NUnit 4.6.1, NUnit3TestAdapter 6.3.0, Microsoft.NET.Test.Sdk 18.10.0 (cùng version với csproj cũ trong history),
   `OutputType WinExe`, `net10.0`, ProjectReference tới `osu.Game.Rulesets.Osu` (case 41 cần ruleset instantiable).
   Không thêm `GlobalAnalyzerConfigFiles` (file không tồn tại). Đường dẫn khớp `osu.Desktop.slnf` đang tham chiếu.
   `osu.sln` không liệt kê project → không sửa; báo cáo lệnh `dotnet sln add`.
2. `osu.Game.Tests/Database/RealmAccessMigrationTests.cs` — NUnit `[TestFixture]`, namespace `osu.Game.Tests.Database`, header license ppy.

## Harness (cô lập 1 case / test — tránh `onMigration` chạy chuỗi N+1..52)
- `[SetUp]`: temp dir `Path.GetTempPath()/osu-realm-migration-tests/<Guid>`, `new NativeStorage(dir)`,
  `realmAccess = new RealmAccess(storage, "client")` (chỉ để lấy instance + storage). `[TearDown]`: dispose, xoá dir.
- `migrate(ulong target, Action<Realm> seed, Action<Realm> verify, RealmSchema? oldSchema = null)`:
  1. Mở `migration.realm` với `SchemaVersion = target - 1` (+ `Schema = oldSchema, IsDynamic = true` nếu có), `realm.Write(seed)`, dispose.
  2. Mở lại với `SchemaVersion = target`, `MigrationCallback = (m, _) => applyMethod.Invoke(realmAccess, new object[] { m, target })`
     (`applyMethod` lấy bằng reflection `BindingFlags.NonPublic | Instance`; không nằm trong BannedSymbols).
  3. `verify(realm)`.
- Case 7/9/10/11 (nhánh TRUE của `TryFindObjectSchema`) dùng `ObjectSchema.Builder` dựng schema cũ có PK đúng
  (`Beatmap`: `ID` Guid PK + `OnlineID` int?; `BeatmapMetadata`: `Author` string; `RulesetSetting`: `RulesetID` int + Key/Value/Variant;
  `KeyBinding`: `ID` Guid PK, `RulesetID` int?, `Action` int, `KeyCombination` string, `Variant` int?). Nhánh FALSE: schema cũ chỉ có class dummy.
- Case 28/32: tạo `.osr` tối thiểu bằng `osu.Game.IO.Legacy.SerializationWriter`, ghi vào `storage` tại `files/` + `GetStoragePath()` của hash,
  score có `RealmNamedFileUsage(new RealmFile { Hash = h }, "replay.osr")`. Ghi file SAU khi tạo RealmAccess (Cleanup chạy trong ctor).

## Control-flow inventory → test (C2/C3)
| # | Decision / condition | Test(s) |
|---|---|---|
| S | `switch(targetVersion)`: 26 case nhãn + 29/30 chung + no-match | 1 test/case bên dưới; `Version12_NoMatchingCase_LeavesDataUnchanged`, `Version50_...` |
| 7 | guard `!TryFindObjectSchema` (local fn, chỉ return khỏi type đó) T/F; loop 0/≥1; `?? -1` null/non-null | `V7_OldSchemaWithoutModels_Skips`, `V7_NullOnlineID_BecomesMinusOne`+`V7_OnlineIDPreserved` (cùng seed 2 rows), `V7_ClassPresentNoRows_NoIteration` |
| 8 | ×2: `binding != null` T/F; `SequenceEqual(Ctrl+Plus / Ctrl+Minus)` T/F | `V8_NoBindings`, `V8_MatchingCombos_Removed`, `V8_OtherCombos_Kept` |
| 9 | guard T/F (return khỏi method); loop 0/≥1 | `V9_NoOldMetadataSchema_Skips`, `V9_OldAuthor_ConvertedToRealmUser`, `V9_NoRows` |
| 10 | guard T/F; loop 0/≥1; `IsNullOrEmpty(name)` T (ID 99→throw→null) / F (ID 0→"osu") | `V10_NoOldSchema`, `V10_KnownRulesetID_MapsName`, `V10_UnknownRulesetID_Removed` |
| 11 | guard T/F; loop; `RulesetID == null` T/F; `IsNullOrEmpty` T/F | `V11_NoOldSchema`, `V11_NullRulesetID_Skipped`, `V11_KnownID_MapsName`, `V11_UnknownID_Removed` |
| 14,20 | foreach 0/≥1 | `V14_SetsUserSettings`, `V14_NoBeatmaps`, `V20_ResetsStarRating`, `V20_NoBeatmaps` |
| 21 | `GetAvailableCount > 0` F (không có collection.db) | `V21_NoCollectionDb_NothingImported`; TRUE = not-unit-testable (fire-and-forget `ContinueWith` + `storage.Move`) |
| 25 | `RemoveRange(Where Protected)` | `V25_RemovesOnlyProtectedSkins` |
| 26 | foreach; `BeatmapInfo?.` null/non-null; `?? string.Empty` | `V26_CopiesBeatmapHash`, `V26_NullBeatmap_EmptyHash`; Hash==null infeasible (non-null string mặc định `string.Empty`) |
| 28 | foreach; lambda `version < FIRST_LAZER_VERSION(30000000)` T/F | `V28_LegacyReplayVersion_MarksLegacy`, `V28_LazerReplayVersion_NotLegacy`, `V28_NoReplayFile_Unchanged` |
| 29/30 | empty | `V29_NoChange`, `V30_NoChange` |
| 31 | `IsLegacyScore && IsLegacyRuleset()`: (T,T),(T,F),(F,–) | `V31_LegacyScoreLegacyRuleset`, `V31_LegacyScoreCustomRuleset`, `V31_NonLegacyScore` |
| 32 | `!IsLegacyScore \|\| !IsLegacyRuleset()`: (T,–),(F,T),(F,F); lambda `!HasFlag(ScoreV2) \|\| Any(SV2)`: (T,–),(F,T),(F,F) | `V32_NonLegacyScore_Skipped`, `V32_CustomRuleset_Skipped`, `V32_NoScoreV2Flag_Unchanged`, `V32_ScoreV2AlreadyPresent_Unchanged`, `V32_ScoreV2Flag_AddsModAndTotal` |
| 33,43,47 | `!= null` T/F; `SequenceEqual` T/F | 3 test mỗi case (none / match removed / mismatch kept) |
| 45,46 | ×2 decisions như 8 | 3 test mỗi case |
| 35 | `All(Shift)` T (2 Shift) / F; foreach category; `countCleared > 0` T/F | `V35_DoubleShiftDash_SecondBecomesLeftClick`, `V35_DistinctCatchDash_Unchanged`, `V35_DuplicateGlobalBindings_Cleared`, `V35_DuplicateRulesetBindings_Cleared`, `V35_NoDuplicates_NothingCleared` (mọi seed có ≥1 catch dash binding) |
| 36 | `OnlineID > 0` T/F | `V36_PositiveOnlineID_MovedToLegacy`, `V36_NonPositive_BothMinusOne` |
| 39 | `Total==0 && EndTime==0`: (T,T),(T,F),(F,–) | 3 test |
| 41 | try OK / catch | `V41_ValidRuleset_PopulatesTotalWithoutMods` (OsuRuleset, BeatmapInfo non-null), `V41_UnloadableRuleset_LoggedAndSkipped` (InstantiationInfo rác) |
| 42 | loops hằng (0-iteration infeasible); `columns%2==1 && column%columns==columns/2` cả 2 outcome luôn xảy ra; `oldKeyBinding != null` T/F | `V42_RemapsSpecialAndNormalActions` (variant 1 action 1→0, variant 2 action 10/11→0/1), `V42_NoManiaBindings_NothingAdded` |
| 48 | Where StatusInt==Qualified | `V48_QualifiedReset_OnlineIDKept`, `V48_RankedUntouched` |
| 49 | Where LegacyOnlineID==0 | `V49_ZeroBecomesMinusOne`, `V49_OtherValuesKept` |

## Infeasible / defect / not-unit-testable (ghi vào báo cáo)
- Case 35 `All(...)` trên list rỗng = vacuous TRUE → `Debug.Assert(Count==2)` + `.Last()` ném → **defect quan sát được**, không viết test khẳng định crash.
  All-Shift với Count≠2: chặn bởi Debug.Assert (invariant giả định).
- Case 42 loop 0 lần: infeasible (bound hằng 1..10). Case 26 `Hash == null`: infeasible. Case 10/11 `IsNullOrEmpty` với chuỗi rỗng: infeasible (ShortName chỉ trả 4 tên hoặc throw).
- Case 21 TRUE: not-unit-testable (async fire-and-forget, cần collection.db hợp lệ).
- Logger output (`countCleared>0` log, stopwatch log): không assert log, chỉ assert trạng thái DB.

## Rủi ro (chưa verify vì không được build/run)
- Realm chấp nhận đổi kiểu `Author` string→link, `OnlineID` int?→int, thêm/bỏ class trong migration (case 7/9/10/11).
- `RealmAccess` ctor với `NativeStorage` không có `HeadlessGameHost` (repo cũ dùng `CleanRunHeadlessGameHost`); nếu cần, bọc như `RealmTest` cũ.
- Exception trong MigrationCallback bị Realm wrap.

## Verification
Chỉ static self-review (đọc lại test vs inventory, namespace/usings/member names). Không build/run. User tự chạy:
`dotnet test osu.Game.Tests/osu.Game.Tests.csproj --filter FullyQualifiedName~RealmAccessMigrationTests`.
