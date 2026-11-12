// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
        // Every object uses TimePreempt = 600 and the default StackLeniency (0.7) => stack threshold of 420ms.
        private static readonly BeatmapDifficulty difficulty = new BeatmapDifficulty { SliderMultiplier = 2 };

        private static HitCircle circle(double time, float x = 100, float y = 100) => new HitCircle { StartTime = time, Position = new Vector2(x, y), TimePreempt = 600 };

        /// <summary>A slider from <paramref name="x"/>,<paramref name="y"/> to the right by <paramref name="length"/>; lasts length / 0.2 ms.</summary>
        private static Slider slider(double time, float x = 100, float y = 100, float length = 100)
        {
            var s = new Slider
            {
                StartTime = time,
                Position = new Vector2(x, y),
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(length, 0) })
            };
            s.ApplyDefaults(new ControlPointInfo(), difficulty);
            s.TimePreempt = 600;
            return s;
        }

        private static Spinner spinner(double time) => new Spinner { StartTime = time, Duration = 100, TimePreempt = 600 };

        private static OsuBeatmap beatmap(int version, params OsuHitObject[] objects)
        {
            var b = new OsuBeatmap { BeatmapVersion = version };
            foreach (var o in objects) b.HitObjects.Add(o);
            return b;
        }

        private static OsuBeatmap modern(params OsuHitObject[] objects) => beatmap(14, objects);

        private static OsuBeatmap legacy(params OsuHitObject[] objects) => beatmap(5, objects);

        // ---- PreProcess: obj is not Spinner && (lastObj == null || lastObj is Spinner) ----

        [Test]
        public void TestPreProcess_FirstObjectBecomesNewCombo()
        {
            // lastObj == null TRUE
            var first = circle(0);
            new OsuBeatmapProcessor(modern(first)).PreProcess();

            Assert.That(first.NewCombo, Is.True);
        }

        [Test]
        public void TestPreProcess_MiddleObjectKeepsNewComboFlag()
        {
            // not Spinner TRUE, lastObj == null FALSE, lastObj is Spinner FALSE -> untouched
            var second = circle(100);
            new OsuBeatmapProcessor(modern(circle(0), second)).PreProcess();

            Assert.That(second.NewCombo, Is.False);
        }

        [Test]
        public void TestPreProcess_ObjectAfterSpinnerBecomesNewCombo()
        {
            // lastObj is Spinner TRUE
            var afterSpinner = circle(300);
            new OsuBeatmapProcessor(modern(circle(0), spinner(100), afterSpinner)).PreProcess();

            Assert.That(afterSpinner.NewCombo, Is.True);
        }

        [Test]
        public void TestPreProcess_SpinnerNeverForcedToNewCombo()
        {
            // `obj is not Spinner` FALSE (even as the first object)
            var firstSpinner = spinner(0);
            new OsuBeatmapProcessor(modern(firstSpinner)).PreProcess();

            Assert.That(firstSpinner.NewCombo, Is.False);
        }

        [Test]
        public void TestPreProcess_UpdatesComboIndices()
        {
            var a = circle(0);
            var b = circle(100);
            new OsuBeatmapProcessor(modern(a, b)).PreProcess();

            Assert.That(a.ComboIndex, Is.EqualTo(1));
            Assert.That(b.IndexInCurrentCombo, Is.EqualTo(1));
        }

        // ---- PostProcess applies stacking ----

        [Test]
        public void TestPostProcess_AppliesStacking()
        {
            var a = circle(0);
            var b = circle(100);
            new OsuBeatmapProcessor(modern(a, b)).PostProcess();

            Assert.That(a.StackHeight, Is.EqualTo(1));
        }

        // ---- ApplyStacking: top-level guards ----

        [Test]
        public void TestStacking_EmptyBeatmap_DoesNothing()
        {
            // hitObjects.Count > 0 FALSE
            Assert.DoesNotThrow(() => OsuBeatmapProcessor.ApplyStacking(modern()));
        }

        [Test]
        public void TestStacking_ResetsExistingStackHeights()
        {
            var lonely = circle(0);
            lonely.StackHeight = 5;

            OsuBeatmapProcessor.ApplyStacking(modern(lonely));

            Assert.That(lonely.StackHeight, Is.EqualTo(0));
        }

        [Test]
        public void TestStacking_BeatmapNotBackedByOsuList_StillStacks()
        {
            // `HitObjects as List<OsuHitObject>` null -> OfType path
            var a = circle(0);
            var b = circle(100);
            var generic = new Beatmap { HitObjects = { a, b } };

            OsuBeatmapProcessor.ApplyStacking(generic);

            Assert.That(a.StackHeight, Is.EqualTo(1));
        }

        // ---- new algorithm (BeatmapVersion >= 6): circle branch ----

        [Test]
        public void TestStacking_TwoCirclesAtSamePosition_EarlierIsStackedUp()
        {
            var a = circle(0);
            var b = circle(100);

            OsuBeatmapProcessor.ApplyStacking(modern(a, b));

            Assert.That(a.StackHeight, Is.EqualTo(1));
            Assert.That(b.StackHeight, Is.EqualTo(0));
        }

        [Test]
        public void TestStacking_ThreeCircles_FormDescendingStack()
        {
            // also exercises `objectI.StackHeight != 0` TRUE (middle circle already stacked when reached)
            var a = circle(0);
            var b = circle(100);
            var c = circle(200);

            OsuBeatmapProcessor.ApplyStacking(modern(a, b, c));

            Assert.That(new[] { a.StackHeight, b.StackHeight, c.StackHeight }, Is.EqualTo(new[] { 2, 1, 0 }));
        }

        [Test]
        public void TestStacking_CirclesFarApartInTime_NotStacked()
        {
            // (int)start - (int)end > threshold TRUE -> break
            var a = circle(0);
            var b = circle(440);

            OsuBeatmapProcessor.ApplyStacking(modern(a, b));

            Assert.That(a.StackHeight, Is.EqualTo(0));
        }

        [Test]
        public void TestStacking_CirclesJustInsideTimeThreshold_Stacked()
        {
            // threshold comparison FALSE
            var a = circle(0);
            var b = circle(400);

            OsuBeatmapProcessor.ApplyStacking(modern(a, b));

            Assert.That(a.StackHeight, Is.EqualTo(1));
        }

        [Test]
        public void TestStacking_CirclesAtDifferentPositions_NotStacked()
        {
            // distance < STACK_DISTANCE FALSE
            var a = circle(0, 100, 100);
            var b = circle(100, 200, 100);

            OsuBeatmapProcessor.ApplyStacking(modern(a, b));

            Assert.That(a.StackHeight, Is.EqualTo(0));
        }

        [Test]
        public void TestStacking_DistanceBoundary_IsStrictlyLessThanStackDistance()
        {
            var nearA = circle(0, 100, 100);
            var nearB = circle(100, 102.9f, 100);
            var farA = circle(0, 100, 100);
            var farB = circle(100, 103f, 100);

            OsuBeatmapProcessor.ApplyStacking(modern(nearA, nearB));
            OsuBeatmapProcessor.ApplyStacking(modern(farA, farB));

            Assert.That(nearA.StackHeight, Is.EqualTo(1));
            Assert.That(farA.StackHeight, Is.EqualTo(0));
        }

        [Test]
        public void TestStacking_SpinnerBetweenCircles_IsSkipped()
        {
            // inner loop `objectN is Spinner` -> continue
            var a = circle(0);
            var s = spinner(50);
            var b = circle(100);

            OsuBeatmapProcessor.ApplyStacking(modern(a, s, b));

            Assert.That(a.StackHeight, Is.EqualTo(1));
            Assert.That(s.StackHeight, Is.EqualTo(0));
        }

        [Test]
        public void TestStacking_SpinnerAsLastObject_IsNotStacked()
        {
            // outer reverse loop `objectI is Spinner` -> continue
            var a = circle(0);
            var s = spinner(100);
            s.Position = a.Position;

            OsuBeatmapProcessor.ApplyStacking(modern(a, s));

            Assert.That(a.StackHeight, Is.EqualTo(0));
            Assert.That(s.StackHeight, Is.EqualTo(0));
        }

        // ---- new algorithm: circle stacked under a slider ----

        [Test]
        public void TestStacking_CircleOnSliderEnd_IsPushedDownAndRight()
        {
            // objectN is Slider && distance(slider end, circle) < STACK_DISTANCE TRUE => negative stack
            var s = slider(0, 0, 0, 100);
            var c = circle(600, 100, 0);

            OsuBeatmapProcessor.ApplyStacking(modern(s, c));

            Assert.That(c.StackHeight, Is.EqualTo(-1));
            Assert.That(s.StackHeight, Is.EqualTo(0));
        }

        [Test]
        public void TestStacking_ObjectsBetweenSliderAndCircle_OnlyThoseOnSliderEndAreOffset()
        {
            // inner `for j`: distance(slider end, objectJ) < STACK_DISTANCE FALSE for the unrelated middle circle
            var s = slider(0, 0, 0, 100);
            var middle = circle(600, 300, 300);
            var c = circle(700, 100, 0);

            OsuBeatmapProcessor.ApplyStacking(modern(s, middle, c));

            Assert.That(middle.StackHeight, Is.EqualTo(0));
            Assert.That(c.StackHeight, Is.EqualTo(-1));
        }

        [Test]
        public void TestStacking_CircleOnSliderStart_StacksSliderUp()
        {
            // objectN is Slider TRUE but end far (&& FALSE) -> falls to the plain position check
            var s = slider(0, 0, 0, 100);
            var c = circle(600, 0, 0);

            OsuBeatmapProcessor.ApplyStacking(modern(s, c));

            Assert.That(s.StackHeight, Is.EqualTo(1));
            Assert.That(c.StackHeight, Is.EqualTo(0));
        }

        // ---- new algorithm: slider branch ----

        [Test]
        public void TestStacking_SliderStartingWhereCircleEnds_StacksCircleUp()
        {
            // objectI is Slider; distance(objectN.EndPosition, objectI.Position) < STACK_DISTANCE
            var c = circle(0, 100, 100);
            var s = slider(100, 100, 100, 100);

            OsuBeatmapProcessor.ApplyStacking(modern(c, s));

            Assert.That(c.StackHeight, Is.EqualTo(1));
            Assert.That(s.StackHeight, Is.EqualTo(0));
        }

        [Test]
        public void TestStacking_SliderFarFromPreviousInTime_NotStacked()
        {
            // objectI.StartTime - objectN.StartTime > threshold TRUE
            var c = circle(0, 100, 100);
            var s = slider(500, 100, 100, 100);

            OsuBeatmapProcessor.ApplyStacking(modern(c, s));

            Assert.That(c.StackHeight, Is.EqualTo(0));
        }

        [Test]
        public void TestStacking_SliderFarFromPreviousInSpace_NotStacked()
        {
            var c = circle(0, 300, 300);
            var s = slider(100, 100, 100, 100);

            OsuBeatmapProcessor.ApplyStacking(modern(c, s));

            Assert.That(c.StackHeight, Is.EqualTo(0));
        }

        [Test]
        public void TestStacking_SpinnerBeforeSlider_IsSkipped()
        {
            // slider loop `objectN is Spinner` -> continue
            var c = circle(0, 100, 100);
            var sp = spinner(50);
            var s = slider(100, 100, 100, 100);

            OsuBeatmapProcessor.ApplyStacking(modern(c, sp, s));

            Assert.That(c.StackHeight, Is.EqualTo(1));
            Assert.That(sp.StackHeight, Is.EqualTo(0));
        }

        // ---- old algorithm (BeatmapVersion < 6) ----

        [Test]
        public void TestLegacyStacking_TwoCircles_EarlierIsStackedUp()
        {
            var a = circle(0);
            var b = circle(100);

            OsuBeatmapProcessor.ApplyStacking(legacy(a, b));

            Assert.That(a.StackHeight, Is.EqualTo(1));
            Assert.That(b.StackHeight, Is.EqualTo(0));
        }

        [Test]
        public void TestLegacyStacking_ThreeCircles_FormDescendingStack()
        {
            var a = circle(0);
            var b = circle(100);
            var c = circle(200);

            OsuBeatmapProcessor.ApplyStacking(legacy(a, b, c));

            Assert.That(new[] { a.StackHeight, b.StackHeight, c.StackHeight }, Is.EqualTo(new[] { 2, 1, 0 }));
        }

        [Test]
        public void TestLegacyStacking_FarApartInTime_NotStacked()
        {
            // StartTime - threshold > startTime TRUE -> break
            var a = circle(0);
            var b = circle(500);

            OsuBeatmapProcessor.ApplyStacking(legacy(a, b));

            Assert.That(a.StackHeight, Is.EqualTo(0));
        }

        [Test]
        public void TestLegacyStacking_DifferentPositions_NotStacked()
        {
            // both distance checks FALSE
            var a = circle(0, 100, 100);
            var b = circle(100, 200, 200);

            OsuBeatmapProcessor.ApplyStacking(legacy(a, b));

            Assert.That(a.StackHeight, Is.EqualTo(0));
            Assert.That(b.StackHeight, Is.EqualTo(0));
        }

        [Test]
        public void TestLegacyStacking_CircleOnSliderEnd_IsPushedDown()
        {
            // else-if branch (slider end position) with `currHitObject is Slider` TRUE, then the already-stacked circle is skipped as the outer object
            var s = slider(0, 0, 0, 100);
            var c = circle(600, 100, 0);

            OsuBeatmapProcessor.ApplyStacking(legacy(s, c));

            Assert.That(c.StackHeight, Is.EqualTo(-1));
            Assert.That(s.StackHeight, Is.EqualTo(0));
        }

        [Test]
        public void TestLegacyStacking_CircleOnCircleEnd_UsesCircleAsOwnEndPosition()
        {
            // `currHitObject is Slider` FALSE branch of position2 selection: a stack on a circle
            var a = circle(0, 100, 100);
            var b = circle(50, 100, 100);

            OsuBeatmapProcessor.ApplyStacking(legacy(a, b));

            Assert.That(a.StackHeight, Is.EqualTo(1));
        }
    }
}
