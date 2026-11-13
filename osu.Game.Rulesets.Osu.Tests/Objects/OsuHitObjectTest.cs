// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Objects
{
    [TestFixture]
    public class OsuHitObjectTest
    {
        private static HitCircle applyDefaults(float approachRate)
        {
            var circle = new HitCircle();
            circle.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty { ApproachRate = approachRate, CircleSize = 5 });
            return circle;
        }

        [Test]
        public void TestPositionAccessors()
        {
            var circle = new HitCircle { Position = new Vector2(10, 20) };

            circle.X = 30;
            circle.Y = 40;

            Assert.That(circle.Position, Is.EqualTo(new Vector2(30, 40)));
            Assert.That(circle.EndPosition, Is.EqualTo(new Vector2(30, 40)));
        }

        [Test]
        public void TestStackOffset()
        {
            var circle = new HitCircle { Position = new Vector2(100, 100), StackHeight = 2, Scale = 0.5f };

            Assert.That(circle.StackOffset.X, Is.EqualTo(-6.4f).Within(1e-4));
            Assert.That(circle.StackedPosition.X, Is.EqualTo(93.6f).Within(1e-4));
            Assert.That(circle.StackedEndPosition, Is.EqualTo(circle.StackedPosition));
            Assert.That(circle.Radius, Is.EqualTo(32));
        }

        // CF: OsuHitObject.ctor | nested is OsuHitObject osuHitObject | TRUE
        // CF: OsuHitObject.ctor | foreach NestedHitObjects | loop n
        [Test]
        public void TestStackHeightPropagatesToNestedObjects()
        {
            var slider = new Slider
            {
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(100, 0) }),
            };
            slider.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());

            slider.StackHeight = 3;

            Assert.That(slider.NestedHitObjects, Is.Not.Empty);
            Assert.That(slider.NestedHitObjects.OfType<OsuHitObject>().All(n => n.StackHeight == 3), Is.True);
        }

        // CF: OsuHitObject.ctor | foreach NestedHitObjects | loop 0
        [Test]
        public void TestStackHeightWithoutNestedObjects()
        {
            var circle = new HitCircle();
            Assert.DoesNotThrow(() => circle.StackHeight = 2);
            Assert.That(circle.StackHeight, Is.EqualTo(2));
        }

        // CF: OsuHitObject.ApplyDefaultsToSelf | Math.Min(1, TimePreempt / PREEMPT_MIN) | non-null
        [Test]
        public void TestPreemptAndFadeInAtAr5()
        {
            var circle = applyDefaults(5);

            Assert.That(circle.TimePreempt, Is.EqualTo(1200));
            Assert.That(circle.TimeFadeIn, Is.EqualTo(400));
        }

        [Test]
        public void TestPreemptAndFadeInAtAr10()
        {
            var circle = applyDefaults(10);

            Assert.That(circle.TimePreempt, Is.EqualTo(450));
            Assert.That(circle.TimeFadeIn, Is.EqualTo(400));
        }

        [Test]
        public void TestFadeInShrinksWhenPreemptBelowMinimum()
        {
            var circle = applyDefaults(11);

            Assert.That(circle.TimePreempt, Is.LessThan(OsuHitObject.PREEMPT_MIN));
            Assert.That(circle.TimeFadeIn, Is.EqualTo(400 * circle.TimePreempt / OsuHitObject.PREEMPT_MIN).Within(1e-6));
        }

        [Test]
        public void TestScaleFromCircleSize()
        {
            Assert.That(applyDefaults(5).Scale, Is.EqualTo(0.5f).Within(1e-3));
        }

        // CF: OsuHitObject.UpdateComboInformation | lastObj?.ComboIndex ?? 0 | null
        // CF: OsuHitObject.UpdateComboInformation | this is not Spinner | TRUE
        // CF: OsuHitObject.UpdateComboInformation | NewCombo | FALSE
        // CF: OsuHitObject.UpdateComboInformation | lastObj == null | TRUE
        // CF: OsuHitObject.UpdateComboInformation | lastObj?.LastInCombo = true | null
        [Test]
        public void TestFirstObjectStartsCombo()
        {
            var circle = new HitCircle { ComboOffset = 2 };
            circle.UpdateComboInformation(null);

            Assert.That(circle.ComboIndex, Is.EqualTo(1));
            Assert.That(circle.ComboIndexWithOffsets, Is.EqualTo(3));
            Assert.That(circle.IndexInCurrentCombo, Is.EqualTo(0));
        }

        // CF: OsuHitObject.UpdateComboInformation | lastObj?.ComboIndex ?? 0 | non-null
        // CF: OsuHitObject.UpdateComboInformation | NewCombo | FALSE
        // CF: OsuHitObject.UpdateComboInformation | lastObj == null | FALSE
        // CF: OsuHitObject.UpdateComboInformation | lastObj is Spinner | FALSE
        [Test]
        public void TestObjectContinuesCombo()
        {
            var first = new HitCircle();
            first.UpdateComboInformation(null);
            var second = new HitCircle();
            second.UpdateComboInformation(first);

            Assert.That(second.ComboIndex, Is.EqualTo(first.ComboIndex));
            Assert.That(second.IndexInCurrentCombo, Is.EqualTo(1));
            Assert.That(first.LastInCombo, Is.False);
        }

        // CF: OsuHitObject.UpdateComboInformation | NewCombo | TRUE
        // CF: OsuHitObject.UpdateComboInformation | lastObj?.LastInCombo = true | non-null
        [Test]
        public void TestNewComboMarksPreviousAsLast()
        {
            var first = new HitCircle();
            first.UpdateComboInformation(null);
            var second = new HitCircle { NewCombo = true, ComboOffset = 1 };
            second.UpdateComboInformation(first);

            Assert.That(second.ComboIndex, Is.EqualTo(first.ComboIndex + 1));
            Assert.That(second.ComboIndexWithOffsets, Is.EqualTo(first.ComboIndexWithOffsets + 2));
            Assert.That(second.IndexInCurrentCombo, Is.EqualTo(0));
            Assert.That(first.LastInCombo, Is.True);
        }

        // CF: OsuHitObject.UpdateComboInformation | lastObj is Spinner | TRUE
        [Test]
        public void TestObjectAfterSpinnerStartsCombo()
        {
            var spinner = new Spinner();
            spinner.UpdateComboInformation(null);
            var circle = new HitCircle();
            circle.UpdateComboInformation(spinner);

            Assert.That(circle.ComboIndex, Is.EqualTo(spinner.ComboIndex + 1));
            Assert.That(circle.IndexInCurrentCombo, Is.EqualTo(0));
        }

        // CF: OsuHitObject.UpdateComboInformation | this is not Spinner | FALSE
        [Test]
        public void TestSpinnerNeverStartsCombo()
        {
            var first = new HitCircle();
            first.UpdateComboInformation(null);
            var spinner = new Spinner { NewCombo = true };
            spinner.UpdateComboInformation(first);

            Assert.That(spinner.ComboIndex, Is.EqualTo(first.ComboIndex));
            Assert.That(spinner.IndexInCurrentCombo, Is.EqualTo(1));
            Assert.That(first.LastInCombo, Is.False);
        }
    }
}
