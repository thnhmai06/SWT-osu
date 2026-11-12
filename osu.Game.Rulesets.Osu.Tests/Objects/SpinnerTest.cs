// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using System.Threading;
using NUnit.Framework;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Osu.Judgements;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Objects
{
    [TestFixture]
    public class SpinnerTest
    {
        // OD 5 => clear at 150 rpm (2.5 rps), full bonus at 380 rpm.
        private static readonly BeatmapDifficulty difficulty = new BeatmapDifficulty { OverallDifficulty = 5 };

        private static Spinner create(double duration, CancellationToken token = default)
        {
            var spinner = new Spinner { StartTime = 1000, Duration = duration };
            spinner.ApplyDefaults(new ControlPointInfo(), difficulty, token);
            return spinner;
        }

        [Test]
        public void TestSpinsRequired_FollowsClearRpm()
        {
            Assert.That(create(2000).SpinsRequired, Is.EqualTo(5));
        }

        [Test]
        public void TestSpinsRequired_FloatingPointErrorTolerated()
        {
            // 2.5 rps * 0.39999s = 0.999975, the 0.0001 tolerance lifts it to 1
            Assert.That(create(399.99).SpinsRequired, Is.EqualTo(1));
        }

        [Test]
        public void TestSpinsRequired_JustBelowToleranceNotRounded()
        {
            Assert.That(create(399.9).SpinsRequired, Is.EqualTo(0));
        }

        [Test]
        public void TestMaximumBonusSpins_LongSpinner_IsPositive()
        {
            // Math.Max(0, 12 - 5 - 2): positive operand wins
            Assert.That(create(2000).MaximumBonusSpins, Is.EqualTo(5));
        }

        [TestCase(500)]
        [TestCase(100)]
        [TestCase(0)]
        public void TestMaximumBonusSpins_ShortSpinner_ClampedToZero(double duration)
        {
            // Math.Max(0, <= 0): zero wins
            Assert.That(create(duration).MaximumBonusSpins, Is.EqualTo(0));
        }

        [Test]
        public void TestSpinsRequiredForBonus_AddsGap()
        {
            Assert.That(create(2000).SpinsRequiredForBonus, Is.EqualTo(7));
        }

        // ---- CreateNestedHitObjects: `i < SpinsRequiredForBonus` TRUE / FALSE ----

        [Test]
        public void TestNested_SplitsIntoNormalAndBonusTicks()
        {
            var spinner = create(2000);

            var normal = spinner.NestedHitObjects.Where(h => h.GetType() == typeof(SpinnerTick)).ToList();
            var bonus = spinner.NestedHitObjects.OfType<SpinnerBonusTick>().ToList();

            Assert.That(normal.Count, Is.EqualTo(7));
            Assert.That(bonus.Count, Is.EqualTo(5));
            Assert.That(spinner.NestedHitObjects.Count, Is.EqualTo(12));
        }

        [Test]
        public void TestNested_BonusTicksComeAfterNormalTicksAndEndAtSpinnerEnd()
        {
            var spinner = create(2000);

            Assert.That(spinner.NestedHitObjects.Last(), Is.TypeOf<SpinnerBonusTick>());
            Assert.That(spinner.NestedHitObjects.Last().StartTime, Is.EqualTo(spinner.EndTime).Within(1e-3));
            Assert.That(spinner.NestedHitObjects.Select(h => h.StartTime), Is.Ordered);
        }

        [Test]
        public void TestNested_ShortSpinner_HasNoBonusTicks()
        {
            // only the `true` arm of the ternary is used
            var spinner = create(0);

            Assert.That(spinner.NestedHitObjects.OfType<SpinnerBonusTick>(), Is.Empty);
            Assert.That(spinner.NestedHitObjects.Count, Is.EqualTo(2));
        }

        [Test]
        public void TestNested_BonusTicksCarrySpinnerBonusSample()
        {
            var bonus = create(2000).NestedHitObjects.OfType<SpinnerBonusTick>().First();

            Assert.That(bonus.Samples.Single().Name, Is.EqualTo("spinnerbonus"));
            Assert.That(bonus.SpinnerDuration, Is.EqualTo(2000));
        }

        [Test]
        public void TestNested_CancelledToken_Throws()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<System.OperationCanceledException>(() => create(2000, cts.Token));
        }

        // ---- misc members ----

        [Test]
        public void TestEndTime_GetterAndSetter()
        {
            var spinner = new Spinner { StartTime = 1000, Duration = 500 };
            Assert.That(spinner.EndTime, Is.EqualTo(1500));

            spinner.EndTime = 2500;
            Assert.That(spinner.Duration, Is.EqualTo(1500));
        }

        [Test]
        public void TestStackOffset_IsAlwaysZero()
        {
            Assert.That(new Spinner { StackHeight = 4, Scale = 1 }.StackOffset, Is.EqualTo(Vector2.Zero));
        }

        [Test]
        public void TestSpinningSamples_NoSamples_Empty()
        {
            // referenceSample == null TRUE
            Assert.That(new Spinner().CreateSpinningSamples(), Is.Empty);
            Assert.That(new Spinner().AuxiliarySamples, Is.Empty);
        }

        [Test]
        public void TestSpinningSamples_UsesFirstSampleAsReference()
        {
            // referenceSample == null FALSE
            var spinner = new Spinner { Samples = new[] { new HitSampleInfo(HitSampleInfo.HIT_CLAP, "drum") } };

            var sample = spinner.CreateSpinningSamples().Single();

            Assert.That(sample.Name, Is.EqualTo("spinnerspin"));
            Assert.That(sample.Bank, Is.EqualTo("drum"));
            Assert.That(spinner.AuxiliarySamples.Single().Name, Is.EqualTo("spinnerspin"));
        }

        [Test]
        public void TestJudgementsAndHitWindows()
        {
            var spinner = create(2000);

            Assert.That(spinner.CreateJudgement(), Is.TypeOf<OsuJudgement>());
            Assert.That(spinner.HitWindows, Is.SameAs(HitWindows.Empty));
        }

        [Test]
        public void TestTickJudgements()
        {
            var tick = new SpinnerTick { SpinnerDuration = 1234 };
            var bonus = new SpinnerBonusTick();

            Assert.That(tick.CreateJudgement().MaxResult, Is.EqualTo(HitResult.SmallBonus));
            Assert.That(bonus.CreateJudgement().MaxResult, Is.EqualTo(HitResult.LargeBonus));
            Assert.That(tick.MaximumJudgementOffset, Is.EqualTo(1234));
        }
    }
}
