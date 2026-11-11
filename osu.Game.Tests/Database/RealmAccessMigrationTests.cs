// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using osu.Framework.Input.Bindings;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Extensions;
using osu.Game.Input.Bindings;
using osu.Game.IO.Legacy;
using osu.Game.Models;
using osu.Game.Online.API;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Scoring;
using osu.Game.Skinning;
using Realms;
using Realms.Schema;

namespace osu.Game.Tests.Database
{
    /// <summary>
    /// Control-flow tests for <c>RealmAccess.applyMigrationsForVersion</c>.
    /// Each test seeds a realm file at schema version (N - 1), reopens it at version N and runs exactly one migration case through reflection,
    /// so that every <c>case</c> of the switch (and the implicit no-match path) is exercised in isolation.
    /// </summary>
    [TestFixture]
    public class RealmAccessMigrationTests
    {
        private static readonly MethodInfo apply_migrations_method =
            typeof(RealmAccess).GetMethod("applyMigrationsForVersion", BindingFlags.NonPublic | BindingFlags.Instance)!;

        #region No-match path of the switch

        [Test]
        public void TestVersion12_NoMatchingCase_LeavesDataUntouched() =>
            migrate(12, (r, _) => r.Add(createBeatmap(), true), (r, _) => Assert.That(r.All<BeatmapInfo>().Single().StarRating, Is.EqualTo(-1)));

        [Test]
        public void TestVersion50_BeyondLastCase_LeavesDataUntouched() =>
            migrate(50, (r, _) => r.Add(createBeatmap(b => b.StarRating = 4), true), (r, _) => Assert.That(r.All<BeatmapInfo>().Single().StarRating, Is.EqualTo(4)));

        #endregion

        #region Case 7: online ID conversion (local function guard, loop, null-coalescing)

        [Test]
        public void TestVersion7_ClassMissingInOldSchema_Skipped() =>
            migrate(7, (r, _) => createDynamic(r, "Unrelated"), (r, _) => Assert.That(r.All<BeatmapInfo>().Count(), Is.Zero),
                schemaOf(objectSchema("Unrelated", Property.Primitive("Value", RealmValueType.Int, isNullable: true))));

        [Test]
        public void TestVersion7_ClassPresentWithoutRows_NoIteration() =>
            migrate(7, (_, _) => { }, (r, _) => Assert.That(r.All<BeatmapInfo>().Count(), Is.Zero), beatmapSchemaWithNullableOnlineId());

        [Test]
        public void TestVersion7_NullOnlineId_BecomesMinusOneAndValueIsKept()
        {
            Guid nullId = Guid.NewGuid();
            Guid valueId = Guid.NewGuid();

            migrate(7, (r, _) =>
            {
                createDynamic(r, "Beatmap", nullId).DynamicApi.Set("OnlineID", RealmValue.Null);
                createDynamic(r, "Beatmap", valueId).DynamicApi.Set("OnlineID", 5L);
            }, (r, _) =>
            {
                var byId = r.All<BeatmapInfo>().ToDictionary(b => b.ID, b => b.OnlineID);
                Assert.That(byId[nullId], Is.EqualTo(-1));
                Assert.That(byId[valueId], Is.EqualTo(5));
            }, beatmapSchemaWithNullableOnlineId());
        }

        #endregion

        #region Case 8 / 33 / 43 / 45 / 46 / 47: key binding removal (null check && SequenceEqual)

        [Test]
        public void TestVersion8_NoBindings_NothingHappens() => assertNoBindings(8);

        [Test]
        public void TestVersion8_BindingsMatchObsoleteDefaults_Removed() => assertMatchingBindingsRemoved(8,
            (GlobalAction.IncreaseScrollSpeed, new[] { InputKey.Control, InputKey.Plus }),
            (GlobalAction.DecreaseScrollSpeed, new[] { InputKey.Control, InputKey.Minus }));

        [Test]
        public void TestVersion8_BindingsDifferFromObsoleteDefaults_Kept() => assertMismatchingBindingsKept(8, GlobalAction.IncreaseScrollSpeed, GlobalAction.DecreaseScrollSpeed);

        [Test]
        public void TestVersion33_NoBindings_NothingHappens() => assertNoBindings(33);

        [Test]
        public void TestVersion33_ChatFocusBoundToTab_Removed() => assertMatchingBindingsRemoved(33, (GlobalAction.ToggleChatFocus, new[] { InputKey.Tab }));

        [Test]
        public void TestVersion33_ChatFocusBoundToOtherKey_Kept() => assertMismatchingBindingsKept(33, GlobalAction.ToggleChatFocus);

        [Test]
        public void TestVersion43_NoBindings_NothingHappens() => assertNoBindings(43);

        [Test]
        public void TestVersion43_FpsToggleBoundToConflictingCombination_Removed() =>
            assertMatchingBindingsRemoved(43, (GlobalAction.ToggleFPSDisplay, new[] { InputKey.Shift, InputKey.Control, InputKey.F }));

