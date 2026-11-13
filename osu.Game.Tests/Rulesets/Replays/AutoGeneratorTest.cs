// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Replays;

namespace osu.Game.Tests.Rulesets.Replays
{
    [TestFixture]
    public class AutoGeneratorTest
    {
        private class TestGenerator : AutoGenerator<ReplayFrame>
        {
            private readonly double[] times;
            public int GenerateCalls;

            public TestGenerator(IBeatmap beatmap, params double[] times)
                : base(beatmap)
            {
                this.times = times;
            }

            public ReplayFrame? Last => LastFrame;
            public int FrameCountBeforeGenerate => Frames.Count;
            public HitObject? Next(int index) => GetNextObject(index);

            protected override void GenerateFrames()
            {
                GenerateCalls++;
                foreach (double t in times) Frames.Add(new ReplayFrame(t));
            }
        }

        private static Beatmap beatmap(params double[] times)
        {
            var b = new Beatmap();
            foreach (double t in times) b.HitObjects.Add(new HitCircle { StartTime = t });
            return b;
        }

        [Test]
        public void TestFramesAreSortedByTime()
        {
            var generator = new TestGenerator(beatmap(), 300, 100, 200);

            Assert.That(generator.Generate().Frames.Select(f => f.Time), Is.EqualTo(new[] { 100d, 200d, 300d }));
        }

        [Test]
        public void TestGeneratingTwiceDoesNotDuplicateFrames()
        {
            var generator = new TestGenerator(beatmap(), 100, 200);

            generator.Generate();
            var second = generator.Generate();

            Assert.That(second.Frames, Has.Count.EqualTo(2));
            Assert.That(generator.GenerateCalls, Is.EqualTo(2));
        }

        // CF: AutoGenerator.LastFrame | Frames.Count == 0 | TRUE
        [Test]
        public void TestLastFrameNullWithoutFrames()
        {
            Assert.That(new TestGenerator(beatmap()).Last, Is.Null);
            Assert.That(new TestGenerator(beatmap()).Generate().Frames, Is.Empty);
        }

        // CF: AutoGenerator.LastFrame | Frames.Count == 0 | FALSE
        [Test]
        public void TestLastFrameIsLastGenerated()
        {
            var generator = new TestGenerator(beatmap(), 300, 100);
            generator.Generate();

            Assert.That(generator.Last!.Time, Is.EqualTo(100), "last in generation order, not sorted order");
        }

        // CF: AutoGenerator.GetNextObject | currentIndex >= Beatmap.HitObjects.Count - 1 | FALSE
        [Test]
        public void TestNextObject()
        {
            var b = beatmap(0, 100, 200);
            var generator = new TestGenerator(b);

            Assert.That(generator.Next(0), Is.SameAs(b.HitObjects[1]));
            Assert.That(generator.Next(1), Is.SameAs(b.HitObjects[2]));
        }

        // CF: AutoGenerator.GetNextObject | currentIndex >= Beatmap.HitObjects.Count - 1 | TRUE
        [Test]
        public void TestNoNextObjectAtOrAfterTheLast()
        {
            var generator = new TestGenerator(beatmap(0, 100));

            Assert.That(generator.Next(1), Is.Null);
            Assert.That(generator.Next(5), Is.Null);
            Assert.That(new TestGenerator(beatmap()).Next(0), Is.Null);
        }

        [Test]
        public void TestKeyUpDelayConstant()
        {
            Assert.That(AutoGenerator.KEY_UP_DELAY, Is.EqualTo(50));
        }
    }
}
