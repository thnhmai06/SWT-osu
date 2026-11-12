// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Replays;
using osu.Game.Rulesets.Osu.Tests.Difficulty;

namespace osu.Game.Rulesets.Osu.Tests.Replays
{
    [TestFixture]
    public class OsuAutoGeneratorTest
    {
        private static OsuReplayFrame[] generate(params OsuHitObject[] objects)
        {
            var beatmap = DifficultyTestHelpers.Beatmap(objects);
            foreach (var o in beatmap.HitObjects)
                o.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            return new OsuAutoGenerator(beatmap, Array.Empty<Mod>()).Generate().Frames.Cast<OsuReplayFrame>().ToArray();
        }

        // HitObjects.Count == 0 TRUE: early return, replay has no frames.
        [Test]
        public void TestEmptyBeatmap_NoFrames() => Assert.That(generate(), Is.Empty);

        // Circle: frames exist, are time ordered, and an action press is present.
        [Test]
        public void TestCircle_FramesOrderedAndPressed()
        {
            var frames = generate(DifficultyTestHelpers.Circle(1000, 100, 100), DifficultyTestHelpers.Circle(1500, 300, 200));

            Assert.That(frames, Is.Not.Empty);
            Assert.That(frames.Select(f => f.Time), Is.Ordered);
            Assert.That(frames.Any(f => f.Actions.Count > 0), Is.True);
        }

        // Slider path: cursor follows the slider so frames cover its duration.
        [Test]
        public void TestSlider_FramesReachSliderEnd()
        {
            var slider = DifficultyTestHelpers.Slider(1000, 100, 100, 200);
            var beatmap = DifficultyTestHelpers.Beatmap(new[] { slider });
            beatmap.HitObjects[0].ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);
            var frames = new OsuAutoGenerator(beatmap, Array.Empty<Mod>()).Generate().Frames.Cast<OsuReplayFrame>().ToArray();

            Assert.That(frames.Select(f => f.Time), Is.Ordered);
            Assert.That(frames.Last().Time, Is.GreaterThanOrEqualTo(((osu.Game.Rulesets.Objects.Types.IHasDuration)beatmap.HitObjects[0]).EndTime - 1));
        }

        // Spinner: frames spin around the centre; more than a few frames are produced.
        [Test]
        public void TestSpinner_ProducesManyFrames()
        {
            var frames = generate(DifficultyTestHelpers.Spinner(1000, 2000));

            Assert.That(frames.Length, Is.GreaterThan(5));
            Assert.That(frames.Select(f => f.Time), Is.Ordered);
        }
    }
}
