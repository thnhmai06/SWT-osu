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
    public class ManiaModInvertTest
    {
        // CF: ManiaModInvert.ApplyToBeatmap | foreach column | loop 0
        [Test]
        public void TestEmptyBeatmap()
        {
            var beatmap = new ManiaBeatmap(new StageDefinition(4));
            new ManiaModInvert().ApplyToBeatmap(beatmap);
            Assert.That(beatmap.HitObjects, Is.Empty);
        }

        // CF: ManiaModInvert.ApplyToBeatmap | for i < locations.Count - 1 | loop 0
        // CF: ManiaModInvert.ApplyToBeatmap | foreach column | loop 1
        [Test]
        public void TestLoneObjectInColumnProducesNothing()
        {
            var beatmap = new ManiaBeatmap(new StageDefinition(4)) { HitObjects = { new Note { StartTime = 0, Column = 1 } } };
            new ManiaModInvert().ApplyToBeatmap(beatmap);
            Assert.That(beatmap.HitObjects, Is.Empty);
        }

        // CF: ManiaModInvert.ApplyToBeatmap | for i < locations.Count - 1 | loop 1
        // CF: ManiaModInvert.ApplyToBeatmap | for i < locations.Count - 1 | loop n
        // CF: ManiaModInvert.ApplyToBeatmap | foreach column | loop n
        [Test]
        public void TestGapsBecomeHoldNotes()
        {
            // default beat length is 1000ms, so each gap shrinks to max(gap / 2, gap - 250).
            var beatmap = new ManiaBeatmap(new StageDefinition(4))
            {
                HitObjects =
                {
                    new Note { StartTime = 0, Column = 0 },
                    new HoldNote { StartTime = 1000, Duration = 500, Column = 0 },
                    new Note { StartTime = 3000, Column = 0 },
                    new Note { StartTime = 500, Column = 1 },
                    new Note { StartTime = 700, Column = 1 },
                }
            };
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 1000 });
            beatmap.Breaks.Add(new osu.Game.Beatmaps.Timing.BreakPeriod(100, 200));

            new ManiaModInvert().ApplyToBeatmap(beatmap);

            var result = beatmap.HitObjects.Cast<HoldNote>().Select(h => (h.StartTime, h.Duration, h.Column)).ToArray();

            Assert.That(result, Is.EqualTo(new[]
            {
                (0.0, 750.0, 0),
                (500.0, 100.0, 1), // max(200 / 2, 200 - 250)
                (1000.0, 1750.0, 0),
            }));
            Assert.That(beatmap.Breaks, Is.Empty);
        }
    }
}
