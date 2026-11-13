// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Beatmaps
{
    [TestFixture]
    public class OsuBeatmapProcessorTest
    {
        private static HitCircle circle(double time, float x = 100, float y = 100) => new HitCircle { StartTime = time, Position = new Vector2(x, y) };

        // slider from (x,y) travelling 100px to the right
        private static Slider slider(double time, float x = 100, float y = 100)
        {
            var s = new Slider
            {
                StartTime = time,
                Position = new Vector2(x, y),
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(100, 0) }),
            };
            s.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());
            return s;
        }

        private static OsuBeatmap process(int version, params OsuHitObject[] objects)
        {
            var beatmap = new OsuBeatmap { BeatmapVersion = version };
            beatmap.HitObjects.AddRange(objects);
            var processor = new OsuBeatmapProcessor(beatmap);
            processor.PreProcess();
            processor.PostProcess();
            return beatmap;
        }

        private static int[] heights(OsuBeatmap beatmap) => beatmap.HitObjects.Select(h => h.StackHeight).ToArray();

        // CF: OsuBeatmapProcessor.PreProcess | foreach obj | loop 0
        [Test]
        public void TestPreProcessEmptyBeatmap()
        {
            Assert.DoesNotThrow(() => process(14));
        }

        // CF: OsuBeatmapProcessor.PreProcess | foreach obj | loop n
        // CF: OsuBeatmapProcessor.PreProcess | obj is not Spinner | TRUE
        // CF: OsuBeatmapProcessor.PreProcess | lastObj == null | TRUE
        // CF: OsuBeatmapProcessor.PreProcess | lastObj == null | FALSE
        // CF: OsuBeatmapProcessor.PreProcess | lastObj is Spinner | TRUE
        // CF: OsuBeatmapProcessor.PreProcess | lastObj is Spinner | FALSE
        [Test]
        public void TestFirstObjectAndObjectAfterSpinnerStartCombo()
        {
            var beatmap = process(14, circle(0), circle(1000, 300, 300), new Spinner { StartTime = 2000, Duration = 100 }, circle(3000, 50, 50), circle(4000, 400, 50));

            Assert.That(beatmap.HitObjects.Select(h => h.NewCombo), Is.EqualTo(new[] { true, false, false, true, false }));
        }

        // CF: OsuBeatmapProcessor.PreProcess | obj is not Spinner | FALSE
        [Test]
        public void TestSpinnerNeverForcedToNewCombo()
        {
            var beatmap = process(14, new Spinner { StartTime = 0, Duration = 100 });

            Assert.That(beatmap.HitObjects[0].NewCombo, Is.False);
        }

        // CF: OsuBeatmapProcessor.ApplyStacking | hitObjects.Count > 0 | FALSE
        // CF: OsuBeatmapProcessor.ApplyStacking | beatmap.HitObjects as List<OsuHitObject> | null
        [Test]
        public void TestStackingOfGenericBeatmapWithoutOsuObjects()
        {
            var beatmap = new Beatmap<HitObject>();
            var processor = new OsuBeatmapProcessor(beatmap);

            Assert.DoesNotThrow(() => processor.PostProcess());
        }

        // CF: OsuBeatmapProcessor.ApplyStacking | hitObjects.Count > 0 | TRUE
        // CF: OsuBeatmapProcessor.ApplyStacking | beatmap.BeatmapVersion >= 6 | TRUE
        // CF: OsuBeatmapProcessor.applyStacking | objectI.StackHeight != 0 || objectI is Spinner | FALSE
        // CF: OsuBeatmapProcessor.applyStacking | objectI is HitCircle | TRUE
        // CF: OsuBeatmapProcessor.applyStacking | Vector2Extensions.Distance(objectN.Position, objectI.Position) < STACK_DISTANCE | TRUE
        [Test]
        public void TestStackedCirclesBuildUpwardsTowardsEarlierObjects()
        {
            var beatmap = process(14, circle(0), circle(100), circle(200));

            Assert.That(heights(beatmap), Is.EqualTo(new[] { 2, 1, 0 }));
        }

        // CF: OsuBeatmapProcessor.applyStacking | Vector2Extensions.Distance(objectN.Position, objectI.Position) < STACK_DISTANCE | FALSE
        [Test]
        public void TestDistantCirclesDoNotStack()
        {
            var beatmap = process(14, circle(0, 100), circle(100, 120));

            Assert.That(heights(beatmap), Is.EqualTo(new[] { 0, 0 }));
        }

        // CF: OsuBeatmapProcessor.applyStacking | (int)objectI.StartTime - (int)endTime > stackThreshold | TRUE
        [Test]
        public void TestCirclesTooFarApartInTimeDoNotStack()
        {
            var beatmap = process(14, circle(0), circle(5000));

            Assert.That(heights(beatmap), Is.EqualTo(new[] { 0, 0 }));
        }

        // CF: OsuBeatmapProcessor.applyStacking | objectN is Spinner | TRUE
        // CF: OsuBeatmapProcessor.applyStacking | objectI is Spinner | TRUE
        [Test]
        public void TestSpinnersAreTransparentToStacking()
        {
            var beatmap = process(14, circle(0), new Spinner { StartTime = 100, Duration = 50, Position = new Vector2(100, 100) }, circle(200));

            Assert.That(heights(beatmap), Is.EqualTo(new[] { 1, 0, 0 }));
        }

        // CF: OsuBeatmapProcessor.applyStacking | objectI.StackHeight != 0 | TRUE
        [Test]
        public void TestObjectsAlreadyInAStackAreSkipped()
        {
            // interleaved stacks: (0,2) at A and (1,3) at B
            var beatmap = process(14, circle(0, 100), circle(50, 300), circle(100, 100), circle(150, 300));

            Assert.That(heights(beatmap), Is.EqualTo(new[] { 1, 1, 0, 0 }));
        }

        // CF: OsuBeatmapProcessor.applyStacking | objectN is Slider && Vector2Extensions.Distance(objectN.EndPosition, objectI.Position) < STACK_DISTANCE | TRUE
        // CF: OsuBeatmapProcessor.applyStacking | Vector2Extensions.Distance(objectN.EndPosition, objectJ.Position) < STACK_DISTANCE | TRUE
        [Test]
        public void TestCircleUnderSliderEndIsStackedNegatively()
        {
            var beatmap = process(14, slider(0), circle(800, 200, 100));

            Assert.That(heights(beatmap), Is.EqualTo(new[] { 0, -1 }));
        }

        // CF: OsuBeatmapProcessor.applyStacking | objectN is Slider | FALSE
        [Test]
        public void TestCircleAwayFromSliderEndIsNotStacked()
        {
            var beatmap = process(14, slider(0), circle(2000, 300, 300));

            Assert.That(heights(beatmap), Is.EqualTo(new[] { 0, 0 }));
        }

        // CF: OsuBeatmapProcessor.applyStacking | objectI is Slider | TRUE
        // CF: OsuBeatmapProcessor.applyStacking | objectI.StartTime - objectN.StartTime > stackThreshold | FALSE
        // CF: OsuBeatmapProcessor.applyStacking | Vector2Extensions.Distance(objectN.EndPosition, objectI.Position) < STACK_DISTANCE | TRUE
        [Test]
        public void TestSliderStacksPositivelyOverEarlierObjectEnding()
        {
            var beatmap = process(14, circle(0, 100, 100), slider(100, 100, 100));

            Assert.That(heights(beatmap), Is.EqualTo(new[] { 1, 0 }));
        }

        // CF: OsuBeatmapProcessor.applyStacking | objectI.StartTime - objectN.StartTime > stackThreshold | TRUE
        // CF: OsuBeatmapProcessor.applyStacking | Vector2Extensions.Distance(objectN.EndPosition, objectI.Position) < STACK_DISTANCE | FALSE
        [Test]
        public void TestSliderDoesNotStackOverDistantOrOldObjects()
        {
            Assert.That(heights(process(14, circle(0, 100, 100), slider(5000, 100, 100))), Is.EqualTo(new[] { 0, 0 }));
            Assert.That(heights(process(14, circle(0, 300, 300), slider(100, 100, 100))), Is.EqualTo(new[] { 0, 0 }));
        }

        // CF: OsuBeatmapProcessor.applyStacking | objectN is Spinner | TRUE
        [Test]
        public void TestSliderStackingSkipsSpinners()
        {
            var beatmap = process(14, circle(0, 100, 100), new Spinner { StartTime = 50, Duration = 10, Position = new Vector2(100, 100) }, slider(100, 100, 100));

            Assert.That(heights(beatmap), Is.EqualTo(new[] { 1, 0, 0 }));
        }

        // CF: OsuBeatmapProcessor.ApplyStacking | beatmap.BeatmapVersion >= 6 | FALSE
        // CF: OsuBeatmapProcessor.applyStackingOld | for (i < hitObjects.Count) | loop n
        // CF: OsuBeatmapProcessor.applyStackingOld | Vector2Extensions.Distance(hitObjects[j].Position, currHitObject.Position) < STACK_DISTANCE | TRUE
        [Test]
        public void TestLegacyStackingOfCircles()
        {
            var beatmap = process(5, circle(0), circle(100), circle(200));

            Assert.That(heights(beatmap), Is.EqualTo(new[] { 2, 1, 0 }));
        }

        // CF: OsuBeatmapProcessor.applyStackingOld | hitObjects[j].StartTime - stackThreshold > startTime | TRUE
        // CF: OsuBeatmapProcessor.applyStackingOld | Vector2Extensions.Distance(hitObjects[j].Position, position2) < STACK_DISTANCE | FALSE
        [Test]
        public void TestLegacyStackingRespectsTimeAndDistance()
        {
            Assert.That(heights(process(5, circle(0), circle(5000))), Is.EqualTo(new[] { 0, 0 }));
            Assert.That(heights(process(5, circle(0, 100), circle(100, 200))), Is.EqualTo(new[] { 0, 0 }));
        }

        // CF: OsuBeatmapProcessor.applyStackingOld | currHitObject.StackHeight != 0 && !(currHitObject is Slider) | TRUE
        // CF: OsuBeatmapProcessor.applyStackingOld | Vector2Extensions.Distance(hitObjects[j].Position, position2) < STACK_DISTANCE | TRUE
        // CF: OsuBeatmapProcessor.applyStackingOld | currHitObject is Slider currSlider | TRUE
        // CF: OsuBeatmapProcessor.applyStackingOld | currHitObject is Slider currSlider | FALSE
        [Test]
        public void TestLegacyStackingUnderSliderEnd()
        {
            var beatmap = process(5, slider(0), circle(800, 200, 100));

            Assert.That(heights(beatmap), Is.EqualTo(new[] { 0, -1 }));
        }
    }
}