        [Test]
        public void TestVersion43_FpsToggleBoundToOtherKey_Kept() => assertMismatchingBindingsKept(43, GlobalAction.ToggleFPSDisplay);

        [Test]
        public void TestVersion45_NoBindings_NothingHappens() => assertNoBindings(45);

        [Test]
        public void TestVersion45_BeatSnapCyclingWithShift_Removed() => assertMatchingBindingsRemoved(45,
            (GlobalAction.EditorCycleNextBeatSnapDivisor, new[] { InputKey.Shift, InputKey.Control, InputKey.MouseWheelLeft }),
            (GlobalAction.EditorCyclePreviousBeatSnapDivisor, new[] { InputKey.Shift, InputKey.Control, InputKey.MouseWheelRight }));

        [Test]
        public void TestVersion45_BeatSnapCyclingWithOtherKeys_Kept() =>
            assertMismatchingBindingsKept(45, GlobalAction.EditorCycleNextBeatSnapDivisor, GlobalAction.EditorCyclePreviousBeatSnapDivisor);

        [Test]
        public void TestVersion46_NoBindings_NothingHappens() => assertNoBindings(46);

        [Test]
        public void TestVersion46_BeatSnapCyclingWithWrongDirection_Removed() => assertMatchingBindingsRemoved(46,
            (GlobalAction.EditorCycleNextBeatSnapDivisor, new[] { InputKey.Control, InputKey.MouseWheelDown }),
            (GlobalAction.EditorCyclePreviousBeatSnapDivisor, new[] { InputKey.Control, InputKey.MouseWheelUp }));

        [Test]
        public void TestVersion46_BeatSnapCyclingWithOtherKeys_Kept() =>
            assertMismatchingBindingsKept(46, GlobalAction.EditorCycleNextBeatSnapDivisor, GlobalAction.EditorCyclePreviousBeatSnapDivisor);

        [Test]
        public void TestVersion47_NoBindings_NothingHappens() => assertNoBindings(47);

        [Test]
        public void TestVersion47_AbsoluteScrollBoundToRightMouse_Removed() =>
            assertMatchingBindingsRemoved(47, (GlobalAction.AbsoluteScrollSongList, new[] { InputKey.MouseRight }));

        [Test]
        public void TestVersion47_AbsoluteScrollBoundToOtherKey_Kept() => assertMismatchingBindingsKept(47, GlobalAction.AbsoluteScrollSongList);

        #endregion

        #region Case 9: author conversion (guard that exits the method, loop)

        [Test]
        public void TestVersion9_MetadataClassMissingInOldSchema_Skipped() =>
            migrate(9, (r, _) => createDynamic(r, "Unrelated"), (r, _) => Assert.That(r.All<BeatmapMetadata>().Count(), Is.Zero),
                schemaOf(objectSchema("Unrelated", Property.Primitive("Value", RealmValueType.Int, isNullable: true))));

        [Test]
        public void TestVersion9_MetadataClassWithoutRows_NoIteration() =>
            migrate(9, (_, _) => { }, (r, _) => Assert.That(r.All<BeatmapMetadata>().Count(), Is.Zero), metadataSchema());

        [Test]
        public void TestVersion9_OldAuthorString_ConvertedToUser() =>
            migrate(9, (r, _) => createDynamic(r, "BeatmapMetadata").DynamicApi.Set("Author", "peppy"),
                (r, _) => Assert.That(r.All<BeatmapMetadata>().Single().Author.Username, Is.EqualTo("peppy")), metadataSchema());

        #endregion

        #region Case 10: ruleset settings (guard, loop, string.IsNullOrEmpty both ways)

        [Test]
        public void TestVersion10_SettingClassMissingInOldSchema_Skipped() =>
            migrate(10, (r, _) => createDynamic(r, "Unrelated"), (r, _) => Assert.That(r.All<RealmRulesetSetting>().Count(), Is.Zero),
                schemaOf(objectSchema("Unrelated", Property.Primitive("Value", RealmValueType.Int, isNullable: true))));

        [Test]
        public void TestVersion10_SettingClassWithoutRows_NoIteration() =>
            migrate(10, (_, _) => { }, (r, _) => Assert.That(r.All<RealmRulesetSetting>().Count(), Is.Zero), rulesetSettingSchema());

        [Test]
        public void TestVersion10_KnownLegacyRulesetId_MappedToShortName() =>
            migrate(10, (r, _) => createDynamic(r, "RulesetSetting").DynamicApi.Set("RulesetID", 0L),
                (r, _) => Assert.That(r.All<RealmRulesetSetting>().Single().RulesetName, Is.EqualTo("osu")), rulesetSettingSchema());

        [Test]
        public void TestVersion10_UnknownLegacyRulesetId_SettingRemoved() =>
            migrate(10, (r, _) => createDynamic(r, "RulesetSetting").DynamicApi.Set("RulesetID", 99L),
                (r, _) => Assert.That(r.All<RealmRulesetSetting>().Count(), Is.Zero), rulesetSettingSchema());

