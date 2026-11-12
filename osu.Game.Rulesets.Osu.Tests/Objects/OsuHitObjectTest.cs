// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Objects
{
    [TestFixture]
    public class OsuHitObjectTest
    {
        private static BeatmapDifficulty difficulty(float ar = 5, float cs = 5) => new BeatmapDifficulty { ApproachRate = ar, CircleSize = cs };

        private static HitCircle applied(float ar = 5, float cs = 5)
        {
            var circle = new HitCircle();
            circle.ApplyDefaults(new ControlPointInfo(), difficulty(ar, cs));
            return circle;
        }

        // ---- ApplyDefaultsToSelf ----

        [TestCase(0, 1800)]
        [TestCase(5, 1200)]
        [TestCase(10, 450)]
        public void TestApplyDefaults_PreemptFollowsApproachRate(float ar, double expectedPreempt)
        {
            Assert.That(applied(ar).TimePreempt, Is.EqualTo(expectedPreempt).Within(1e-6));
        }

        [Test]
        public void TestApplyDefaults_NormalPreempt_FullFadeIn()
        {
            // Math.Min(1, preempt / 450): ratio >= 1 picks 1
            Assert.That(applied(5).TimeFadeIn, Is.EqualTo(400).Within(1e-6));
        }

        [Test]
        public void TestApplyDefaults_PreemptBelowMinimum_ShortensFadeIn()
        {
            // AR 11 extends the linear range past 450ms (to 300ms); Math.Min picks the ratio < 1
            var circle = applied(11);

            Assert.That(circle.TimePreempt, Is.EqualTo(300).Within(1e-6));
            Assert.That(circle.TimeFadeIn, Is.EqualTo(400 * 300 / 450.0).Within(1e-6));
        }

        [Test]
        public void TestApplyDefaults_LargerCircleSizeGivesSmallerScale()
        {
            float small = applied(cs: 2).Scale;
            float large = applied(cs: 7).Scale;

            Assert.That(small, Is.GreaterThan(large));
            Assert.That(large, Is.GreaterThan(0));
        }

        [Test]
        public void TestHitWindows_AreOsuHitWindows()
        {
            Assert.That(applied().HitWindows, Is.TypeOf<OsuHitWindows>());
        }

        // ---- geometry properties ----

        [Test]
        public void TestXAndY_AreComponentsOfPosition()
        {
            var circle = new HitCircle { Position = new Vector2(10, 20) };

            circle.X = 30;
            Assert.That(circle.Position, Is.EqualTo(new Vector2(30, 20)));

            circle.Y = 40;
            Assert.That(circle.Position, Is.EqualTo(new Vector2(30, 40)));
        }

        [Test]
        public void TestStackOffsetAndStackedPositions()
        {
            var circle = new HitCircle { Position = new Vector2(100, 100), StackHeight = 2, Scale = 0.5f };

            // StackHeight * Scale * -6.4 on both axes
            Assert.That(circle.StackOffset.X, Is.EqualTo(-6.4f).Within(1e-5));
            Assert.That(circle.StackedPosition.X, Is.EqualTo(93.6f).Within(1e-4));
            Assert.That(circle.StackedEndPosition, Is.EqualTo(circle.StackedPosition));
        }

        [Test]
        public void TestRadius_ScalesObjectRadius()
        {
            Assert.That(new HitCircle { Scale = 0.5f }.Radius, Is.EqualTo(OsuHitObject.OBJECT_RADIUS * 0.5));
        }

        // ---- stack height propagation to nested objects ----

        [Test]
        public void TestStackHeightChange_PropagatesToNestedOsuObjectsOnly()
        {
            // `nested is OsuHitObject` TRUE for the circle, FALSE for the plain hit object
            var parent = new ParentWithNested();
            parent.ApplyDefaults(new ControlPointInfo(), difficulty());

            parent.StackHeight = 3;

            Assert.That(parent.NestedCircle.StackHeight, Is.EqualTo(3));
        }

        // ---- UpdateComboInformation ----

        private static HitCircle previous() => new HitCircle { ComboIndex = 3, ComboIndexWithOffsets = 5, IndexInCurrentCombo = 4 };

        [Test]
        public void TestCombo_NewComboFlag_StartsNewCombo()
        {
            // NewCombo TRUE (lastObj non-null, not a spinner)
            var last = previous();
            var circle = new HitCircle { NewCombo = true, ComboOffset = 2 };

            circle.UpdateComboInformation(last);

            Assert.That(circle.ComboIndex, Is.EqualTo(4));
            Assert.That(circle.ComboIndexWithOffsets, Is.EqualTo(5 + 2 + 1));
            Assert.That(circle.IndexInCurrentCombo, Is.EqualTo(0));
            Assert.That(last.LastInCombo, Is.True);
        }

        [Test]
        public void TestCombo_NoPreviousObject_StartsNewCombo()
        {
            // NewCombo FALSE, lastObj == null TRUE; `lastObj?.` guards are null-safe
            var circle = new HitCircle();

            circle.UpdateComboInformation(null);

            Assert.That(circle.ComboIndex, Is.EqualTo(1));
            Assert.That(circle.ComboIndexWithOffsets, Is.EqualTo(1));
            Assert.That(circle.IndexInCurrentCombo, Is.EqualTo(0));
        }

        [Test]
        public void TestCombo_AfterSpinner_StartsNewCombo()
        {
            // NewCombo FALSE, lastObj == null FALSE, lastObj is Spinner TRUE
            var spinner = new Spinner { ComboIndex = 3, ComboIndexWithOffsets = 5, IndexInCurrentCombo = 4 };
            var circle = new HitCircle();

            circle.UpdateComboInformation(spinner);

            Assert.That(circle.ComboIndex, Is.EqualTo(4));
            Assert.That(circle.IndexInCurrentCombo, Is.EqualTo(0));
            Assert.That(spinner.LastInCombo, Is.True);
        }

        [Test]
        public void TestCombo_ContinuesPreviousCombo()
        {
            // all three sub-conditions FALSE
            var last = previous();
            var circle = new HitCircle();

            circle.UpdateComboInformation(last);

            Assert.That(circle.ComboIndex, Is.EqualTo(3));
            Assert.That(circle.ComboIndexWithOffsets, Is.EqualTo(5));
            Assert.That(circle.IndexInCurrentCombo, Is.EqualTo(5));
            Assert.That(last.LastInCombo, Is.False);
        }

        [Test]
        public void TestCombo_SpinnerNeverStartsNewCombo()
        {
            // `this is not Spinner` FALSE: NewCombo is ignored
            var last = previous();
            var spinner = new Spinner { NewCombo = true };

            spinner.UpdateComboInformation(last);

            Assert.That(spinner.ComboIndex, Is.EqualTo(3));
            Assert.That(spinner.IndexInCurrentCombo, Is.EqualTo(5));
            Assert.That(last.LastInCombo, Is.False);
        }

        private class ParentWithNested : OsuHitObject
        {
            public HitCircle NestedCircle { get; } = new HitCircle();

            protected override void CreateNestedHitObjects(CancellationToken cancellationToken)
            {
                base.CreateNestedHitObjects(cancellationToken);
                AddNested(new PlainHitObject());
                AddNested(NestedCircle);
            }
        }

        private class PlainHitObject : HitObject
        {
        }
    }
}
