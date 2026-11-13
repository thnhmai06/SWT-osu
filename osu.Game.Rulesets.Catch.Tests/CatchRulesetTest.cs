// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Catch.Mods;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets.Catch.Difficulty;
using osu.Game.Rulesets.Catch.Replays;
using osu.Game.Rulesets.Catch.Scoring;

namespace osu.Game.Rulesets.Catch.Tests
{
    [TestFixture]
    public class CatchRulesetTest
    {
        private readonly CatchRuleset ruleset = new CatchRuleset();

        // CF: CatchRuleset.GetDefaultKeyBindings | switch variant | default
        [Test]
        public void TestDefaultBindingsContainMovementAndDash()
        {
            var bindings = ruleset.GetDefaultKeyBindings().ToArray();

            Assert.That(bindings.Select(b => b.Action).Distinct(), Is.EquivalentTo(new object[] { CatchAction.MoveLeft, CatchAction.MoveRight, CatchAction.Dash }));
        }

        // CF: CatchRuleset.GetDefaultKeyBindings | switch variant | case EDITOR_VARIANT
        [Test]
        public void TestEditorBindings()
        {
            var actions = ruleset.GetDefaultKeyBindings(Ruleset.EDITOR_VARIANT).Select(b => b.Action).ToArray();

            Assert.That(actions, Is.EquivalentTo(new object[]
            {
                CatchAction.EditorFruitTool, CatchAction.EditorJuiceStreamTool, CatchAction.EditorBananaShowerTool, CatchAction.EditorToggleDistanceSnap,
            }));
        }

        // CF: CatchRuleset.ConvertFromLegacyMods | mods.HasFlag(Nightcore) | FALSE
        // CF: CatchRuleset.ConvertFromLegacyMods | each flag | FALSE
        [Test]
        public void TestNoLegacyModsConvertsToNothing()
        {
            Assert.That(ruleset.ConvertFromLegacyMods(LegacyMods.None), Is.Empty);
        }

        // CF: CatchRuleset.ConvertFromLegacyMods | mods.HasFlag(Nightcore) | TRUE
        // CF: CatchRuleset.ConvertFromLegacyMods | mods.HasFlag(Perfect) | TRUE
        // CF: CatchRuleset.ConvertFromLegacyMods | mods.HasFlag(Cinema) | TRUE
        // CF: CatchRuleset.ConvertFromLegacyMods | each remaining flag | TRUE
        [Test]
        public void TestLegacyModsConvert()
        {
            var mods = ruleset.ConvertFromLegacyMods(
                LegacyMods.Nightcore | LegacyMods.Perfect | LegacyMods.Cinema | LegacyMods.Easy | LegacyMods.Flashlight | LegacyMods.HalfTime
                | LegacyMods.HardRock | LegacyMods.Hidden | LegacyMods.NoFail | LegacyMods.Relax | LegacyMods.ScoreV2).Select(m => m.GetType()).ToArray();

            Assert.That(mods, Is.EquivalentTo(new[]
            {
                typeof(CatchModNightcore), typeof(CatchModPerfect), typeof(CatchModCinema), typeof(CatchModEasy), typeof(CatchModFlashlight),
                typeof(CatchModHalfTime), typeof(CatchModHardRock), typeof(CatchModHidden), typeof(CatchModNoFail), typeof(CatchModRelax), typeof(ModScoreV2),
            }));
        }

        // CF: CatchRuleset.ConvertFromLegacyMods | else-if DoubleTime | TRUE
        // CF: CatchRuleset.ConvertFromLegacyMods | else-if SuddenDeath | TRUE
        // CF: CatchRuleset.ConvertFromLegacyMods | else-if Autoplay | TRUE
        [Test]
        public void TestLegacyAlternativesConvertWhenPreferredFlagAbsent()
        {
            var mods = ruleset.ConvertFromLegacyMods(LegacyMods.DoubleTime | LegacyMods.SuddenDeath | LegacyMods.Autoplay).Select(m => m.GetType()).ToArray();

            Assert.That(mods, Is.EquivalentTo(new[] { typeof(CatchModDoubleTime), typeof(CatchModSuddenDeath), typeof(CatchModAutoplay) }));
        }