        #endregion

        #region Case 11: ruleset key bindings (guard, loop, null ruleset id, IsNullOrEmpty)

        [Test]
        public void TestVersion11_KeyBindingClassMissingInOldSchema_Skipped() =>
            migrate(11, (r, _) => createDynamic(r, "Unrelated"), (r, _) => Assert.That(r.All<RealmKeyBinding>().Count(), Is.Zero),
                schemaOf(objectSchema("Unrelated", Property.Primitive("Value", RealmValueType.Int, isNullable: true))));

        [Test]
        public void TestVersion11_KeyBindingClassWithoutRows_NoIteration() =>
            migrate(11, (_, _) => { }, (r, _) => Assert.That(r.All<RealmKeyBinding>().Count(), Is.Zero), keyBindingSchema());

        [Test]
        public void TestVersion11_NullRulesetId_BindingLeftAsGlobal()
        {
            migrate(11, (r, _) => createOldKeyBinding(r, RealmValue.Null),
                (r, _) =>
                {
                    var binding = r.All<RealmKeyBinding>().Single();
                    Assert.That(binding.RulesetName, Is.Null);
                }, keyBindingSchema());
        }

        [Test]
        public void TestVersion11_KnownLegacyRulesetId_MappedToShortName() =>
            migrate(11, (r, _) => createOldKeyBinding(r, 1L), (r, _) => Assert.That(r.All<RealmKeyBinding>().Single().RulesetName, Is.EqualTo("taiko")), keyBindingSchema());

        [Test]
        public void TestVersion11_UnknownLegacyRulesetId_BindingRemoved() =>
            migrate(11, (r, _) => createOldKeyBinding(r, 99L), (r, _) => Assert.That(r.All<RealmKeyBinding>().Count(), Is.Zero), keyBindingSchema());

        #endregion

        #region Case 14 / 20: loops over beatmaps

        [Test]
        public void TestVersion14_NoBeatmaps_LoopNotEntered() => migrate(14, (_, _) => { }, (r, _) => Assert.That(r.All<BeatmapInfo>().Count(), Is.Zero));

        [Test]
        public void TestVersion14_Beatmap_UserSettingsReplaced() =>
            migrate(14, (r, _) => r.Add(createBeatmap(b => b.UserSettings = null!), true), (r, _) => Assert.That(r.All<BeatmapInfo>().Single().UserSettings, Is.Not.Null));

        [Test]
        public void TestVersion20_NoBeatmaps_LoopNotEntered() => migrate(20, (_, _) => { }, (r, _) => Assert.That(r.All<BeatmapInfo>().Count(), Is.Zero));

        [Test]
        public void TestVersion20_Beatmap_StarRatingReset() =>
            migrate(20, (r, _) => r.Add(createBeatmap(b => b.StarRating = 5.5), true), (r, _) => Assert.That(r.All<BeatmapInfo>().Single().StarRating, Is.EqualTo(-1)));

        #endregion

        #region Case 21: collection import (only the "nothing to import" side is deterministic)

        [Test]
        public void TestVersion21_NoCollectionDatabase_NothingImportedOrRenamed() =>
            migrate(21, (_, _) => { }, (_, storage) =>
            {
                Assert.That(storage.Exists("collection.db.migrated"), Is.False);
                Assert.That(storage.Exists("collection.db"), Is.False);
            });

        #endregion

        #region Case 25: protected skins

        [Test]
        public void TestVersion25_OnlyProtectedSkinsRemoved() =>
            migrate(25, (r, _) =>
            {
                r.Add(new SkinInfo("protected") { ID = Guid.NewGuid(), Protected = true }, true);
                r.Add(new SkinInfo("custom") { ID = Guid.NewGuid(), Protected = false }, true);
            }, (r, _) => Assert.That(r.All<SkinInfo>().ToList().Select(s => s.Name), Is.EqualTo(new[] { "custom" })));

        #endregion

        #region Case 26: beatmap hash (null-conditional + null-coalescing)

        [Test]
        public void TestVersion26_ScoreWithBeatmap_HashCopiedFromBeatmap() =>
            migrate(26, (r, _) => r.Add(new ScoreInfo(createBeatmap(b => b.Hash = "beatmap-hash"), createRuleset(0)) { BeatmapHash = string.Empty }, true),
                (r, _) => Assert.That(r.All<ScoreInfo>().Single().BeatmapHash, Is.EqualTo("beatmap-hash")));

        [Test]
        public void TestVersion26_ScoreWithoutBeatmap_HashBecomesEmpty() =>
            migrate(26, (r, _) =>
            {
                var score = new ScoreInfo(null, createRuleset(0)) { BeatmapHash = "stale" };
                score.BeatmapInfo = null;
                r.Add(score, true);
            }, (r, _) => Assert.That(r.All<ScoreInfo>().Single().BeatmapHash, Is.EqualTo(string.Empty)));

