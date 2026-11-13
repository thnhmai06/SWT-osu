// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Mania.Judgements;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Mania.Tests.Judgements
{
    [TestFixture]
    public class ManiaJudgementTest
    {
        private static double healthFor(HitResult result)
        {
            var judgement = new ManiaJudgement();
            return judgement.HealthIncreaseFor(new JudgementResult(new Note(), judgement) { Type = result });
        }

        // CF: ManiaJudgement.HealthIncreaseFor | case Meh | case Meh
        // CF: ManiaJudgement.HealthIncreaseFor | case Ok | case Ok
        // CF: ManiaJudgement.HealthIncreaseFor | case Good | case Good
        // CF: ManiaJudgement.HealthIncreaseFor | case Great | case Great
        // CF: ManiaJudgement.HealthIncreaseFor | case Perfect | case Perfect
        // CF: ManiaJudgement.HealthIncreaseFor | switch(result) no case matches | default
        [Test]
        public void TestHealthIncreases()
        {
            const double max = 0.05; // Judgement.DEFAULT_MAX_HEALTH_INCREASE (protected)

            Assert.That(healthFor(HitResult.Meh), Is.EqualTo(-max * 0.5).Within(1e-12));
            Assert.That(healthFor(HitResult.Ok), Is.EqualTo(-max * 0.3).Within(1e-12));
            Assert.That(healthFor(HitResult.Good), Is.EqualTo(max * 0.1).Within(1e-12));
            Assert.That(healthFor(HitResult.Great), Is.EqualTo(max * 0.8).Within(1e-12));
            Assert.That(healthFor(HitResult.Perfect), Is.EqualTo(max).Within(1e-12));
            Assert.That(healthFor(HitResult.Miss), Is.EqualTo(-max * 2).Within(1e-12));
        }

        // CF: HoldNoteBodyJudgement.MaxResult | constant | non-null
        // CF: HoldNoteBodyJudgement.MinResult | constant | non-null
        [Test]
        public void TestHoldNoteBodyJudgementResults()
        {
            var j = new HoldNoteBodyJudgement();
            Assert.That(j.MaxResult, Is.EqualTo(HitResult.IgnoreHit));
            Assert.That(j.MinResult, Is.EqualTo(HitResult.ComboBreak));
        }
    }
}
