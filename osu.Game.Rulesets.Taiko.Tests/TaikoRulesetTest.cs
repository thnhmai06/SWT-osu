// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Input.Bindings;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko.Mods;

namespace osu.Game.Rulesets.Taiko.Tests
{
    [TestFixture]
    public class TaikoRulesetTest
    {
        private readonly TaikoRuleset ruleset = new TaikoRuleset();

        private Type[] convert(LegacyMods mods) => ruleset.ConvertFromLegacyMods(mods).Select(m => m.GetType()).ToArray();

        // CF: TaikoRuleset.ConvertFromLegacyMods | mods.HasFlag(LegacyMods.Nightcore) | TRUE
        // CF: TaikoRuleset.ConvertFromLegacyMods | mods.HasFlag(LegacyMods.DoubleTime) | FALSE
        // CF: TaikoRuleset.ConvertFromLegacyMods | mods.HasFlag(LegacyMods.Perfect) | TRUE
        // CF: TaikoRuleset.ConvertFromLegacyMods | mods.HasFlag(LegacyMods.Cinema) | TRUE
        [Test]
        public void TestNightcorePerfectCinemaTakePrecedence()
            => Assert.That(convert(LegacyMods.Nightcore | LegacyMods.DoubleTime | LegacyMods.Perfect | LegacyMods.SuddenDeath | LegacyMods.Cinema | LegacyMods.Autoplay),
                Is.EqualTo(new[] { typeof(TaikoModNightcore), typeof(TaikoModPerfect), typeof(TaikoModCinema) }));

        // CF: TaikoRuleset.ConvertFromLegacyMods | mods.HasFlag(LegacyMods.Nightcore) | FALSE
        // CF: TaikoRuleset.ConvertFromLegacyMods | mods.HasFlag(LegacyMods.DoubleTime) | TRUE
        // CF: TaikoRuleset.ConvertFromLegacyMods | mods.HasFlag(LegacyMods.Perfect) | FALSE
        // CF: TaikoRuleset.ConvertFromLegacyMods | mods.HasFlag(LegacyMods.SuddenDeath) | TRUE
        // CF: TaikoRuleset.ConvertFromLegacyMods | mods.HasFlag(LegacyMods.Cinema) | FALSE
        // CF: TaikoRuleset.ConvertFromLegacyMods | mods.HasFlag(LegacyMods.Autoplay) | TRUE
        [Test]
        public void TestFallbackVariants()
            => Assert.That(convert(LegacyMods.DoubleTime | LegacyMods.SuddenDeath | LegacyMods.Autoplay),
                Is.EqualTo(new[] { typeof(TaikoModDoubleTime), typeof(TaikoModSuddenDeath), typeof(TaikoModAutoplay) }));

        // CF: TaikoRuleset.ConvertFromLegacyMods | mods.HasFlag(LegacyMods.SuddenDeath) | FALSE
        // CF: TaikoRuleset.ConvertFromLegacyMods | mods.HasFlag(LegacyMods.Autoplay) | FALSE
        // CF: TaikoRuleset.ConvertFromLegacyMods | mods.HasFlag(LegacyMods.Easy) | FALSE
        // CF: TaikoRuleset.ConvertFromLegacyMods | mods.HasFlag(LegacyMods.Flashlight) | FALSE
        // CF: TaikoRuleset.ConvertFromLegacyMods | mods.HasFlag(LegacyMods.HalfTime) | FALSE
        // CF: TaikoRuleset.ConvertFromLegacyMods | mods.HasFlag(LegacyMods.HardRock) | FALSE
        // CF: TaikoRuleset.ConvertFromLegacyMods | mods.HasFlag(LegacyMods.Hidden) | FALSE
        // CF: TaikoRuleset.ConvertFromLegacyMods | mods.HasFlag(LegacyMods.NoFail) | FALSE
        // CF: TaikoRuleset.ConvertFromLegacyMods | mods.HasFlag(LegacyMods.Relax) | FALSE
        // CF: TaikoRuleset.ConvertFromLegacyMods | mods.HasFlag(LegacyMods.ScoreV2) | FALSE
        [Test]
        public void TestNoFlagsGivesNoMods() => Assert.That(convert(LegacyMods.None), Is.Empty);

        // CF: TaikoRuleset.ConvertFromLegacyMods | mods.HasFlag(LegacyMods.Easy) | TRUE
        // CF: TaikoRuleset.ConvertFromLegacyMods | mods.HasFlag(LegacyMods.Flashlight) | TRUE
        // CF: TaikoRuleset.ConvertFromLegacyMods | mods.HasFlag(LegacyMods.HalfTime) | TRUE
        // CF: TaikoRuleset.ConvertFromLegacyMods | mods.HasFlag(LegacyMods.HardRock) | TRUE
        // CF: TaikoRuleset.ConvertFromLegacyMods | mods.HasFlag(LegacyMods.Hidden) | TRUE
        // CF: TaikoRuleset.ConvertFromLegacyMods | mods.HasFlag(LegacyMods.NoFail) | TRUE
        // CF: TaikoRuleset.ConvertFromLegacyMods | mods.HasFlag(LegacyMods.Relax) | TRUE
        // CF: TaikoRuleset.ConvertFromLegacyMods | mods.HasFlag(LegacyMods.ScoreV2) | TRUE
        [Test]
        public void TestSimpleFlags()
            => Assert.That(convert(LegacyMods.Easy | LegacyMods.Flashlight | LegacyMods.HalfTime | LegacyMods.HardRock | LegacyMods.Hidden | LegacyMods.NoFail | LegacyMods.Relax | LegacyMods.ScoreV2),
                Is.EqualTo(new[]
                {
                    typeof(TaikoModEasy), typeof(TaikoModFlashlight), typeof(TaikoModHalfTime), typeof(TaikoModHardRock),
                    typeof(TaikoModHidden), typeof(TaikoModNoFail), typeof(TaikoModRelax), typeof(ModScoreV2)
                }));

