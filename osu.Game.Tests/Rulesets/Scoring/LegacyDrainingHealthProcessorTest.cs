// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Tests.Rulesets.Scoring
{
    [TestFixture]
    public class LegacyDrainingHealthProcessorTest
    {
        private class TestProcessor : LegacyDrainingHealthProcessor
        {
            private readonly double increase;
            private readonly bool scaleByMultiplier;
            private IBeatmap? beatmap;

            /// <param name="increase">The health increase granted per hit object.</param>
            /// <param name="scaleByMultiplier">
            /// Whether the increase is multiplied by <see cref="LegacyDrainingHealthProcessor.HpMultiplierNormal"/>, like real rulesets do.
            /// Required for the drain computation to be able to converge when recovery is insufficient.
            /// </param>
            public TestProcessor(double increase = 0.05, bool scaleByMultiplier = false)
                : base(0)
            {
                this.increase = increase;
                this.scaleByMultiplier = scaleByMultiplier;
            }

            public override void ApplyBeatmap(IBeatmap beatmap)
            {
                this.beatmap = beatmap;
                base.ApplyBeatmap(beatmap);
            }

            public double Multiplier => HpMultiplierNormal;

            protected override IEnumerable<HitObject> EnumerateTopLevelHitObjects() => beatmap!.HitObjects;

            protected override IEnumerable<HitObject> EnumerateNestedHitObjects(HitObject hitObject) => hitObject.NestedHitObjects;

            protected override double GetHealthIncreaseFor(HitObject hitObject, HitResult result) => scaleByMultiplier ? increase * HpMultiplierNormal : increase;
        }

        private static Beatmap beatmapOf(float drainRate, params double[] times)
        {
            var b = new Beatmap();
            b.Difficulty.DrainRate = drainRate;
            foreach (double t in times)
                b.HitObjects.Add(new HitObject { StartTime = t });
            return b;
        }

        // CF: LegacyDrainingHealthProcessor.ComputeDrainRate | topLevelObjectCount == 0 | TRUE
        [Test]
        public void TestEmptyBeatmapKeepsInitialTestDrop()
        {
            var processor = new TestProcessor();
            processor.ApplyBeatmap(beatmapOf(5));

            Assert.That(processor.DrainRate, Is.EqualTo(0.00025));
            Assert.That(processor.Multiplier, Is.EqualTo(1));
        }

        // CF: LegacyDrainingHealthProcessor.ComputeDrainRate | topLevelObjectCount == 0 | FALSE
        // CF: LegacyDrainingHealthProcessor.ComputeDrainRate | !fail | TRUE
        // CF: LegacyDrainingHealthProcessor.ComputeDrainRate | foreach top level object | loop n
        // CF: LegacyDrainingHealthProcessor.ComputeDrainRate | foreach nested object | loop 0
        [Test]
        public void TestSuccessfulIterationReportsPassedDrop()
        {
            var processor = new TestProcessor(0.05);
            var successes = new List<string>();
            processor.OnIterationSuccess = successes.Add;

            processor.ApplyBeatmap(beatmapOf(5, 0, 500, 1000, 1500, 2000));

            Assert.That(successes.Count(s => s.StartsWith("PASSED drop", System.StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(processor.DrainRate, Is.GreaterThan(0));
        }

        // CF: LegacyDrainingHealthProcessor.ComputeDrainRate | currentHp <= lowestHpEver | TRUE
        [Test]
        public void TestTooHarshDrainIsReducedUntilItPasses()
        {
            var processor = new TestProcessor(0.1);
            var failures = new List<string>();
            processor.OnIterationFail = failures.Add;

            // at the initial 0.00025/ms a 4s gap drains 100% of health, which fails the "lowest hp ever" requirement.
            processor.ApplyBeatmap(beatmapOf(10, 0, 4000, 4500, 5000));

            Assert.That(failures, Is.Not.Empty);
            Assert.That(failures[0], Does.Contain("hp too low"));
            Assert.That(processor.DrainRate, Is.LessThan(0.00025));
        }

        // CF: LegacyDrainingHealthProcessor.ComputeDrainRate | currentBreak < Breaks.Count && Breaks[currentBreak].EndTime <= h.StartTime | TRUE
        [Test]
        public void TestBreaksPreventDrain()
        {
            var withBreak = beatmapOf(10, 0, 4000);
            withBreak.Breaks.Add(new BreakPeriod(200, 3900));
            var processor = new TestProcessor(0.1);
            var failures = new List<string>();
            processor.OnIterationFail = failures.Add;

            processor.ApplyBeatmap(withBreak);

            Assert.That(failures.Any(f => f.Contains("hp too low")), Is.False, "the 3.7s break is not drained");
        }

        // CF: LegacyDrainingHealthProcessor.ComputeDrainRate | !fail && currentHp < lowestHpEnd | TRUE
        // CF: LegacyDrainingHealthProcessor.ComputeDrainRate | !fail && recovery < hpRecoveryAvailable | TRUE
        [Test]
        public void TestInsufficientRecoveryRaisesHpMultiplier()
        {
            var processor = new TestProcessor(0.0001, scaleByMultiplier: true);
            var failures = new List<string>();
            processor.OnIterationFail = failure =>
            {
                // guard: ComputeDrainRate loops until it converges; abort instead of exhausting memory if it ever stops converging.
                if (failures.Count > 5000)
                    throw new System.InvalidOperationException("drain computation did not converge");

                failures.Add(failure);
            };

            processor.ApplyBeatmap(beatmapOf(5, 0, 1000, 2000, 3000));

            Assert.That(failures.Any(f => f.Contains("recovery too low") || f.Contains("end hp too low")), Is.True);
            Assert.That(processor.Multiplier, Is.GreaterThan(1));
        }

        // CF: LegacyDrainingHealthProcessor.Reset | HpMultiplierNormal = 1 | no throw
        [Test]
        public void TestResetRestoresMultiplier()
        {
            int iterations = 0;
            var processor = new TestProcessor(0.0001, scaleByMultiplier: true);
            processor.OnIterationFail = _ =>
            {
                // guard: abort instead of exhausting memory if the drain computation stops converging.
                if (++iterations > 5000)
                    throw new System.InvalidOperationException("drain computation did not converge");
            };
            processor.ApplyBeatmap(beatmapOf(5, 0, 1000, 2000, 3000));
            Assert.That(processor.Multiplier, Is.GreaterThan(1), "precondition: the first beatmap raised the multiplier");

            processor.ApplyBeatmap(beatmapOf(5));

            Assert.That(processor.Multiplier, Is.EqualTo(1));
        }

        // CF: LegacyDrainingHealthProcessor.GetHealthIncreaseFor | sealed override delegates | no throw
        [Test]
        public void TestResultHealthIncreaseDelegatesToHitObjectOverload()
        {
            var processor = new TestProcessor(0.07);
            var obj = new HitObject();
            var result = new JudgementResult(obj, obj.Judgement) { Type = HitResult.Great };

            processor.ApplyResult(result);

            Assert.That(processor.Health.Value, Is.EqualTo(1), "capped at 1 after adding 0.07");

            processor.Health.Value = 0.5;
            processor.ApplyResult(new JudgementResult(obj, obj.Judgement) { Type = HitResult.Great });
            Assert.That(processor.Health.Value, Is.EqualTo(0.57).Within(1e-9));
        }
    }
}