        #endregion

        #region Case 28: replay version check

        [Test]
        public void TestVersion28_ScoreWithoutReplayFile_Unchanged() =>
            migrate(28, (r, _) => r.Add(new ScoreInfo(createBeatmap(), createRuleset(0)), true), (r, _) => Assert.That(r.All<ScoreInfo>().Single().IsLegacyScore, Is.False));

        [Test]
        public void TestVersion28_ReplayBelowFirstLazerVersion_MarkedLegacy() =>
            migrate(28, (r, storage) =>
            {
                writeReplay(storage, "aabb28legacy", w =>
                {
                    w.Write((byte)0);
                    w.Write(20200101);
                });
                r.Add(createScoreWithReplay("aabb28legacy", createRuleset(0)), true);
            }, (r, _) => Assert.That(r.All<ScoreInfo>().Single().IsLegacyScore, Is.True));

        [Test]
        public void TestVersion28_ReplayAtLazerVersion_NotMarkedLegacy() =>
            migrate(28, (r, storage) =>
            {
                writeReplay(storage, "aabb28lazer", w =>
                {
                    w.Write((byte)0);
                    w.Write(30000000);
                });
                r.Add(createScoreWithReplay("aabb28lazer", createRuleset(0)), true);
            }, (r, _) => Assert.That(r.All<ScoreInfo>().Single().IsLegacyScore, Is.False));

        #endregion

        #region Case 29 / 30: intentionally empty

        [Test]
        public void TestVersion29_EmptyCase_LeavesDataUntouched() =>
            migrate(29, (r, _) => r.Add(createBeatmap(b => b.StarRating = 3), true), (r, _) => Assert.That(r.All<BeatmapInfo>().Single().StarRating, Is.EqualTo(3)));

        [Test]
        public void TestVersion30_EmptyCase_LeavesDataUntouched() =>
            migrate(30, (r, _) => r.Add(createBeatmap(b => b.StarRating = 3), true), (r, _) => Assert.That(r.All<BeatmapInfo>().Single().StarRating, Is.EqualTo(3)));

        #endregion

        #region Case 31: IsLegacyScore && IsLegacyRuleset()

        [Test]
        public void TestVersion31_LegacyScoreOnLegacyRuleset_UpgradeQueuedAndTotalPreserved() =>
            migrate(31, (r, _) => r.Add(new ScoreInfo(createBeatmap(), createRuleset(0)) { IsLegacyScore = true, TotalScore = 5000 }, true), (r, _) =>
            {
                var score = r.All<ScoreInfo>().Single();
                Assert.That(score.TotalScoreVersion, Is.EqualTo(30000002));
                Assert.That(score.LegacyTotalScore, Is.EqualTo(5000));
            });

        [Test]
        public void TestVersion31_LegacyScoreOnCustomRuleset_MarkedAsLatestVersion() =>
            migrate(31, (r, _) => r.Add(new ScoreInfo(createBeatmap(), createRuleset(999)) { IsLegacyScore = true, TotalScore = 5000 }, true), (r, _) =>
            {
                var score = r.All<ScoreInfo>().Single();
                Assert.That(score.TotalScoreVersion, Is.EqualTo(30000003));
                Assert.That(score.LegacyTotalScore, Is.Null);
            });

        [Test]
        public void TestVersion31_NonLegacyScore_MarkedAsLatestVersion() =>
            migrate(31, (r, _) => r.Add(new ScoreInfo(createBeatmap(), createRuleset(0)) { IsLegacyScore = false, TotalScore = 5000 }, true), (r, _) =>
            {
                var score = r.All<ScoreInfo>().Single();
                Assert.That(score.TotalScoreVersion, Is.EqualTo(30000003));
                Assert.That(score.LegacyTotalScore, Is.Null);
            });

        #endregion

        #region Case 32: score v2 detection

        [Test]
        public void TestVersion32_NonLegacyScore_Skipped() => assertScoreV2Result(false, 0, createScoreV2Replay(true), false);

        [Test]
        public void TestVersion32_LegacyScoreOnCustomRuleset_Skipped() => assertScoreV2Result(true, 999, createScoreV2Replay(true), false);

        [Test]
        public void TestVersion32_ReplayWithoutScoreV2Flag_Unchanged() => assertScoreV2Result(true, 0, createScoreV2Replay(false), false);

        [Test]
        public void TestVersion32_ScoreV2ModAlreadyPresent_Unchanged() => assertScoreV2Result(true, 0, createScoreV2Replay(true), true);

        [Test]
        public void TestVersion32_ReplayWithScoreV2Flag_ModAddedAndTotalReplaced() => assertScoreV2Result(true, 0, createScoreV2Replay(true), false, expectMigrated: true);

        #endregion

        #region Case 35: duplicate binding cleanup

