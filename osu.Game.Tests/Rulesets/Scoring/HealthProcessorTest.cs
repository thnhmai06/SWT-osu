// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Tests.Rulesets.Scoring
{
    [TestFixture]
    public class HealthProcessorTest
    {
        private class TestHealthProcessor : HealthProcessor
        {
        }

        private TestHealthProcessor processor = null!;

        [SetUp]
        public void SetUp() => processor = new TestHealthProcessor();

        private static JudgementResult result(HitResult type) => new JudgementResult(new HitObject(), new Judgement()) { Type = type };

        // CF: HealthProcessor.ApplyResultInternal | HasFailed | FALSE
        // CF: HealthProcessor.ApplyResultInternal | meetsAnyFailCondition | FALSE
        // CF: HealthProcessor.meetsAnyFailCondition | CheckDefaultFailCondition | FALSE
        // CF: HealthProcessor.meetsAnyFailCondition | FailConditions != null | FALSE
        [Test]
        public void TestHealthChangesByJudgementIncrease()
        {
            var miss = result(HitResult.Miss);
            processor.ApplyResult(miss);

            Assert.That(miss.HealthAtJudgement, Is.EqualTo(1));
            Assert.That(miss.FailedAtJudgement, Is.False);
            Assert.That(processor.Health.Value, Is.EqualTo(0.9).Within(1e-12));

            processor.ApplyResult(result(HitResult.Great));
            Assert.That(processor.Health.Value, Is.EqualTo(0.95).Within(1e-12));

            processor.ApplyResult(result(HitResult.Perfect));
            Assert.That(processor.Health.Value, Is.EqualTo(1), "health is capped at the maximum");
        }

        // CF: HealthProcessor.meetsAnyFailCondition | CheckDefaultFailCondition | TRUE
        // CF: HealthProcessor.ApplyResultInternal | meetsAnyFailCondition | TRUE
        // CF: HealthProcessor.TriggerFailure | HasFailed | FALSE
        // CF: HealthProcessor.TriggerFailure | Failed?.Invoke() != false | TRUE
        // CF: HealthProcessor.ApplyResultInternal | HasFailed | TRUE
        [Test]
        public void TestFailureWhenHealthDepleted()
        {
            int failedCalls = 0;
            processor.Failed += () =>
            {
                failedCalls++;
                return true;
            };

            for (int i = 0; i < 10; i++)
                processor.ApplyResult(result(HitResult.Miss));

            Assert.That(processor.HasFailed, Is.True);
            Assert.That(failedCalls, Is.EqualTo(1));

            // further judgements neither change health nor re-trigger failure.
            var afterFail = result(HitResult.Great);
            processor.ApplyResult(afterFail);

            Assert.That(afterFail.FailedAtJudgement, Is.True);
            Assert.That(processor.Health.Value, Is.EqualTo(0).Within(1e-9));
            Assert.That(failedCalls, Is.EqualTo(1));
        }

        // CF: HealthProcessor.TriggerFailure | HasFailed | TRUE
        [Test]
        public void TestTriggerFailureIsIdempotent()
        {
            int calls = 0;
            processor.Failed += () =>
            {
                calls++;
                return true;
            };

            processor.TriggerFailure();
            processor.TriggerFailure();

            Assert.That(calls, Is.EqualTo(1));
        }

        // CF: HealthProcessor.TriggerFailure | Failed?.Invoke() != false | FALSE
        // CF: HealthProcessor.TriggerFailure | Failed == null | null
        [Test]
        public void TestFailureCanBeVetoed()
        {
            processor.Failed += () => false;
            processor.TriggerFailure();
            Assert.That(processor.HasFailed, Is.False);

            var noHandlers = new TestHealthProcessor();
            noHandlers.TriggerFailure();
            Assert.That(noHandlers.HasFailed, Is.True, "no veto without handlers");
        }

        // CF: HealthProcessor.meetsAnyFailCondition | FailConditions != null | TRUE
        // CF: HealthProcessor.meetsAnyFailCondition | foreach condition | loop n
        // CF: HealthProcessor.meetsAnyFailCondition | conditionResult | TRUE
        // CF: HealthProcessor.meetsAnyFailCondition | conditionResult | FALSE
        [Test]
        public void TestCustomFailConditions()
        {
            int evaluated = 0;
            processor.FailConditions += (_, _) =>
            {
                evaluated++;
                return false;
            };
            processor.FailConditions += (_, r) =>
            {
                evaluated++;
                return r.Type == HitResult.Meh;
            };

            processor.ApplyResult(result(HitResult.Great));
            Assert.That(processor.HasFailed, Is.False);
            Assert.That(evaluated, Is.EqualTo(2));

            processor.ApplyResult(result(HitResult.Meh));
            Assert.That(processor.HasFailed, Is.True);
        }

        // CF: HealthProcessor.RevertResultInternal | HasFailed | FALSE
        // CF: HealthProcessor.RevertResultInternal | HasFailed | TRUE
        [Test]
        public void TestRevert()
        {
            var first = result(HitResult.Miss);
            processor.ApplyResult(first);
            processor.RevertResult(first);

            Assert.That(processor.Health.Value, Is.EqualTo(1));
            Assert.That(processor.HasFailed, Is.False);

            // revert of a result that was applied after failure must keep the failed state.
            processor.TriggerFailure();
            var late = result(HitResult.Great);
            processor.ApplyResult(late);
            processor.RevertResult(late);

            Assert.That(processor.HasFailed, Is.True);
        }

        // CF: HealthProcessor.RevertResultInternal | revert restores failed state of earlier result | no throw
        [Test]
        public void TestRevertingTheFailingResultRecoversFromFailure()
        {
            for (int i = 0; i < 9; i++)
                processor.ApplyResult(result(HitResult.Miss));

            var failing = result(HitResult.Miss);
            processor.ApplyResult(failing);
            Assert.That(processor.HasFailed, Is.True);

            processor.RevertResult(failing);

            Assert.That(processor.HasFailed, Is.False, "the failing result itself was applied while not yet failed");
            Assert.That(processor.Health.Value, Is.EqualTo(failing.HealthAtJudgement));
        }

        // CF: HealthProcessor.Reset | Health.Value = 1; HasFailed = false | no throw
        [Test]
        public void TestApplyBeatmapResets()
        {
            processor.ApplyResult(result(HitResult.Miss));
            processor.TriggerFailure();

            processor.ApplyBeatmap(new Beatmap());

            Assert.That(processor.Health.Value, Is.EqualTo(1));
            Assert.That(processor.HasFailed, Is.False);
        }
    }
}
