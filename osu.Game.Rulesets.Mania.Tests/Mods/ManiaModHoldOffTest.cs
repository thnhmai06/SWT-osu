// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Mania.Beatmaps;
using osu.Game.Rulesets.Mania.Mods;
using osu.Game.Rulesets.Mania.Objects;

namespace osu.Game.Rulesets.Mania.Tests.Mods
{
    [TestFixture]
    public class ManiaModHoldOffTest
    {
        private static ManiaBeatmap createBeatmap(params ManiaHitObject[] objects)
        {
            var beatmap = new ManiaBeatmap(new StageDefinition(4)) { HitObjects = objects.ToList() };
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });
            beatmap.ControlPointInfo.Add(900, new TimingControlPoint { BeatLength = 1000 });
            beatmap.ControlPointInfo.Add(0, new EffectControlPoint { ScrollSpeed = 1.5 });
            return beatmap;
        }

        // CF: ManiaModHoldOff.ApplyToBeatmap | foreach HoldNote | loop 0
        // CF: ManiaModHoldOff.ApplyToBeatmap | !Precision.AlmostEquals(before, after) | FALSE
        [Test]
        public void TestNoHoldNotesLeavesBeatmapUntouched()
        {
            var beatmap = createBeatmap(new Note { StartTime = 0 }, new Note { StartTime = 2000, Column = 1 });

            new ManiaModHoldOff().ApplyToBeatmap(beatmap);

            Assert.That(beatmap.HitObjects, Has.Count.EqualTo(2));
            Assert.That(beatmap.ControlPointInfo.EffectPoints[0].ScrollSpeed, Is.EqualTo(1.5));
        }

        // CF: ManiaModHoldOff.ApplyToBeatmap | foreach HoldNote | loop 1
        // CF: ManiaModHoldOff.ApplyToBeatmap | foreach HoldNote | loop n
        // CF: ManiaModHoldOff.ApplyToBeatmap | !Precision.AlmostEquals(before, after) | TRUE
        // CF: ManiaModHoldOff.ApplyToBeatmap | foreach effectPoint | loop n
        [Test]
        public void TestHoldNotesBecomeNotesAndScrollSpeedIsCompensated()
        {
            var beatmap = createBeatmap(
                new Note { StartTime = 0, Column = 0 },
                new HoldNote { StartTime = 500, Duration = 1500, Column = 1 });

            // most common beat length is 1000 before (the slow section lasts the longest), and 500 afterwards (the last object now ends at 500).
            new ManiaModHoldOff().ApplyToBeatmap(beatmap);

            Assert.That(beatmap.HitObjects.Select(h => h.GetType()), Is.All.EqualTo(typeof(Note)));
            Assert.That(beatmap.HitObjects.Select(h => (h.StartTime, h.Column)), Is.EqualTo(new[] { (0.0, 0), (500.0, 1) }));
            Assert.That(beatmap.ControlPointInfo.EffectPoints[0].ScrollSpeed, Is.EqualTo(1.5 * 2).Within(1e-9));
        }
    }
}