        [Test]
        public void TestVersion35_CatchDashBothShift_SecondBecomesLeftClick() =>
            migrate(35, (r, _) =>
            {
                r.Add(createBinding(2, new KeyCombination(InputKey.Shift), "fruits", 0), true);
                r.Add(createBinding(2, new KeyCombination(InputKey.Shift), "fruits", 0), true);
            }, (r, _) => Assert.That(r.All<RealmKeyBinding>().ToList().Select(b => b.KeyCombination.Keys.Single()),
                Is.EquivalentTo(new[] { InputKey.Shift, InputKey.MouseLeft })));

        [Test]
        public void TestVersion35_CatchDashAlreadyDistinct_Unchanged() =>
            migrate(35, (r, _) =>
            {
                r.Add(createBinding(2, new KeyCombination(InputKey.Shift), "fruits", 0), true);
                r.Add(createBinding(2, new KeyCombination(InputKey.Z), "fruits", 0), true);
            }, (r, _) => Assert.That(r.All<RealmKeyBinding>().ToList().Select(b => b.KeyCombinationString),
                Is.EquivalentTo(new[] { new KeyCombination(InputKey.Shift).ToString(), new KeyCombination(InputKey.Z).ToString() })));

        [Test]
        public void TestVersion35_GlobalBindingsSharingCombinationInCategory_Cleared()
        {
            var actions = Enum.GetValues<GlobalActionCategory>().Select(c => GlobalActionContainer.GetGlobalActionsFor(c).ToArray()).First(a => a.Length >= 2).Take(2).ToArray();

            migrate(35, (r, _) =>
            {
                seedDistinctCatchDash(r);
                r.Add(createBinding(actions[0], new KeyCombination(InputKey.F9)), true);
                r.Add(createBinding(actions[1], new KeyCombination(InputKey.F9)), true);
            }, (r, _) => Assert.That(r.All<RealmKeyBinding>().ToList().Where(b => b.RulesetName == null).Select(b => b.KeyCombinationString),
                Is.All.EqualTo(new KeyCombination(InputKey.None).ToString())));
        }

        [Test]
        public void TestVersion35_RulesetBindingsSharingCombinationInVariant_Cleared() =>
            migrate(35, (r, _) =>
            {
                seedDistinctCatchDash(r);
                r.Add(createBinding(0, new KeyCombination(InputKey.F9), "mania", 4), true);
                r.Add(createBinding(1, new KeyCombination(InputKey.F9), "mania", 4), true);
            }, (r, _) => Assert.That(r.All<RealmKeyBinding>().ToList().Where(b => b.RulesetName == "mania").Select(b => b.KeyCombinationString),
                Is.All.EqualTo(new KeyCombination(InputKey.None).ToString())));

        [Test]
        public void TestVersion35_NoDuplicates_NothingCleared() =>
            migrate(35, (r, _) =>
            {
                seedDistinctCatchDash(r);
                r.Add(createBinding(GlobalAction.ToggleChatFocus, new KeyCombination(InputKey.F9)), true);
                r.Add(createBinding(0, new KeyCombination(InputKey.F8), "mania", 4), true);
            }, (r, _) =>
            {
                var combos = r.All<RealmKeyBinding>().ToList().Select(b => b.KeyCombinationString).ToList();
                Assert.That(combos, Does.Not.Contain(new KeyCombination(InputKey.None).ToString()));
                Assert.That(combos, Has.Count.EqualTo(4));
            });

        #endregion

        #region Case 36: online ID split

        [Test]
        public void TestVersion36_PositiveOnlineId_MovedToLegacyOnlineId() =>
            migrate(36, (r, _) => r.Add(new ScoreInfo(createBeatmap(), createRuleset(0)) { OnlineID = 1234 }, true), (r, _) =>
            {
                var score = r.All<ScoreInfo>().Single();
                Assert.That(score.LegacyOnlineID, Is.EqualTo(1234));
                Assert.That(score.OnlineID, Is.EqualTo(-1));
            });

        [Test]
        public void TestVersion36_NonPositiveOnlineId_BothBecomeMinusOne() =>
            migrate(36, (r, _) => r.Add(new ScoreInfo(createBeatmap(), createRuleset(0)) { OnlineID = 0, LegacyOnlineID = 77 }, true), (r, _) =>
            {
                var score = r.All<ScoreInfo>().Single();
                Assert.That(score.LegacyOnlineID, Is.EqualTo(-1));
                Assert.That(score.OnlineID, Is.EqualTo(-1));
            });

        #endregion

        #region Case 39: TotalObjectCount == 0 && EndTimeObjectCount == 0

        [Test]
        public void TestVersion39_BothCountsZero_ResetToMinusOne() =>
            migrate(39, (r, _) => r.Add(createBeatmap(b => (b.TotalObjectCount, b.EndTimeObjectCount) = (0, 0)), true), (r, _) =>
            {
                var beatmap = r.All<BeatmapInfo>().Single();
                Assert.That((beatmap.TotalObjectCount, beatmap.EndTimeObjectCount), Is.EqualTo((-1, -1)));
            });

