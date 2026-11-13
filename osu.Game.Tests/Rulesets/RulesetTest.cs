// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;

namespace osu.Game.Tests.Rulesets
{
    [TestFixture]
    public class RulesetTest
    {
        private class StubRuleset : Ruleset
        {
            private readonly string shortName;
            private readonly Func<ModType, IEnumerable<Mod>> mods;
            private readonly HitResult[]? validResults;

            public StubRuleset(string shortName, Func<ModType, IEnumerable<Mod>>? mods = null, HitResult[]? validResults = null)
            {
                this.shortName = shortName;
                this.mods = mods ?? (_ => []);
                this.validResults = validResults;
            }

            public override string Description => "stub";
            public override string ShortName => shortName;
            public override IEnumerable<Mod> GetModsFor(ModType type) => mods(type);
            public override IEnumerable<HitResult> GetValidHitResults() => validResults ?? base.GetValidHitResults();

            public override DrawableRuleset CreateDrawableRulesetWith(IBeatmap beatmap, IReadOnlyList<Mod>? mods = null) => throw new NotSupportedException();
            public override IBeatmapConverter CreateBeatmapConverter(IBeatmap beatmap) => throw new NotSupportedException();
            public override DifficultyCalculator CreateDifficultyCalculator(IWorkingBeatmap beatmap) => throw new NotSupportedException();
        }

        // CF: Ruleset.ctor | (this as ILegacyRuleset)?.LegacyID ?? -1 | default
        [Test]
        public void TestRulesetInfoForNonLegacyRuleset()
        {
            var info = new StubRuleset("stub-ctor").RulesetInfo;

            Assert.That(info.OnlineID, Is.EqualTo(-1));
            Assert.That(info.Available, Is.True);
        }

        // CF: Ruleset.ctor | (this as ILegacyRuleset)?.LegacyID ?? -1 | non-null
        [Test]
        public void TestRulesetInfoForLegacyRuleset()
        {
            var ruleset = new OsuRuleset();

            Assert.That(ruleset.RulesetInfo.OnlineID, Is.EqualTo(0));
            Assert.That(ruleset.RulesetInfo.ShortName, Is.EqualTo(ruleset.ShortName));
            Assert.That(ruleset.RulesetInfo.Name, Is.EqualTo(ruleset.Description));
        }

        // CF: Ruleset.AllMods | string.IsNullOrEmpty(ShortName) | TRUE
        [Test]
        public void TestAllModsNotCachedWithoutShortName()
        {
            var ruleset = new StubRuleset(string.Empty, t => t == ModType.Fun ? [new OsuModNoFail()] : []);

            Assert.That(ruleset.AllMods.Single(), Is.Not.SameAs(ruleset.AllMods.Single()));
        }

        // CF: Ruleset.AllMods | string.IsNullOrEmpty(ShortName) | FALSE
        // CF: Ruleset.AllMods | !mod_reference_cache.TryGetValue(ShortName, out mods) | TRUE
        // CF: Ruleset.AllMods | !mod_reference_cache.TryGetValue(ShortName, out mods) | FALSE
        [Test]
        public void TestAllModsCachedByShortName()
        {
            var ruleset = new StubRuleset("stub-allmods-cache", t => t == ModType.Fun ? [new OsuModNoFail()] : []);
            var other = new StubRuleset("stub-allmods-cache", t => t == ModType.Fun ? [new OsuModEasy()] : []);

            Assert.That(ruleset.AllMods.Single(), Is.SameAs(ruleset.AllMods.Single()));
            Assert.That(other.AllMods.Single(), Is.InstanceOf<OsuModNoFail>(), "second ruleset with the same short name reuses the first cache entry");
        }

        // CF: Ruleset.CreateAllMods | mod != null | FALSE
        // CF: Ruleset.CreateAllMods | (mod as MultiMod)?.Mods ?? new[] { mod } | non-null
        // CF: Ruleset.CreateAllMods | (mod as MultiMod)?.Mods ?? new[] { mod } | null
        [Test]
        public void TestCreateAllModsDropsNullsAndFlattensMultiMods()
        {
            var ruleset = new StubRuleset("stub-create", t => t switch
            {
                ModType.DifficultyReduction => [null!, new MultiMod(new OsuModEasy(), new OsuModNoFail())],
                ModType.Fun => [new OsuModHidden()],
                _ => [],
            });

            Assert.That(ruleset.CreateAllMods().Select(m => m.GetType()),
                Is.EqualTo(new[] { typeof(OsuModEasy), typeof(OsuModNoFail), typeof(OsuModHidden) }));
        }

