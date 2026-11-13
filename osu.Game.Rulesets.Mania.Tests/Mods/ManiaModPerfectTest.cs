// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Mania.Judgements;
using osu.Game.Rulesets.Mania.Mods;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mania.Scoring;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Mania.Tests.Mods
{
    [TestFixture]
    public class ManiaModPerfectTest
    {
        private class ExposedPerfect : ManiaModPerfect
        {
            public bool Fails(JudgementResult result) => FailCondition(new ManiaHealthProcessor(0), result);
        }

        private class CustomJudgement : Judgement
        {
            private readonly HitResult min;
            private readonly HitResult max;

            public CustomJudgement(HitResult min, HitResult max)
            {
                this.min = min;
                this.max = max;
            }

            public override HitResult MinResult => min;
            public override HitResult MaxResult => max;
        }

        private static JudgementResult result(Judgement judgement, HitResult type) => new JudgementResult(new Note(), judgement) { Type = type };

        // CF: ManiaModPerfect.FailCondition | !isRelevantResult(Min) && !isRelevantResult(Max) && !isRelevantResult(Type) | TRUE
        // CF: ManiaModPerfect.isRelevantResult | AffectsAccuracy() || AffectsCombo() | FALSE
        [Test]
        public void TestIrrelevantJudgementNeverFails()
        {
            var judgement = new CustomJudgement(HitResult.IgnoreMiss, HitResult.IgnoreHit);
            Assert.That(new ExposedPerfect().Fails(result(judgement, HitResult.IgnoreMiss)), Is.False);
        }

        // CF: ManiaModPerfect.FailCondition | !isRelevantResult(Min) | FALSE
        // CF: ManiaModPerfect.isRelevantResult | AffectsAccuracy() | TRUE
        // CF: ManiaModPerfect.FailCondition | result.Judgement.MaxResult == Perfect | TRUE
        // CF: ManiaModPerfect.FailCondition | !RequirePerfectHits.Value | TRUE
        // CF: ManiaModPerfect.FailCondition | result.Type < Great | TRUE
        // CF: ManiaModPerfect.FailCondition | result.Type < Great | FALSE
        [TestCase(HitResult.Perfect, false)]
        [TestCase(HitResult.Great, false)]
        [TestCase(HitResult.Good, true)]
        [TestCase(HitResult.Miss, true)]
        public void TestDefaultAllowsGreats(HitResult type, bool expectedFail)
        {
            Assert.That(new ExposedPerfect().Fails(result(new ManiaJudgement(), type)), Is.EqualTo(expectedFail));
        }

        // CF: ManiaModPerfect.FailCondition | !RequirePerfectHits.Value | FALSE
        // CF: ManiaModPerfect.FailCondition | result.Type != result.Judgement.MaxResult | TRUE
        // CF: ManiaModPerfect.FailCondition | result.Type != result.Judgement.MaxResult | FALSE
        [TestCase(HitResult.Perfect, false)]
        [TestCase(HitResult.Great, true)]
        public void TestRequirePerfectHits(HitResult type, bool expectedFail)
        {
            var mod = new ExposedPerfect { RequirePerfectHits = { Value = true } };
            Assert.That(mod.Fails(result(new ManiaJudgement(), type)), Is.EqualTo(expectedFail));
        }

        // CF: ManiaModPerfect.FailCondition | result.Judgement.MaxResult == Perfect | FALSE
        // CF: ManiaModPerfect.FailCondition | !isRelevantResult(Max) | FALSE
        // CF: ManiaModPerfect.isRelevantResult | AffectsAccuracy() || AffectsCombo() | TRUE
        [Test]
        public void TestNonPerfectMaxResultMustBeReachedExactly()
        {
            var judgement = new CustomJudgement(HitResult.IgnoreMiss, HitResult.Great);
            var mod = new ExposedPerfect();

            Assert.That(mod.Fails(result(judgement, HitResult.Great)), Is.False);
            Assert.That(mod.Fails(result(judgement, HitResult.Good)), Is.True);
        }

        // CF: ManiaModPerfect.FailCondition | !isRelevantResult(Type) | FALSE
        // CF: ManiaModPerfect.isRelevantResult | AffectsCombo() | TRUE
        [Test]
        public void TestRelevantOnlyThroughResultType()
        {
            var judgement = new CustomJudgement(HitResult.IgnoreMiss, HitResult.IgnoreHit);
            Assert.That(new ExposedPerfect().Fails(result(judgement, HitResult.Miss)), Is.True);
        }
    }
}
