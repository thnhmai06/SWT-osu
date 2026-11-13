// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Tests.Rulesets.Mods
{
    [TestFixture]
    public class ModAccuracyChallengeTest
    {
        private ModAccuracyChallenge mod = null!;
        private ScoreProcessor scoreProcessor = null!;
        private HealthProcessor healthProcessor = null!;
        private int failures;

        [SetUp]
        public void SetUp()
        {
            mod = new ModAccuracyChallenge();
            scoreProcessor = new ScoreProcessor(new OsuRuleset());
            healthProcessor = new DrainingHealthProcessor(0);
            failures = 0;
            healthProcessor.Failed += () =>
            {
                failures++;
                return true;
            };
        }

        private void apply(ModAccuracyChallenge.AccuracyMode judgeMode, bool bindHealthProcessor = true)
        {
            mod.AccuracyJudgeMode.Value = judgeMode;
            if (bindHealthProcessor) mod.ApplyToHealthProcessor(healthProcessor);
            mod.ApplyToScoreProcessor(scoreProcessor);
        }

        // CF: ModAccuracyChallenge.ApplyToScoreProcessor | switch (AccuracyJudgeMode.Value) | case Standard
        // CF: ModAccuracyChallenge.ApplyToScoreProcessor | s.NewValue < MinimumAccuracy.Value | TRUE
        [Test]
        public void TestStandardModeFailsWhenAccuracyDropsBelowMinimum()
        {
            apply(ModAccuracyChallenge.AccuracyMode.Standard);

            scoreProcessor.Accuracy.Value = 0.85;

            Assert.That(failures, Is.EqualTo(1));
            Assert.That(healthProcessor.HasFailed, Is.True);
        }

        // CF: ModAccuracyChallenge.ApplyToScoreProcessor | s.NewValue < MinimumAccuracy.Value | FALSE
        [Test]
        public void TestStandardModeToleratesAccuracyAtOrAboveMinimum()
        {
            apply(ModAccuracyChallenge.AccuracyMode.Standard);

            scoreProcessor.Accuracy.Value = 0.9;
            scoreProcessor.Accuracy.Value = 0.95;

            Assert.That(failures, Is.Zero);
        }

        // CF: ModAccuracyChallenge.ApplyToScoreProcessor | switch (AccuracyJudgeMode.Value) | case MaximumAchievable
        [Test]
        public void TestMaximumAchievableModeWatchesMaximumAccuracyOnly()
        {
            apply(ModAccuracyChallenge.AccuracyMode.MaximumAchievable);

            scoreProcessor.Accuracy.Value = 0.1;
            Assert.That(failures, Is.Zero, "standard accuracy is ignored in this mode");

            scoreProcessor.MaximumAccuracy.Value = 0.85;
            Assert.That(failures, Is.EqualTo(1));
        }

        [Test]
        public void TestStandardModeIgnoresMaximumAccuracy()
        {
            apply(ModAccuracyChallenge.AccuracyMode.Standard);

            scoreProcessor.MaximumAccuracy.Value = 0.1;

            Assert.That(failures, Is.Zero);
        }

        [Test]
        public void TestMinimumAccuracyIsConfigurable()
        {
            mod.MinimumAccuracy.Value = 0.7;
            apply(ModAccuracyChallenge.AccuracyMode.Standard);

            scoreProcessor.Accuracy.Value = 0.8;
            Assert.That(failures, Is.Zero);

            scoreProcessor.Accuracy.Value = 0.65;
            Assert.That(failures, Is.EqualTo(1));
        }

        [Test]
        public void TestWithoutHealthProcessorFailureIsNoOp()
        {
            apply(ModAccuracyChallenge.AccuracyMode.Standard, false);

            Assert.That(() => scoreProcessor.Accuracy.Value = 0.5, Throws.Nothing);
            Assert.That(failures, Is.Zero);
        }

        [Test]
        public void TestMiscellaneousProperties()
        {
            Assert.That(mod.AdjustRank(ScoreRank.A, 0.5), Is.EqualTo(ScoreRank.A));
            Assert.That(mod.IncompatibleMods, Is.SupersetOf(new[] { typeof(ModEasyWithExtraLives), typeof(ModPerfect), typeof(ModNoFail), typeof(ModCinema) }));
        }

        // CF: ModAccuracyChallenge.SettingDescription | !MinimumAccuracy.IsDefault | FALSE
        // CF: ModAccuracyChallenge.SettingDescription | !AccuracyJudgeMode.IsDefault | FALSE
        // CF: ModAccuracyChallenge.SettingDescription | !Restart.IsDefault | FALSE
        [Test]
        public void TestNoSettingDescriptionByDefault()
        {
            Assert.That(mod.SettingDescription, Is.Empty);
        }

        // CF: ModAccuracyChallenge.SettingDescription | !MinimumAccuracy.IsDefault | TRUE
        // CF: ModAccuracyChallenge.SettingDescription | !AccuracyJudgeMode.IsDefault | TRUE
        // CF: ModAccuracyChallenge.SettingDescription | !Restart.IsDefault | TRUE
        [Test]
        public void TestSettingDescriptionListsChangedSettings()
        {
            mod.MinimumAccuracy.Value = 0.8;
            mod.AccuracyJudgeMode.Value = ModAccuracyChallenge.AccuracyMode.Standard;
            mod.Restart.Value = true;

            var settings = mod.SettingDescription;

            Assert.That(System.Linq.Enumerable.Select(settings, s => s.setting.ToString()), Is.EqualTo(new[] { "Minimum accuracy", "Accuracy mode", "Restart on fail" }));
        }
    }
}