        // CF: Ruleset.CreateModFromAcronym | string.Equals(m.Acronym, acronym, OrdinalIgnoreCase) | TRUE
        [Test]
        public void TestCreateModFromAcronymIsCaseInsensitiveAndReturnsFreshInstance()
        {
            var ruleset = new StubRuleset("stub-acronym", t => t == ModType.Fun ? [new OsuModNoFail()] : []);

            var mod = ruleset.CreateModFromAcronym("nf");

            Assert.That(mod, Is.InstanceOf<OsuModNoFail>());
            Assert.That(mod, Is.Not.SameAs(ruleset.AllMods.Single()));
        }

        // CF: Ruleset.CreateModFromAcronym | string.Equals(m.Acronym, acronym, OrdinalIgnoreCase) | FALSE
        [Test]
        public void TestCreateModFromAcronymUnknown()
        {
            Assert.That(new StubRuleset("stub-acronym-none").CreateModFromAcronym("ZZ"), Is.Null);
        }

        // CF: Ruleset.CreateMod | m is T | TRUE
        // CF: Ruleset.CreateMod | m is T | FALSE
        [Test]
        public void TestCreateModByType()
        {
            var ruleset = new StubRuleset("stub-create-mod", t => t == ModType.Fun ? [new OsuModNoFail()] : []);

            Assert.That(ruleset.CreateMod<OsuModNoFail>(), Is.Not.Null);
            Assert.That(ruleset.CreateMod<OsuModEasy>(), Is.Null);
            Assert.That(ruleset.GetAutoplayMod(), Is.Null);
            Assert.That(ruleset.GetTouchDeviceMod(), Is.Null);
        }

        // CF: Ruleset.ConvertToLegacyMods | foreach mod | loop 0
        [Test]
        public void TestConvertToLegacyModsEmpty()
        {
            Assert.That(new StubRuleset("stub-legacy").ConvertToLegacyMods([]), Is.EqualTo(LegacyMods.None));
        }

        // CF: Ruleset.ConvertToLegacyMods | switch (mod) | case ModNoFail
        // CF: Ruleset.ConvertToLegacyMods | switch (mod) | case ModEasy
        // CF: Ruleset.ConvertToLegacyMods | switch (mod) | case ModHidden
        // CF: Ruleset.ConvertToLegacyMods | switch (mod) | case ModHardRock
        // CF: Ruleset.ConvertToLegacyMods | switch (mod) | case ModPerfect
        // CF: Ruleset.ConvertToLegacyMods | switch (mod) | case ModSuddenDeath
        // CF: Ruleset.ConvertToLegacyMods | switch (mod) | case ModNightcore
        // CF: Ruleset.ConvertToLegacyMods | switch (mod) | case ModDoubleTime
        // CF: Ruleset.ConvertToLegacyMods | switch (mod) | case ModRelax
        // CF: Ruleset.ConvertToLegacyMods | switch (mod) | case ModHalfTime
        // CF: Ruleset.ConvertToLegacyMods | switch (mod) | case ModFlashlight
        // CF: Ruleset.ConvertToLegacyMods | switch (mod) | case ModCinema
        // CF: Ruleset.ConvertToLegacyMods | switch (mod) | case ModAutoplay
        // CF: Ruleset.ConvertToLegacyMods | switch (mod) | case ModScoreV2
        [TestCaseSource(nameof(legacyModCases))]
        public void TestConvertToLegacyMods(Mod mod, LegacyMods expected)
        {
            Assert.That(new StubRuleset("stub-legacy").ConvertToLegacyMods([mod]), Is.EqualTo(expected));
        }

        private static IEnumerable<TestCaseData> legacyModCases() =>
        [
            new TestCaseData(new OsuModNoFail(), LegacyMods.NoFail),
            new TestCaseData(new OsuModEasy(), LegacyMods.Easy),
            new TestCaseData(new OsuModHidden(), LegacyMods.Hidden),
            new TestCaseData(new OsuModHardRock(), LegacyMods.HardRock),
            new TestCaseData(new OsuModPerfect(), LegacyMods.Perfect | LegacyMods.SuddenDeath),
            new TestCaseData(new OsuModSuddenDeath(), LegacyMods.SuddenDeath),
            new TestCaseData(new OsuModNightcore(), LegacyMods.Nightcore | LegacyMods.DoubleTime),
            new TestCaseData(new OsuModDoubleTime(), LegacyMods.DoubleTime),
            new TestCaseData(new OsuModRelax(), LegacyMods.Relax),
            new TestCaseData(new OsuModHalfTime(), LegacyMods.HalfTime),
            new TestCaseData(new OsuModFlashlight(), LegacyMods.Flashlight),
            new TestCaseData(new OsuModCinema(), LegacyMods.Cinema | LegacyMods.Autoplay),
            new TestCaseData(new OsuModAutoplay(), LegacyMods.Autoplay),
            new TestCaseData(new ModScoreV2(), LegacyMods.ScoreV2),
        ];

