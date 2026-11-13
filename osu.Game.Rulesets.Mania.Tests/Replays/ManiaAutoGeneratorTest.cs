// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Mania.Beatmaps;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mania.Replays;

namespace osu.Game.Rulesets.Mania.Tests.Replays
{
    [TestFixture]
    public class ManiaAutoGeneratorTest
    {
        private static (double time, ManiaAction[] actions)[] generate(params ManiaHitObject[] objects)
        {
            var beatmap = new ManiaBeatmap(new StageDefinition(4)) { HitObjects = objects.ToList() };
            return new ManiaAutoGenerator(beatmap).Generate().Frames.Cast<ManiaReplayFrame>().Select(f => (f.Time, f.Actions.ToArray())).ToArray();
        }

        // CF: ManiaAutoGenerator.GenerateFrames | Beatmap.HitObjects.Count == 0 | TRUE
        [Test]
        public void TestEmptyBeatmapHasNoFrames() => Assert.That(generate(), Is.Empty);

        // CF: ManiaAutoGenerator.GenerateFrames | Beatmap.HitObjects.Count == 0 | FALSE
        // CF: ManiaAutoGenerator.GenerateFrames | case HitPoint | case HitPoint
        // CF: ManiaAutoGenerator.GenerateFrames | case ReleasePoint | case ReleasePoint
        // CF: ManiaAutoGenerator.calculateReleaseTime | currentObject is HoldNote | FALSE
        // CF: ManiaAutoGenerator.calculateReleaseTime | nextObject == null | TRUE
        // CF: ManiaAutoGenerator.GetNextObject | for i | loop 0
        [Test]
        public void TestSingleNoteIsReleasedAfterDelay()
        {
            var frames = generate(new Note { StartTime = 1000, Column = 0 });

            Assert.That(frames.Select(f => f.time), Is.EqualTo(new[] { 1000.0, 1000 + ManiaAutoGenerator.RELEASE_DELAY }));
            Assert.That(frames[0].actions, Is.EqualTo(new[] { ManiaAction.Key1 }));
            Assert.That(frames[1].actions, Is.Empty);
        }

        // CF: ManiaAutoGenerator.calculateReleaseTime | currentObject is HoldNote | TRUE
        // CF: ManiaAutoGenerator.calculateReleaseTime | hold.Duration > 0 | TRUE
        [Test]
        public void TestHoldNoteReleasedExactlyAtEnd()
        {
            var frames = generate(new HoldNote { StartTime = 1000, Duration = 500, Column = 2 });

            Assert.That(frames.Select(f => f.time), Is.EqualTo(new[] { 1000.0, 1500.0 }));
            Assert.That(frames[0].actions, Is.EqualTo(new[] { ManiaAction.Key3 }));
        }

        // CF: ManiaAutoGenerator.calculateReleaseTime | hold.Duration > 0 | FALSE
        [Test]
        public void TestZeroLengthHoldUsesShortDelay()
        {
            var frames = generate(new HoldNote { StartTime = 1000, Duration = 0, Column = 0 });
            Assert.That(frames.Select(f => f.time), Is.EqualTo(new[] { 1000.0, 1001.0 }));
        }

        // CF: ManiaAutoGenerator.calculateReleaseTime | nextObject == null | FALSE
        // CF: ManiaAutoGenerator.calculateReleaseTime | nextObject.StartTime > endTime + releaseDelay | FALSE
        // CF: ManiaAutoGenerator.GetNextObject | for i | loop n
        // CF: ManiaAutoGenerator.GetNextObject | Beatmap.HitObjects[i].Column == desiredColumn | TRUE
        // CF: ManiaAutoGenerator.GetNextObject | Beatmap.HitObjects[i].Column == desiredColumn | FALSE
        [Test]
        public void TestReleaseIsBroughtForwardWhenNextNoteIsClose()
        {
            // next note in the same column is 10ms away (< release delay) so the key is lifted at 90% of the gap; the other-column note must not matter.
            var frames = generate(
                new Note { StartTime = 1000, Column = 0 },
                new Note { StartTime = 1005, Column = 1 },
                new Note { StartTime = 1010, Column = 0 });

            Assert.That(frames.Select(f => f.time), Does.Contain(1009.0));
        }

        // CF: ManiaAutoGenerator.calculateReleaseTime | nextObject.StartTime > endTime + releaseDelay | TRUE
        [Test]
        public void TestReleaseDelayedFullyWhenNextNoteIsFarAway()
        {
            var frames = generate(new Note { StartTime = 1000, Column = 0 }, new Note { StartTime = 1500, Column = 0 });
            Assert.That(frames.Select(f => f.time), Is.EqualTo(new[] { 1000.0, 1020.0, 1500.0, 1520.0 }));
        }

        // CF: ManiaAutoGenerator.GenerateFrames | foreach point in group | loop n
        [Test]
        public void TestSimultaneousNotesShareAFrame()
        {
            var frames = generate(new Note { StartTime = 1000, Column = 0 }, new Note { StartTime = 1000, Column = 3 });

            Assert.That(frames[0].time, Is.EqualTo(1000));
            Assert.That(frames[0].actions, Is.EquivalentTo(new[] { ManiaAction.Key1, ManiaAction.Key4 }));
            Assert.That(frames[1].actions, Is.Empty);
        }
    }
}
