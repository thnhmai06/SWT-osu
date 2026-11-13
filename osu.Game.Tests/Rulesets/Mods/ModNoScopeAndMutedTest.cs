// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Tests.Rulesets.Mods
{
    [TestFixture]
    public class ModNoScopeAndMutedTest
    {
        private class TestNoScope : ModNoScope
        {
            public override osu.Framework.Localisation.LocalisableString Description => "test";
            public override BindableInt HiddenComboCount { get; } = new BindableInt(100);
            public float Alpha => ComboBasedAlpha;
            public float MinAlpha => MIN_ALPHA;
        }

        private static (TestNoScope mod, ScoreProcessor processor) apply(int hiddenAtCombo)
        {
            var mod = new TestNoScope();
            mod.HiddenComboCount.Value = hiddenAtCombo;
            var processor = new ScoreProcessor(new OsuRuleset());
            mod.ApplyToScoreProcessor(processor);
            return (mod, processor);
        }

        // CF: ModNoScope.ApplyToScoreProcessor | HiddenComboCount.Value == 0 | TRUE
        [Test]
        public void TestZeroHiddenComboCountNeverTracksCombo()
        {
            var (mod, processor) = apply(0);

            processor.Combo.Value = 50;

            Assert.That(mod.Alpha, Is.EqualTo(0f), "alpha is never computed (field default)");
        }

        // CF: ModNoScope.ApplyToScoreProcessor | HiddenComboCount.Value == 0 | FALSE
        [Test]
        public void TestAlphaFadesLinearlyWithCombo()
        {
            var (mod, processor) = apply(100);

            Assert.That(mod.Alpha, Is.EqualTo(1f), "initial combo 0");

            processor.Combo.Value = 25;
            Assert.That(mod.Alpha, Is.EqualTo(0.75f).Within(1e-6));

            processor.Combo.Value = 100;
            Assert.That(mod.Alpha, Is.EqualTo(mod.MinAlpha), "never fully zero");

            processor.Combo.Value = 500;
            Assert.That(mod.Alpha, Is.EqualTo(mod.MinAlpha));
        }

        [Test]
        public void TestNoScopeRankUnchanged()
        {
            Assert.That(new TestNoScope().AdjustRank(osu.Game.Scoring.ScoreRank.B, 0.5), Is.EqualTo(osu.Game.Scoring.ScoreRank.B));
        }

        // CF: ModNoScope.GetSettingTooltipText | ReferenceEquals(bindable, HiddenComboCount) | TRUE
        [TestCase(0, "always hidden")]
        [TestCase(30, "30")]
        public void TestHiddenComboFormatting(int value, string expected)
        {
            Assert.That(HiddenComboSlider.FormatHiddenComboValue(value).ToString(), Is.EqualTo(expected));
        }

        [TestCase(0, "always muted")]
        [TestCase(30, "30")]
        public void TestMuteComboFormatting(int value, string expected)
        {
            Assert.That(MuteComboSlider.FormatMuteComboValue(value).ToString(), Is.EqualTo(expected));
        }

        // CF: ModMuted.ctor | InverseMuting changes MuteComboCount.MinValue | TRUE
        // CF: ModMuted.ctor | InverseMuting changes MuteComboCount.MinValue | FALSE
        [Test]
        public void TestInverseMutingRequiresAtLeastOneCombo()
        {
            var mod = new OsuModMuted();
            Assert.That(mod.MuteComboCount.MinValue, Is.EqualTo(0));

            mod.InverseMuting.Value = true;
            Assert.That(mod.MuteComboCount.MinValue, Is.EqualTo(1));
            Assert.That(mod.MuteComboCount.Value, Is.GreaterThanOrEqualTo(1));

            mod.InverseMuting.Value = false;
            Assert.That(mod.MuteComboCount.MinValue, Is.EqualTo(0));
        }
    }
}