        // CF: Ruleset.ConvertToLegacyMods | switch (mod) | default
        // CF: Ruleset.ConvertToLegacyMods | foreach mod | loop n
        [Test]
        public void TestConvertToLegacyModsCombinesAndIgnoresUnknown()
        {
            var value = new StubRuleset("stub-legacy").ConvertToLegacyMods([new OsuModHidden(), new OsuModSpunOut(), new OsuModHardRock()]);

            Assert.That(value, Is.EqualTo(LegacyMods.Hidden | LegacyMods.HardRock));
        }

        // CF: Ruleset.GetHitResultsForDisplay | result is hard blocked (None/IgnoreHit/IgnoreMiss/ComboBreak/LargeTickMiss/SmallTickMiss) | TRUE
        [Test]
        public void TestHitResultsForDisplayExcludesBlockedTypes()
        {
            var results = new StubRuleset("stub-display").GetHitResultsForDisplay().Select(r => r.result).ToArray();

            Assert.That(results, Does.Not.Contain(HitResult.None));
            Assert.That(results, Does.Not.Contain(HitResult.IgnoreHit));
            Assert.That(results, Does.Not.Contain(HitResult.IgnoreMiss));
            Assert.That(results, Does.Not.Contain(HitResult.ComboBreak));
            Assert.That(results, Does.Not.Contain(HitResult.LargeTickMiss));
            Assert.That(results, Does.Not.Contain(HitResult.SmallTickMiss));
            Assert.That(results, Does.Contain(HitResult.Great));
        }

        // CF: Ruleset.GetHitResultsForDisplay | result == HitResult.Miss || validResults.Contains(result) | TRUE
        // CF: Ruleset.GetHitResultsForDisplay | result == HitResult.Miss || validResults.Contains(result) | FALSE
        [Test]
        public void TestHitResultsForDisplayLimitedToValidResultsPlusMiss()
        {
            var ruleset = new StubRuleset("stub-display", validResults: [HitResult.Great, HitResult.Ok]);

            Assert.That(ruleset.GetHitResultsForDisplay().Select(r => r.result), Is.EquivalentTo(new[] { HitResult.Miss, HitResult.Ok, HitResult.Great }));
        }

        [Test]
        public void TestAllVariantsIncludesEditorVariant()
        {
            Assert.That(new StubRuleset("stub-variants").AllVariants, Is.EqualTo(new[] { 0, Ruleset.EDITOR_VARIANT }));
        }

        // CF: Ruleset.GetAdjustedDisplayDifficulty | foreach mod in mods.OfType<IApplicableToDifficulty>() | loop 0
        [Test]
        public void TestAdjustedDifficultyWithoutMods()
        {
            var info = new BeatmapInfo { Difficulty = new BeatmapDifficulty { CircleSize = 4, OverallDifficulty = 8 } };

            var adjusted = new StubRuleset("stub-diff").GetAdjustedDisplayDifficulty(info, []);

            Assert.That(adjusted.CircleSize, Is.EqualTo(4));
            Assert.That(adjusted, Is.Not.SameAs(info.Difficulty), "must be a copy");
        }

        // CF: Ruleset.GetAdjustedDisplayDifficulty | foreach mod in mods.OfType<IApplicableToDifficulty>() | loop n
        [Test]
        public void TestAdjustedDifficultyAppliesDifficultyMods()
        {
            var info = new BeatmapInfo { Difficulty = new BeatmapDifficulty { CircleSize = 4, OverallDifficulty = 8 } };

            var adjusted = new StubRuleset("stub-diff").GetAdjustedDisplayDifficulty(info, [new OsuModEasy(), new OsuModHidden()]);

            Assert.That(adjusted.OverallDifficulty, Is.LessThan(8), "Easy lowers difficulty");
            Assert.That(info.Difficulty.OverallDifficulty, Is.EqualTo(8), "original untouched");
        }

        [Test]
        public void TestBeatmapAttributesForDisplay()
        {
            var info = new BeatmapInfo { Difficulty = new BeatmapDifficulty { CircleSize = 4, ApproachRate = 9, OverallDifficulty = 8, DrainRate = 5 } };
            var ruleset = new StubRuleset("stub-attrs");

            var attributes = ruleset.GetBeatmapAttributesForDisplay(info, []).ToArray();

            Assert.That(attributes.Select(a => a.Acronym), Is.EqualTo(new[] { "CS", "AR", "OD", "HP" }));
            Assert.That(attributes.Select(a => a.OriginalValue), Is.EqualTo(new[] { 4f, 9f, 8f, 5f }));
            Assert.That(ruleset.GetBeatmapAttributesForRankedPlayCard(info, []).Select(a => a.Acronym), Is.EqualTo(attributes.Select(a => a.Acronym)));
        }
    }
}