        [Test]
        public void TestVersion39_TotalZeroButEndTimeNonZero_Unchanged() =>
            migrate(39, (r, _) => r.Add(createBeatmap(b => (b.TotalObjectCount, b.EndTimeObjectCount) = (0, 3)), true), (r, _) =>
            {
                var beatmap = r.All<BeatmapInfo>().Single();
                Assert.That((beatmap.TotalObjectCount, beatmap.EndTimeObjectCount), Is.EqualTo((0, 3)));
            });

        [Test]
        public void TestVersion39_TotalNonZero_Unchanged() =>
            migrate(39, (r, _) => r.Add(createBeatmap(b => (b.TotalObjectCount, b.EndTimeObjectCount) = (10, 0)), true), (r, _) =>
            {
                var beatmap = r.All<BeatmapInfo>().Single();
                Assert.That((beatmap.TotalObjectCount, beatmap.EndTimeObjectCount), Is.EqualTo((10, 0)));
            });

        #endregion

        #region Case 41: try / catch

        [Test]
        public void TestVersion41_LoadableRuleset_TotalScoreWithoutModsPopulated() =>
            migrate(41, (r, _) => r.Add(new ScoreInfo(createBeatmap(), new OsuRuleset().RulesetInfo) { TotalScore = 1000 }, true),
                (r, _) => Assert.That(r.All<ScoreInfo>().Single().TotalScoreWithoutMods, Is.EqualTo(1000)));

        [Test]
        public void TestVersion41_UnloadableRuleset_ExceptionSwallowedAndScoreSkipped() =>
            migrate(41, (r, _) => r.Add(new ScoreInfo(createBeatmap(), new RulesetInfo("missing", "missing", "Missing.Namespace.MissingRuleset, Missing.Assembly", 999)) { TotalScore = 1000 }, true),
                (r, _) => Assert.That(r.All<ScoreInfo>().Single().TotalScoreWithoutMods, Is.Zero));

        #endregion

        #region Case 42: mania binding remap

        [Test]
        public void TestVersion42_NoManiaBindings_NothingAdded() =>
            migrate(42, (_, _) => { }, (r, _) => Assert.That(r.All<RealmKeyBinding>().Count(), Is.Zero));

        [Test]
        public void TestVersion42_SpecialAndNormalActions_RenumberedAndMissingOnesSkipped() =>
            migrate(42, (r, _) =>
            {
                // 1 column (odd, centre column): old Special1 (1) becomes action 0.
                r.Add(createBinding(1, new KeyCombination(InputKey.A), "mania", 1), true);
                // 2 columns (even): old Key1 (10) becomes action 0, old Key2 (11) is absent so nothing is added for action 1.
                r.Add(createBinding(10, new KeyCombination(InputKey.B), "mania", 2), true);
            }, (r, _) =>
            {
                var bindings = r.All<RealmKeyBinding>().ToList().Select(b => (b.Variant, b.ActionInt, b.KeyCombinationString)).ToList();
                Assert.That(bindings, Is.EquivalentTo(new (int?, int, string)[]
                {
                    (1, 0, new KeyCombination(InputKey.A).ToString()),
                    (2, 0, new KeyCombination(InputKey.B).ToString()),
                }));
            });

        #endregion

        #region Case 48 / 49: filtered loops

        [Test]
        public void TestVersion48_QualifiedBeatmap_OnlineInfoResetButIdKept() =>
            migrate(48, (r, _) => r.Add(createBeatmap(b =>
            {
                b.OnlineID = 42;
                b.Status = BeatmapOnlineStatus.Qualified;
                b.OnlineMD5Hash = "online";
            }), true), (r, _) =>
            {
                var beatmap = r.All<BeatmapInfo>().Single();
                Assert.That(beatmap.OnlineID, Is.EqualTo(42));
                Assert.That(beatmap.Status, Is.EqualTo(BeatmapOnlineStatus.None));
                Assert.That(beatmap.OnlineMD5Hash, Is.EqualTo(string.Empty));
            });

        [Test]
        public void TestVersion48_RankedBeatmap_Untouched() =>
            migrate(48, (r, _) => r.Add(createBeatmap(b =>
            {
                b.OnlineID = 42;
                b.Status = BeatmapOnlineStatus.Ranked;
                b.OnlineMD5Hash = "online";
            }), true), (r, _) =>
            {
                var beatmap = r.All<BeatmapInfo>().Single();
                Assert.That(beatmap.Status, Is.EqualTo(BeatmapOnlineStatus.Ranked));
                Assert.That(beatmap.OnlineMD5Hash, Is.EqualTo("online"));
            });

        [Test]
        public void TestVersion49_ZeroLegacyOnlineId_BecomesMinusOne() =>
            migrate(49, (r, _) => r.Add(new ScoreInfo(createBeatmap(), createRuleset(0)) { LegacyOnlineID = 0 }, true),
                (r, _) => Assert.That(r.All<ScoreInfo>().Single().LegacyOnlineID, Is.EqualTo(-1)));

