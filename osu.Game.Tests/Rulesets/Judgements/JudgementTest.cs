// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Tests.Rulesets.Judgements
{
    [TestFixture]
    public class JudgementTest
    {
        private class MaxJudgement : Judgement
        {
            private readonly HitResult max;

            public MaxJudgement(HitResult max) => this.max = max;

            public override HitResult MaxResult => max;
        }

        // CF: Judgement.MinResult | case SmallBonus | case SmallBonus
        // CF: Judgement.MinResult | case LargeBonus | case LargeBonus
        // CF: Judgement.MinResult | case IgnoreHit | case IgnoreHit
        // CF: Judgement.MinResult | case SmallTickHit | case SmallTickHit
        // CF: Judgement.MinResult | case LargeTickHit | case LargeTickHit
        // CF: Judgement.MinResult | case SliderTailHit | case SliderTailHit
        // CF: Judgement.MinResult | default | default
        [TestCase(HitResult.SmallBonus, HitResult.IgnoreMiss)]
        [TestCase(HitResult.LargeBonus, HitResult.IgnoreMiss)]
        [TestCase(HitResult.IgnoreHit, HitResult.IgnoreMiss)]
        [TestCase(HitResult.SmallTickHit, HitResult.SmallTickMiss)]
        [TestCase(HitResult.LargeTickHit, HitResult.LargeTickMiss)]
        [TestCase(HitResult.SliderTailHit, HitResult.IgnoreMiss)]
        [TestCase(HitResult.Great, HitResult.Miss)]
        [TestCase(HitResult.Perfect, HitResult.Miss)]
        public void TestMinResult(HitResult max, HitResult expectedMin)
        {
            Assert.That(new MaxJudgement(max).MinResult, Is.EqualTo(expectedMin));
        }

        // CF: Judgement.HealthIncreaseFor | case SmallTickHit | case SmallTickHit
        // CF: Judgement.HealthIncreaseFor | case SmallTickMiss | case SmallTickMiss
        // CF: Judgement.HealthIncreaseFor | case SliderTailHit | case SliderTailHit
        // CF: Judgement.HealthIncreaseFor | case LargeTickHit | case LargeTickHit
        // CF: Judgement.HealthIncreaseFor | case LargeTickMiss | case LargeTickMiss
        // CF: Judgement.HealthIncreaseFor | case Miss | case Miss
        // CF: Judgement.HealthIncreaseFor | case Meh | case Meh
        // CF: Judgement.HealthIncreaseFor | case Ok | case Ok
        // CF: Judgement.HealthIncreaseFor | case Good | case Good
        // CF: Judgement.HealthIncreaseFor | case Great | case Great
        // CF: Judgement.HealthIncreaseFor | case Perfect | case Perfect
        // CF: Judgement.HealthIncreaseFor | case SmallBonus | case SmallBonus
        // CF: Judgement.HealthIncreaseFor | case LargeBonus | case LargeBonus
        // CF: Judgement.HealthIncreaseFor | default | default
        [TestCase(HitResult.SmallTickHit, 0.025)]
        [TestCase(HitResult.SmallTickMiss, -0.025)]
        [TestCase(HitResult.SliderTailHit, 0.05)]
        [TestCase(HitResult.LargeTickHit, 0.05)]
        [TestCase(HitResult.LargeTickMiss, -0.05)]
        [TestCase(HitResult.Miss, -0.1)]
        [TestCase(HitResult.Meh, 0.0025)]
        [TestCase(HitResult.Ok, 0.025)]
        [TestCase(HitResult.Good, 0.0375)]
        [TestCase(HitResult.Great, 0.05)]
        [TestCase(HitResult.Perfect, 0.0525)]
        [TestCase(HitResult.SmallBonus, 0.025)]
        [TestCase(HitResult.LargeBonus, 0.05)]
        [TestCase(HitResult.None, 0)]
        [TestCase(HitResult.IgnoreHit, 0)]
        public void TestHealthIncrease(HitResult result, double expected)
        {
            var judgement = new Judgement();
            var judgementResult = new JudgementResult(new HitObject(), judgement) { Type = result };

            Assert.That(judgement.HealthIncreaseFor(judgementResult), Is.EqualTo(expected).Within(1e-12));
            Assert.That(judgementResult.HealthIncrease, Is.EqualTo(expected).Within(1e-12));
        }

        // CF: Judgement.MaxHealthIncrease | HealthIncreaseFor(MaxResult) | no throw
        // CF: Judgement.ToString | MaxResult interpolation | no throw
        [Test]
        public void TestMaxHealthIncreaseAndToString()
        {
            Assert.That(new Judgement().MaxHealthIncrease, Is.EqualTo(0.0525).Within(1e-12));
            Assert.That(new MaxJudgement(HitResult.Miss).MaxHealthIncrease, Is.EqualTo(-0.1).Within(1e-12));
            Assert.That(new MaxJudgement(HitResult.Great).ToString(), Is.EqualTo("MaxResult:Great"));
        }
    }
}
