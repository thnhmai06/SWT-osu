// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Osu.Tests
{
    [TestFixture]
    public class OsuRulesetTest
    {
        private readonly OsuRuleset ruleset = new OsuRuleset();

        // ---- ConvertFromLegacyMods ----

        private Type[] fromLegacy(LegacyMods mods) => ruleset.ConvertFromLegacyMods(mods).Select(m => m.GetType()).ToArray();

        [Test]
        public void TestFromLegacy_NoFlags_NoMods() => Assert.That(fromLegacy(LegacyMods.None), Is.Empty);

        [Test]
        public void TestFromLegacy_Nightcore_WinsOverDoubleTime()
        {
            // Nightcore TRUE short-circuits the `else if (DoubleTime)`; legacy Nightcore always arrives together with DoubleTime
            Assert.That(fromLegacy(LegacyMods.Nightcore | LegacyMods.DoubleTime), Is.EqualTo(new[] { typeof(OsuModNightcore) }));
        }

        [Test]
        public void TestFromLegacy_DoubleTimeAlone() => Assert.That(fromLegacy(LegacyMods.DoubleTime), Is.EqualTo(new[] { typeof(OsuModDoubleTime) }));

        [Test]
        public void TestFromLegacy_Perfect_WinsOverSuddenDeath()
            => Assert.That(fromLegacy(LegacyMods.Perfect | LegacyMods.SuddenDeath), Is.EqualTo(new[] { typeof(OsuModPerfect) }));

        [Test]
        public void TestFromLegacy_SuddenDeathAlone() => Assert.That(fromLegacy(LegacyMods.SuddenDeath), Is.EqualTo(new[] { typeof(OsuModSuddenDeath) }));

        [Test]
        public void TestFromLegacy_Cinema_WinsOverAutoplay()
            => Assert.That(fromLegacy(LegacyMods.Cinema | LegacyMods.Autoplay), Is.EqualTo(new[] { typeof(OsuModCinema) }));

        [Test]
        public void TestFromLegacy_AutoplayAlone() => Assert.That(fromLegacy(LegacyMods.Autoplay), Is.EqualTo(new[] { typeof(OsuModAutoplay) }));

        [TestCase(LegacyMods.Autopilot, typeof(OsuModAutopilot))]
        [TestCase(LegacyMods.Easy, typeof(OsuModEasy))]
        [TestCase(LegacyMods.Flashlight, typeof(OsuModFlashlight))]
        [TestCase(LegacyMods.HalfTime, typeof(OsuModHalfTime))]
        [TestCase(LegacyMods.HardRock, typeof(OsuModHardRock))]
        [TestCase(LegacyMods.Hidden, typeof(OsuModHidden))]
        [TestCase(LegacyMods.NoFail, typeof(OsuModNoFail))]
        [TestCase(LegacyMods.Relax, typeof(OsuModRelax))]
        [TestCase(LegacyMods.SpunOut, typeof(OsuModSpunOut))]
        [TestCase(LegacyMods.Target, typeof(OsuModTargetPractice))]
        [TestCase(LegacyMods.TouchDevice, typeof(OsuModTouchDevice))]
        [TestCase(LegacyMods.ScoreV2, typeof(ModScoreV2))]
        public void TestFromLegacy_SingleFlag(LegacyMods flag, Type expected) => Assert.That(fromLegacy(flag), Is.EqualTo(new[] { expected }));

        [Test]
        public void TestFromLegacy_UnrelatedFlag_Ignored() => Assert.That(fromLegacy(LegacyMods.Key4), Is.Empty);

        [Test]
        public void TestFromLegacy_MultipleFlags_AllConverted()
        {
            var result = fromLegacy(LegacyMods.Hidden | LegacyMods.HardRock | LegacyMods.DoubleTime);

            Assert.That(result, Is.EquivalentTo(new[] { typeof(OsuModDoubleTime), typeof(OsuModHardRock), typeof(OsuModHidden) }));
        }

        // ---- ConvertToLegacyMods ----

        [TestCase(typeof(OsuModAutopilot), LegacyMods.Autopilot)]
        [TestCase(typeof(OsuModSpunOut), LegacyMods.SpunOut)]
        [TestCase(typeof(OsuModTargetPractice), LegacyMods.Target)]
        [TestCase(typeof(OsuModTouchDevice), LegacyMods.TouchDevice)]
        public void TestToLegacy_RulesetSpecificCase(Type modType, LegacyMods expected)
        {
            Assert.That(ruleset.ConvertToLegacyMods(new[] { (Mod)Activator.CreateInstance(modType)! }) & expected, Is.EqualTo(expected));
        }

        [Test]
        public void TestToLegacy_CommonModHandledByBaseOnly()
        {
            // switch default: no ruleset-specific flag is added, base flag is preserved
            var result = ruleset.ConvertToLegacyMods(new Mod[] { new OsuModHidden() });

            Assert.That(result, Is.EqualTo(LegacyMods.Hidden));
        }

        [Test]
        public void TestToLegacy_NoMods_None() => Assert.That(ruleset.ConvertToLegacyMods(Array.Empty<Mod>()), Is.EqualTo(LegacyMods.None));

        [Test]
        public void TestToLegacy_MultipleMods_CombinedFlags()
        {
            var result = ruleset.ConvertToLegacyMods(new Mod[] { new OsuModSpunOut(), new OsuModHidden(), new OsuModTouchDevice() });

            Assert.That(result, Is.EqualTo(LegacyMods.SpunOut | LegacyMods.Hidden | LegacyMods.TouchDevice));
        }

        [Test]
        public void TestToLegacy_ModWithoutLegacyEquivalent_AddsNothing()
        {
            Assert.That(ruleset.ConvertToLegacyMods(new Mod[] { new OsuModMirror() }), Is.EqualTo(LegacyMods.None));
        }

        // ---- CreateScoreMultiplierCalculator ----

        private static ScoreMultiplierContext context(ScoreInfo? score) => new ScoreMultiplierContext(new BeatmapDifficulty(), score);

        [Test]
        public void TestMultiplierCalculator_NoScore_UsesLatestVersion()
        {
            // context.Score != null FALSE
            Assert.That(ruleset.CreateScoreMultiplierCalculator(context(null)), Is.TypeOf<OsuScoreMultiplierCalculatorV2>());
        }

        [Test]
        public void TestMultiplierCalculator_OldScore_UsesV1()
        {
            // Score != null TRUE && TotalScoreVersion < 30000017 TRUE
            Assert.That(ruleset.CreateScoreMultiplierCalculator(context(new ScoreInfo { TotalScoreVersion = 30000016 })), Is.TypeOf<OsuScoreMultiplierCalculatorV1>());
        }

        [Test]
        public void TestMultiplierCalculator_ScoreAtVersionBoundary_UsesV2()
        {
            // Score != null TRUE && TotalScoreVersion < 30000017 FALSE
            Assert.That(ruleset.CreateScoreMultiplierCalculator(context(new ScoreInfo { TotalScoreVersion = 30000017 })), Is.TypeOf<OsuScoreMultiplierCalculatorV2>());
        }

        // ---- hit results ----

        [Test]
        public void TestValidHitResults_ContainBasicAndNestedResults()
        {
            var results = ruleset.GetValidHitResults().ToList();

            Assert.That(results, Is.SupersetOf(new[] { HitResult.Great, HitResult.Ok, HitResult.Meh, HitResult.Miss, HitResult.LargeTickHit, HitResult.SliderTailHit, HitResult.SmallBonus, HitResult.LargeBonus }));
            Assert.That(results, Is.Unique);
            Assert.That(results, Does.Not.Contain(HitResult.Perfect));
        }

        [TestCase(HitResult.LargeTickHit, "slider tick")]
        [TestCase(HitResult.SliderTailHit, "slider end")]
        [TestCase(HitResult.SmallTickHit, "slider end")]
        [TestCase(HitResult.SmallBonus, "spinner spin")]
        [TestCase(HitResult.LargeBonus, "spinner bonus")]
        public void TestDisplayName_RulesetSpecificCases(HitResult result, string expected)
        {
            Assert.That(ruleset.GetDisplayNameForHitResult(result).ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void TestDisplayName_OtherResults_FallBackToBase()
        {
            // switch has no matching case => base implementation
            Assert.That(ruleset.GetDisplayNameForHitResult(HitResult.Great).ToString(), Is.Not.AnyOf("slider tick", "slider end", "spinner spin", "spinner bonus"));
        }

        // ---- GetAdjustedDisplayDifficulty ----

        private static BeatmapInfo info(float ar = 8, float od = 8) => new BeatmapInfo { Difficulty = new BeatmapDifficulty { ApproachRate = ar, OverallDifficulty = od } };

        [Test]
        public void TestAdjustedDifficulty_NoRateChange_KeepsApproachRateAndOd()
        {
            var adjusted = ruleset.GetAdjustedDisplayDifficulty(info(), Array.Empty<Mod>());

            Assert.That(adjusted.ApproachRate, Is.EqualTo(8).Within(1e-3));
            Assert.That(adjusted.OverallDifficulty, Is.EqualTo(8).Within(1e-3));
        }

        [Test]
        public void TestAdjustedDifficulty_Speedup_RaisesApproachRateAndOd()
        {
            var adjusted = ruleset.GetAdjustedDisplayDifficulty(info(), new Mod[] { new OsuModDoubleTime() });

            Assert.That(adjusted.ApproachRate, Is.GreaterThan(8));
            Assert.That(adjusted.OverallDifficulty, Is.GreaterThan(8));
        }

        [Test]
        public void TestAdjustedDifficulty_Slowdown_LowersApproachRateAndOd()
        {
            var adjusted = ruleset.GetAdjustedDisplayDifficulty(info(), new Mod[] { new OsuModHalfTime() });

            Assert.That(adjusted.ApproachRate, Is.LessThan(8));
            Assert.That(adjusted.OverallDifficulty, Is.LessThan(8));
        }

        [Test]
        public void TestRulesetMetadata()
        {
            Assert.That(ruleset.ShortName, Is.EqualTo(OsuRuleset.SHORT_NAME));
            Assert.That(ruleset.EditorShowScrollSpeed, Is.False);
        }
    }
}