        // CF: TaikoRuleset.GetModsFor | switch (type) | case DifficultyReduction
        // CF: TaikoRuleset.GetModsFor | switch (type) | case DifficultyIncrease
        // CF: TaikoRuleset.GetModsFor | switch (type) | case Conversion
        // CF: TaikoRuleset.GetModsFor | switch (type) | case Automation
        // CF: TaikoRuleset.GetModsFor | switch (type) | case Fun
        // CF: TaikoRuleset.GetModsFor | switch (type) | case System
        [TestCase(ModType.DifficultyReduction, typeof(TaikoModEasy))]
        [TestCase(ModType.DifficultyIncrease, typeof(TaikoModHardRock))]
        [TestCase(ModType.Conversion, typeof(TaikoModRandom))]
        [TestCase(ModType.Automation, typeof(TaikoModRelax))]
        [TestCase(ModType.Fun, typeof(TaikoModMuted))]
        [TestCase(ModType.System, typeof(ModScoreV2))]
        public void TestModsPerType(ModType type, Type expectedFirstOrContained)
        {
            var mods = ruleset.GetModsFor(type).SelectMany(m => m is MultiMod multi ? multi.Mods : new[] { m }).ToList();
            Assert.That(mods, Is.Not.Empty);
            Assert.That(mods.Select(m => m.GetType()), Does.Contain(expectedFirstOrContained));
            Assert.That(mods.All(m => m.Type == type), Is.True);
        }

        // CF: TaikoRuleset.GetModsFor | switch (type) | default
        [Test]
        public void TestUnknownModTypeGivesNoMods() => Assert.That(ruleset.GetModsFor((ModType)99), Is.Empty);

        // CF: TaikoRuleset.GetDefaultKeyBindings | switch (variant) | default
        [Test]
        public void TestDefaultKeyBindingsAreDrumKeys()
        {
            var bindings = ruleset.GetDefaultKeyBindings().ToList();
            Assert.That(bindings, Has.Count.EqualTo(8));
            Assert.That(bindings.Select(b => b.Action).Distinct(), Is.EquivalentTo(new object[] { TaikoAction.LeftRim, TaikoAction.LeftCentre, TaikoAction.RightCentre, TaikoAction.RightRim }));
            Assert.That(bindings.Single(b => (TaikoAction)b.Action == TaikoAction.LeftRim && b.KeyCombination.Keys.Contains(InputKey.D)), Is.Not.Null);
        }

        // CF: TaikoRuleset.GetDefaultKeyBindings | switch (variant) | case EDITOR_VARIANT
        [Test]
        public void TestEditorKeyBindingsAreTools()
        {
            var actions = ruleset.GetDefaultKeyBindings(Ruleset.EDITOR_VARIANT).Select(b => (TaikoAction)b.Action).ToList();
            Assert.That(actions, Is.EqualTo(new[] { TaikoAction.EditorHitTool, TaikoAction.EditorDrumRollTool, TaikoAction.EditorSwellTool }));
        }

        // CF: TaikoRuleset.GetDisplayNameForHitResult | case SmallBonus | case SmallBonus
        // CF: TaikoRuleset.GetDisplayNameForHitResult | case LargeBonus | case LargeBonus
        // CF: TaikoRuleset.GetDisplayNameForHitResult | switch (result) | default
        [Test]
        public void TestHitResultDisplayNames()
        {
            Assert.That(ruleset.GetDisplayNameForHitResult(HitResult.SmallBonus).ToString(), Is.EqualTo("drum tick"));
            Assert.That(ruleset.GetDisplayNameForHitResult(HitResult.LargeBonus).ToString(), Is.EqualTo("bonus"));
            Assert.That(ruleset.GetDisplayNameForHitResult(HitResult.Great).ToString(), Is.Not.EqualTo("drum tick").And.Not.EqualTo("bonus").And.Not.Empty);
        }

        [Test]
        public void TestValidHitResultsContainTaikoJudgements()
            => Assert.That(ruleset.GetValidHitResults(), Is.SupersetOf(new[] { HitResult.Great, HitResult.Ok, HitResult.Miss }));

        [Test]
        public void TestIdentity()
        {
            Assert.That(ruleset.ShortName, Is.EqualTo("taiko"));
            Assert.That(ruleset.LegacyID, Is.EqualTo(1));
        }
    }
}