        // CF: CatchRuleset.GetModsFor | switch type | case DifficultyReduction / DifficultyIncrease / Conversion / Automation / Fun / System
        [TestCase(ModType.DifficultyReduction, 3)]
        [TestCase(ModType.DifficultyIncrease, 6)]
        [TestCase(ModType.Conversion, 3)]
        [TestCase(ModType.Automation, 2)]
        [TestCase(ModType.Fun, 6)]
        [TestCase(ModType.System, 1)]
        public void TestModsForType(ModType type, int expectedCount)
        {
            Assert.That(ruleset.GetModsFor(type).Count(), Is.EqualTo(expectedCount));
        }

        // CF: CatchRuleset.GetModsFor | switch type | default
        [Test]
        public void TestUnsupportedModTypeHasNoMods()
        {
            Assert.That(ruleset.GetModsFor(ModType.Fun + 100), Is.Empty);
        }

        // CF: CatchRuleset.GetDisplayNameForHitResult | switch result | case LargeTickHit
        // CF: CatchRuleset.GetDisplayNameForHitResult | switch result | case SmallTickHit
        // CF: CatchRuleset.GetDisplayNameForHitResult | switch result | case LargeBonus
        [Test]
        public void TestHitResultDisplayNames()
        {
            Assert.That(ruleset.GetDisplayNameForHitResult(HitResult.LargeTickHit).ToString(), Is.EqualTo("Large droplet"));
            Assert.That(ruleset.GetDisplayNameForHitResult(HitResult.SmallTickHit).ToString(), Is.EqualTo("Small droplet"));
            Assert.That(ruleset.GetDisplayNameForHitResult(HitResult.LargeBonus).ToString(), Is.EqualTo("Banana"));
        }

        // CF: CatchRuleset.GetDisplayNameForHitResult | switch result | default (falls through to base)
        [Test]
        public void TestOtherHitResultsUseBaseName()
        {
            Assert.That(ruleset.GetDisplayNameForHitResult(HitResult.Great).ToString(), Is.Not.Empty);
        }

        [Test]
        public void TestValidHitResultsContainCatchResults()
        {
            Assert.That(ruleset.GetValidHitResults(), Does.Contain(HitResult.LargeBonus).And.Contain(HitResult.SmallTickHit).And.Not.Contain(HitResult.Ok));
        }

        [Test]
        public void TestFactoryMethodsReturnCatchTypes()
        {
            Assert.That(ruleset.LegacyID, Is.EqualTo(2));
            Assert.That(ruleset.CreateScoreProcessor(), Is.TypeOf<CatchScoreProcessor>());
            Assert.That(ruleset.CreateHealthProcessor(0), Is.TypeOf<CatchHealthProcessor>());
            Assert.That(ruleset.CreatePerformanceCalculator(), Is.TypeOf<CatchPerformanceCalculator>());
            Assert.That(ruleset.CreateConvertibleReplayFrame(), Is.TypeOf<CatchReplayFrame>());
        }

        [Test]
        public void TestAdjustedDisplayDifficultyFollowsRate()
        {
            var info = new BeatmapInfo { Difficulty = new BeatmapDifficulty { ApproachRate = 5 } };

            float none = ruleset.GetAdjustedDisplayDifficulty(info, Array.Empty<Mod>()).ApproachRate;
            float faster = ruleset.GetAdjustedDisplayDifficulty(info, new Mod[] { new CatchModDoubleTime() }).ApproachRate;
            float slower = ruleset.GetAdjustedDisplayDifficulty(info, new Mod[] { new CatchModHalfTime() }).ApproachRate;

            Assert.That(none, Is.EqualTo(5).Within(1e-3));
            Assert.That(faster, Is.GreaterThan(none));
            Assert.That(slower, Is.LessThan(none));
        }

        [Test]
        public void TestBeatmapAttributesForDisplay()
        {
            var info = new BeatmapInfo { Difficulty = new BeatmapDifficulty { CircleSize = 4, ApproachRate = 5, DrainRate = 6 } };

            var attributes = ruleset.GetBeatmapAttributesForDisplay(info, Array.Empty<Mod>()).ToArray();

            Assert.That(attributes.Select(a => a.Acronym), Is.EqualTo(new[] { "CS", "AR", "HP" }));
            Assert.That(attributes[0].OriginalValue, Is.EqualTo(4));
            Assert.That(attributes[2].OriginalValue, Is.EqualTo(6));
        }
    }
}
