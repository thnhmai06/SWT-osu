// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using System.Threading;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Tests.Objects
{
    [TestFixture]
    public class DrumRollTest
    {
        private static DrumRoll create(double tickRate, bool strong = false, CancellationToken token = default)
        {
            var cpi = new ControlPointInfo();
            cpi.Add(0, new TimingControlPoint { BeatLength = 500 });

            var roll = new DrumRoll { StartTime = 0, Duration = 1000, IsStrong = strong };
            roll.ApplyDefaults(cpi, new BeatmapDifficulty { SliderTickRate = tickRate }, token);
            return roll;
        }

        // CF: DrumRoll.ApplyDefaultsToSelf | difficulty.SliderTickRate == 3 | FALSE
        // CF: DrumRoll.createTicks | tickSpacing == 0 | FALSE
        // CF: DrumRoll.createTicks | for t < EndTime + tickSpacing / 2 | loop n
        // CF: DrumRoll.createTicks | first | TRUE
        // CF: DrumRoll.createTicks | first | FALSE
        [Test]
        public void TestFourTicksPerBeat()
        {
            var roll = create(1);
            var ticks = roll.NestedHitObjects.OfType<DrumRollTick>().ToList();

            Assert.That(roll.TickRate, Is.EqualTo(4));
            Assert.That(ticks, Has.Count.EqualTo(9));
            Assert.That(ticks[0].FirstTick, Is.True);
            Assert.That(ticks.Skip(1).Any(t => t.FirstTick), Is.False);
            Assert.That(ticks[1].StartTime, Is.EqualTo(125));
        }

        // CF: DrumRoll.ApplyDefaultsToSelf | difficulty.SliderTickRate == 3 | TRUE
        [Test]
        public void TestThreeTicksPerBeat()
        {
            var roll = create(3);
            Assert.That(roll.TickRate, Is.EqualTo(3));
            Assert.That(roll.NestedHitObjects.OfType<DrumRollTick>().Count(), Is.EqualTo(7));
        }

        // CF: DrumRoll.createTicks | tickSpacing == 0 | TRUE
        // CF: DrumRoll.createTicks | for t < EndTime + tickSpacing / 2 | loop 0
        // infeasible: tickSpacing is BeatLength / TickRate and BeatLength has MinValue 6, so it is never 0 after ApplyDefaults; a drum roll with Duration < 0 gives loop 0 below
        [Test]
        public void TestNegativeDurationCreatesNoTicks()
        {
            var cpi = new ControlPointInfo();
            cpi.Add(0, new TimingControlPoint { BeatLength = 500 });
            var roll = new DrumRoll { StartTime = 1000, Duration = -1000 };
            roll.ApplyDefaults(cpi, new BeatmapDifficulty());
            Assert.That(roll.NestedHitObjects.OfType<DrumRollTick>(), Is.Empty);
        }

        // CF: DrumRoll.createTicks | cancellationToken.ThrowIfCancellationRequested | throws OperationCanceledException
        [Test]
        public void TestCancellationThrows()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            Assert.Throws<System.OperationCanceledException>(() => create(1, token: cts.Token));
        }

        // CF: TaikoStrongableHitObject.CreateNestedHitObjects | IsStrong | TRUE
        [Test]
        public void TestStrongRollHasIgnoredStrongNestedHit()
        {
            var roll = create(1, strong: true);
            var strong = roll.NestedHitObjects.OfType<DrumRoll.StrongNestedHit>().Single();
            Assert.That(strong.CreateJudgement(), Is.TypeOf<IgnoreJudgement>());
            Assert.That(strong.StartTime, Is.EqualTo(1000));
        }

        [Test]
        public void TestEndTimeAndDurationAreLinked()
        {
            var roll = new DrumRoll { StartTime = 100 };
            roll.EndTime = 600;
            Assert.That(roll.Duration, Is.EqualTo(500));
            Assert.That(roll.CreateJudgement(), Is.TypeOf<IgnoreJudgement>());
        }
    }
}
