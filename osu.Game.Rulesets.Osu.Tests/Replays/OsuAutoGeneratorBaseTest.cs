// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Replays;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Replays;
using osu.Game.Rulesets.Replays;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Replays
{
    [TestFixture]
    public class OsuAutoGeneratorBaseTest
    {
        private static Probe create(params Mod[] mods) => new Probe(new Beatmap(), mods);

        [Test]
        public void TestNoRateModsLeaveTimeAndRateUntouched()
        {
            var probe = create(new OsuModHidden());

            Assert.That(probe.TimeDelta(100, 400), Is.EqualTo(300));
            Assert.That(probe.Rate(0, 1), Is.EqualTo(1));
            Assert.That(probe.FrameDelay(0), Is.EqualTo(1000.0 / 60).Within(1e-9));
        }

        [Test]
        public void TestRateModsShortenRealTimeAndFrameDelay()
        {
            var probe = create(new OsuModDoubleTime());

            Assert.That(probe.TimeDelta(0, 300), Is.EqualTo(200).Within(1e-9));
            Assert.That(probe.Rate(0, 1), Is.EqualTo(1.5).Within(1e-9));
            Assert.That(probe.FrameDelay(0), Is.EqualTo(1000.0 / 60 * 1.5).Within(1e-9));
        }

        // CF: OsuAutoGeneratorBase.FindInsertionIndex | index < 0 | TRUE
        [Test]
        public void TestFramesAreInsertedInTimeOrder()
        {
            var probe = create();

            probe.Add(new OsuReplayFrame(300, Vector2.Zero));
            probe.Add(new OsuReplayFrame(100, Vector2.Zero));
            probe.Add(new OsuReplayFrame(200, Vector2.Zero));

            Assert.That(probe.Times, Is.EqualTo(new double[] { 100, 200, 300 }));
        }

        // CF: OsuAutoGeneratorBase.FindInsertionIndex | index < 0 | FALSE
        // CF: OsuAutoGeneratorBase.FindInsertionIndex | index < Frames.Count && frame.Time == Frames[index].Time | TRUE
        // CF: OsuAutoGeneratorBase.FindInsertionIndex | index < Frames.Count && frame.Time == Frames[index].Time | FALSE
        [Test]
        public void TestFrameWithEqualTimeIsInsertedAfterExistingOnes()
        {
            var probe = create();
            var first = new OsuReplayFrame(100, new Vector2(1, 1));
            var second = new OsuReplayFrame(100, new Vector2(2, 2));
            var later = new OsuReplayFrame(200, Vector2.Zero);

            probe.Add(first);
            probe.Add(later);
            probe.Add(second);

            Assert.That(probe.AllFrames, Is.EqualTo(new ReplayFrame[] { first, second, later }));
        }

        [Test]
        public void TestCirclePosition()
        {
            var p = Probe.Circle(System.Math.PI / 2, 50);

            Assert.That(p.X, Is.EqualTo(0).Within(1e-3));
            Assert.That(p.Y, Is.EqualTo(50).Within(1e-3));
        }

        private class Probe : OsuAutoGeneratorBase
        {
            public Probe(IBeatmap beatmap, IReadOnlyList<Mod> mods)
                : base(beatmap, mods)
            {
            }

            public override Replay Generate() => Replay;

            public double TimeDelta(double s, double e) => ApplyModsToTimeDelta(s, e);
            public double Rate(double t, double r) => ApplyModsToRate(t, r);
            public double FrameDelay(double t) => GetFrameDelay(t);
            public void Add(ReplayFrame f) => AddFrameToReplay(f);
            public IEnumerable<double> Times => Frames.Select(f => f.Time);
            public List<ReplayFrame> AllFrames => Frames;
            public static Vector2 Circle(double t, double r) => CirclePosition(t, r);
        }
    }
}