        [Test]
        public void TestVersion49_NonZeroLegacyOnlineId_Kept() =>
            migrate(49, (r, _) => r.Add(new ScoreInfo(createBeatmap(), createRuleset(0)) { LegacyOnlineID = 99 }, true),
                (r, _) => Assert.That(r.All<ScoreInfo>().Single().LegacyOnlineID, Is.EqualTo(99)));

        #endregion

        #region Shared assertions

        private void assertNoBindings(ulong version) =>
            migrate(version, (_, _) => { }, (r, _) => Assert.That(r.All<RealmKeyBinding>().Count(), Is.Zero));

        private void assertMatchingBindingsRemoved(ulong version, params (GlobalAction action, InputKey[] keys)[] bindings) =>
            migrate(version, (r, _) =>
            {
                foreach (var (action, keys) in bindings)
                    r.Add(createBinding(action, new KeyCombination(keys)), true);
            }, (r, _) => Assert.That(r.All<RealmKeyBinding>().Count(), Is.Zero));

        private void assertMismatchingBindingsKept(ulong version, params GlobalAction[] actions) =>
            migrate(version, (r, _) =>
            {
                foreach (var action in actions)
                    r.Add(createBinding(action, new KeyCombination(InputKey.F12)), true);
            }, (r, _) =>
            {
                var remaining = r.All<RealmKeyBinding>().ToList();
                Assert.That(remaining.Select(b => b.ActionInt), Is.EquivalentTo(actions.Select(a => (int)a)));
                Assert.That(remaining.Select(b => b.KeyCombinationString), Is.All.EqualTo(new KeyCombination(InputKey.F12).ToString()));
            });

        private void assertScoreV2Result(bool isLegacyScore, int rulesetOnlineId, Action<SerializationWriter> replay, bool alreadyHasScoreV2, bool expectMigrated = false)
        {
            const string hash = "aabb32replay";

            migrate(32, (r, storage) =>
            {
                writeReplay(storage, hash, replay);

                var score = createScoreWithReplay(hash, createRuleset(rulesetOnlineId));
                score.IsLegacyScore = isLegacyScore;
                score.TotalScore = 1;
                score.APIMods = alreadyHasScoreV2 ? new[] { new APIMod(new ModScoreV2()) } : Array.Empty<APIMod>();
                r.Add(score, true);
            }, (r, _) =>
            {
                var score = r.All<ScoreInfo>().Single();

                if (expectMigrated)
                {
                    Assert.That(score.APIMods.Select(m => m.Acronym), Is.EquivalentTo(new[] { "SV2" }));
                    Assert.That(score.TotalScore, Is.EqualTo(777777));
                    Assert.That(score.LegacyTotalScore, Is.EqualTo(777777));
                }
                else
                {
                    Assert.That(score.APIMods.Length, Is.EqualTo(alreadyHasScoreV2 ? 1 : 0));
                    Assert.That(score.TotalScore, Is.EqualTo(1));
                }
            });
        }

        #endregion

        #region Harness

        /// <summary>
        /// Seeds a realm file at schema version <paramref name="targetVersion"/> - 1, reopens it at <paramref name="targetVersion"/>
        /// and runs only that version's migration case, then hands the migrated realm to <paramref name="verify"/>.
        /// </summary>
        private void migrate(ulong targetVersion, Action<Realm, Storage> seed, Action<Realm, Storage> verify, RealmSchema? oldSchema = null, [CallerMemberName] string caller = "")
        {
            using (var host = new CleanRunHeadlessGameHost(callingMethodName: caller))
            {
                host.Run(new MigrationTestGame(() =>
                {
                    var storage = host.Storage;

                    using (var access = new RealmAccess(storage, "host_client"))
                    {
                        string path = storage.GetFullPath("migration_under_test.realm");

                        var seedConfiguration = new RealmConfiguration(path) { SchemaVersion = targetVersion - 1 };

                        if (oldSchema != null)
                        {
                            seedConfiguration.Schema = oldSchema;
                            seedConfiguration.IsDynamic = true;
                        }

                        using (var seedRealm = Realm.GetInstance(seedConfiguration))
                            seedRealm.Write(() => seed(seedRealm, storage));

                        var migratedConfiguration = new RealmConfiguration(path)
                        {
                            SchemaVersion = targetVersion,
                            MigrationCallback = (migration, _) => invokeMigration(access, migration, targetVersion),
                        };

                        using (var migratedRealm = Realm.GetInstance(migratedConfiguration))
                            verify(migratedRealm, storage);
                    }
                }));
            }
        }

        private static void invokeMigration(RealmAccess access, Migration migration, ulong version)
        {
            try
            {
                apply_migrations_method.Invoke(access, new object[] { migration, version });
            }
            catch (TargetInvocationException e) when (e.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            }
        }

        private partial class MigrationTestGame : Framework.Game
        {
            public MigrationTestGame(Action work)
            {
                Scheduler.Add(() =>
                {
                    work();
                    Exit();
                });
            }
        }

        #endregion

        #region Model and schema helpers

        private static RulesetInfo createRuleset(int onlineId) => onlineId switch
        {
            0 => new RulesetInfo("osu", "osu!", string.Empty, 0) { Available = true },
            _ => new RulesetInfo($"custom{onlineId}", "custom", string.Empty, onlineId) { Available = true },
        };

        private static BeatmapInfo createBeatmap(Action<BeatmapInfo>? configure = null)
        {
            var beatmap = new BeatmapInfo(createRuleset(0), new BeatmapDifficulty(), new BeatmapMetadata()) { ID = Guid.NewGuid() };
            configure?.Invoke(beatmap);
            return beatmap;
        }

        private static RealmKeyBinding createBinding(object action, KeyCombination combination, string? rulesetName = null, int? variant = null) =>
            new RealmKeyBinding(action, combination, rulesetName, variant) { ID = Guid.NewGuid() };

        private static void seedDistinctCatchDash(Realm realm)
        {
            // case 35 asserts on exactly two catch dash bindings when none of them is distinct, so seed a safe pair for every other path.
            realm.Add(createBinding(2, new KeyCombination(InputKey.Shift), "fruits", 0), true);
            realm.Add(createBinding(2, new KeyCombination(InputKey.Z), "fruits", 0), true);
        }

        private static ScoreInfo createScoreWithReplay(string hash, RulesetInfo ruleset)
        {
            var score = new ScoreInfo(createBeatmap(), ruleset);
            score.Files.Add(new RealmNamedFileUsage(new RealmFile { Hash = hash }, "replay.osr"));
            return score;
        }

        private static void writeReplay(Storage storage, string hash, Action<SerializationWriter> write)
        {
            var files = storage.GetStorageForDirectory("files");

            using (var stream = files.CreateFileSafely(new RealmFile { Hash = hash }.GetStoragePath()))
            using (var writer = new SerializationWriter(stream))
                write(writer);
        }

        private static Action<SerializationWriter> createScoreV2Replay(bool withScoreV2Flag) => w =>
        {
            w.Write((byte)0); // ruleset
            w.Write(20200101); // version
            w.Write("beatmap-hash");
            w.Write("player");
            w.Write("replay-hash");
            for (int i = 0; i < 6; i++)
                w.Write((ushort)0); // 300, 100, 50, geki, katu, miss
            w.Write(777777); // total score
            w.Write((ushort)0); // max combo
            w.Write(false); // perfect
            w.Write(withScoreV2Flag ? 1 << 29 : 0); // legacy mods (ScoreV2 = 1 << 29)
        };

        private static IRealmObjectBase createDynamic(Realm realm, string className, Guid? primaryKey = null) =>
            primaryKey == null ? realm.DynamicApi.CreateObject(className) : realm.DynamicApi.CreateObject(className, primaryKey);

        private static void createOldKeyBinding(Realm realm, RealmValue rulesetId)
        {
            var binding = createDynamic(realm, "KeyBinding", Guid.NewGuid());
            binding.DynamicApi.Set("RulesetID", rulesetId);
            binding.DynamicApi.Set("Action", 0L);
            binding.DynamicApi.Set("KeyCombination", new KeyCombination(InputKey.A).ToString());
        }

        private static ObjectSchema objectSchema(string name, params Property[] properties)
        {
            var builder = new ObjectSchema.Builder(name, ObjectSchema.ObjectType.RealmObject);
            foreach (var property in properties)
                builder.Add(property);
            return builder.Build();
        }

        private static RealmSchema schemaOf(params ObjectSchema[] objects)
        {
            var builder = new RealmSchema.Builder();
            foreach (var obj in objects)
                builder.Add(obj);
            return builder.Build();
        }

        private static RealmSchema beatmapSchemaWithNullableOnlineId() => schemaOf(objectSchema("Beatmap",
            Property.Primitive("ID", RealmValueType.Guid, isPrimaryKey: true),
            Property.Primitive("OnlineID", RealmValueType.Int, isNullable: true)));

        private static RealmSchema metadataSchema() => schemaOf(objectSchema("BeatmapMetadata",
            Property.Primitive("Author", RealmValueType.String)));

        private static RealmSchema rulesetSettingSchema() => schemaOf(objectSchema("RulesetSetting",
            Property.Primitive("RulesetID", RealmValueType.Int)));

        private static RealmSchema keyBindingSchema() => schemaOf(objectSchema("KeyBinding",
            Property.Primitive("ID", RealmValueType.Guid, isPrimaryKey: true),
            Property.Primitive("RulesetID", RealmValueType.Int, isNullable: true),
            Property.Primitive("Action", RealmValueType.Int),
            Property.Primitive("KeyCombination", RealmValueType.String)));

        #endregion
    }
}
