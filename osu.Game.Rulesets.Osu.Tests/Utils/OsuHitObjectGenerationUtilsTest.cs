// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Utils;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Utils
{
    [TestFixture]
    public class OsuHitObjectGenerationUtilsTest
    {
        private static void assertVector(Vector2 actual, float x, float y, float tolerance = 1e-3f)
        {
            Assert.That(actual.X, Is.EqualTo(x).Within(tolerance), "X");
            Assert.That(actual.Y, Is.EqualTo(y).Within(tolerance), "Y");
        }

        private static Slider slider(Vector2 position, params Vector2[] pathPoints)
            => new Slider { Position = position, Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero }.Concat(pathPoints).ToArray()) };

        // ---- RotateVectorTowardsVector: angle wrap-around loops ----

        [Test]
        public void TestRotateVectorTowards_RatioZero_ReturnsInitialDirection()
        {
            assertVector(OsuHitObjectGenerationUtils.RotateVectorTowardsVector(new Vector2(0, 10), new Vector2(5, 0), 0), 0, 10);
        }

        [Test]
        public void TestRotateVectorTowards_RatioOne_PointsAtDestinationKeepingLength()
        {
            // diff inside [-pi, pi]: neither wrap loop runs
            assertVector(OsuHitObjectGenerationUtils.RotateVectorTowardsVector(new Vector2(0, 10), new Vector2(5, 0), 1), 10, 0);
        }

        [Test]
        public void TestRotateVectorTowards_HalfRatio_BisectsAngle()
        {
            float expected = 10 * MathF.Cos(MathF.PI / 4);

            assertVector(OsuHitObjectGenerationUtils.RotateVectorTowardsVector(new Vector2(0, 10), new Vector2(5, 0), 0.5f), expected, expected);
        }

        [Test]
        public void TestRotateVectorTowards_DiffBelowMinusPi_TakesShortWayAround()
        {
            // initial 170deg, destination -170deg: raw diff -340deg is wrapped to +20deg (first while loop runs)
            var initial = new Vector2(MathF.Cos(170 * MathF.PI / 180), MathF.Sin(170 * MathF.PI / 180)) * 10;
            var destination = new Vector2(MathF.Cos(-170 * MathF.PI / 180), MathF.Sin(-170 * MathF.PI / 180));

            var result = OsuHitObjectGenerationUtils.RotateVectorTowardsVector(initial, destination, 0.5f);

            // halfway along the short arc = 180deg
            assertVector(result, -10, 0, 1e-2f);
        }

        [Test]
        public void TestRotateVectorTowards_DiffAbovePi_TakesShortWayAround()
        {
            // initial -170deg, destination 170deg: raw diff +340deg is wrapped to -20deg (second while loop runs)
            var initial = new Vector2(MathF.Cos(-170 * MathF.PI / 180), MathF.Sin(-170 * MathF.PI / 180)) * 10;
            var destination = new Vector2(MathF.Cos(170 * MathF.PI / 180), MathF.Sin(170 * MathF.PI / 180));

            var result = OsuHitObjectGenerationUtils.RotateVectorTowardsVector(initial, destination, 0.5f);

            assertVector(result, -10, 0, 1e-2f);
        }

        // ---- RotateAwayFromEdge: X/Y branch combinations and Math.Max / Math.Min arms ----

        [Test]
        public void TestRotateAwayFromEdge_PlayfieldCentre_LeavesVectorUnchanged()
        {
            // X < middle FALSE, Y < middle FALSE, both ratios negative so Math.Max keeps 0
            assertVector(OsuHitObjectGenerationUtils.RotateAwayFromEdge(new Vector2(256, 192), new Vector2(0, 100)), 0, 100);
        }

        [Test]
        public void TestRotateAwayFromEdge_LeftEdge_RotatesTowardsMiddle()
        {
            // X < middle TRUE (ratio 1 -> rotation 0.5); destination (256, 0) at 0deg, initial 90deg => 45deg
            float c = 100 * MathF.Cos(MathF.PI / 4);
            assertVector(OsuHitObjectGenerationUtils.RotateAwayFromEdge(new Vector2(0, 192), new Vector2(0, 100)), c, c);
        }

        [Test]
        public void TestRotateAwayFromEdge_RightEdge_RotatesTowardsMiddle()
        {
            // X < middle FALSE with positive ratio; destination (-256, 0) at 180deg, initial 90deg => 135deg
            float c = 100 * MathF.Cos(MathF.PI / 4);
            assertVector(OsuHitObjectGenerationUtils.RotateAwayFromEdge(new Vector2(512, 192), new Vector2(0, 100)), -c, c);
        }

        [Test]
        public void TestRotateAwayFromEdge_TopEdge_RotatesTowardsMiddle()
        {
            // Y < middle TRUE; destination (0, 192) at 90deg, initial 0deg => 45deg
            float c = 100 * MathF.Cos(MathF.PI / 4);
            assertVector(OsuHitObjectGenerationUtils.RotateAwayFromEdge(new Vector2(256, 0), new Vector2(100, 0)), c, c);
        }

        [Test]
        public void TestRotateAwayFromEdge_BottomEdge_RotatesTowardsMiddle()
        {
            // Y < middle FALSE with positive ratio; destination (0, -192) at -90deg, initial 0deg => -45deg
            float c = 100 * MathF.Cos(MathF.PI / 4);
            assertVector(OsuHitObjectGenerationUtils.RotateAwayFromEdge(new Vector2(256, 384), new Vector2(100, 0)), c, -c);
        }

        [Test]
        public void TestRotateAwayFromEdge_XDistanceDominates_KeepsLargerRatio()
        {
            // prev (0,100): X ratio 1, Y ratio ~0.31 -> Math.Max keeps the X ratio (0.5 rotation)
            var prev = new Vector2(0, 100);
            var rel = new Vector2(0, 100);
            float destAngle = MathF.Atan2(192 - 100, 256 - 0);
            float expectedAngle = MathF.PI / 2 + 0.5f * (destAngle - MathF.PI / 2);

            assertVector(OsuHitObjectGenerationUtils.RotateAwayFromEdge(prev, rel), 100 * MathF.Cos(expectedAngle), 100 * MathF.Sin(expectedAngle));
        }

        [Test]
        public void TestRotateAwayFromEdge_YDistanceDominates_KeepsLargerRatio()
        {
            // prev (200,0): X ratio < 0 (clamped to 0 by Math.Max), Y ratio 1
            var prev = new Vector2(200, 0);
            var rel = new Vector2(100, 0);
            float destAngle = MathF.Atan2(192, 256 - 200);
            float expectedAngle = 0.5f * destAngle;

            assertVector(OsuHitObjectGenerationUtils.RotateAwayFromEdge(prev, rel), 100 * MathF.Cos(expectedAngle), 100 * MathF.Sin(expectedAngle));
        }

        [Test]
        public void TestRotateAwayFromEdge_RotationRatioCappedAtOne()
        {
            // Math.Min(1, 1 * 2) picks 1: vector ends up pointing straight at the middle (0deg)
            assertVector(OsuHitObjectGenerationUtils.RotateAwayFromEdge(new Vector2(0, 192), new Vector2(0, 100), 2f), 100, 0);
        }

        // ---- reflection / flipping / rotation ----

        [Test]
        public void TestReflectHorizontally_Circle_MirrorsXOnly()
        {
            // `osuObject is not Slider` TRUE -> early return
            var circle = new HitCircle { Position = new Vector2(100, 50) };

            OsuHitObjectGenerationUtils.ReflectHorizontallyAlongPlayfield(circle);

            assertVector(circle.Position, 412, 50);
        }

        [Test]
        public void TestReflectHorizontally_Slider_MirrorsPositionAndPath()
        {
            // `osuObject is not Slider` FALSE -> control points mirrored too
            var s = slider(new Vector2(100, 50), new Vector2(60, 30));

            OsuHitObjectGenerationUtils.ReflectHorizontallyAlongPlayfield(s);

            assertVector(s.Position, 412, 50);
            assertVector(s.Path.ControlPoints[1].Position, -60, 30);
        }

        [Test]
        public void TestReflectVertically_Circle_MirrorsYOnly()
        {
            var circle = new HitCircle { Position = new Vector2(100, 50) };

            OsuHitObjectGenerationUtils.ReflectVerticallyAlongPlayfield(circle);

            assertVector(circle.Position, 100, 334);
        }

        [Test]
        public void TestReflectVertically_Slider_MirrorsPositionAndPath()
        {
            var s = slider(new Vector2(100, 50), new Vector2(60, 30));

            OsuHitObjectGenerationUtils.ReflectVerticallyAlongPlayfield(s);

            assertVector(s.Position, 100, 334);
            assertVector(s.Path.ControlPoints[1].Position, 60, -30);
        }

        [Test]
        public void TestFlipSliderInPlace_KeepsPositionAndMirrorsPath()
        {
            var s = slider(new Vector2(100, 50), new Vector2(60, 30));

            OsuHitObjectGenerationUtils.FlipSliderInPlaceHorizontally(s);

            assertVector(s.Position, 100, 50);
            assertVector(s.Path.ControlPoints[1].Position, -60, 30);
        }

        [Test]
        public void TestRotateSlider_RotatesControlPointsAroundOrigin()
        {
            var s = slider(new Vector2(100, 50), new Vector2(100, 0));

            OsuHitObjectGenerationUtils.RotateSlider(s, MathF.PI / 2);

            assertVector(s.Position, 100, 50);
            assertVector(s.Path.ControlPoints[1].Position, 0, 100, 1e-2f);
        }

        [Test]
        public void TestModifySlider_PreservesExpectedDistanceAndPathType()
        {
            var s = new Slider { Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(100, 0) }, 80) };

            OsuHitObjectGenerationUtils.FlipSliderInPlaceHorizontally(s);

            Assert.That(s.Path.ExpectedDistance.Value, Is.EqualTo(80));
            Assert.That(s.Path.ControlPoints[0].Type, Is.EqualTo(PathType.LINEAR));
        }

        // ---- IsHitObjectOnBeat ----

        private static OsuBeatmap beatmapWithBeatLength(double beatLength)
        {
            var beatmap = new OsuBeatmap();
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = beatLength, TimeSignature = TimeSignature.SimpleQuadruple });
            return beatmap;
        }

        [TestCase(0, true)]
        [TestCase(499, true)] // `+ 1` offset => lands exactly on the beat
        [TestCase(500, true)] // within the 1ms tolerance
        [TestCase(501, false)]
        [TestCase(250, false)]
        public void TestIsHitObjectOnBeat_EveryBeat(double time, bool expected)
        {
            // downbeatsOnly FALSE (default)
            Assert.That(OsuHitObjectGenerationUtils.IsHitObjectOnBeat(beatmapWithBeatLength(500), new HitCircle { StartTime = time }), Is.EqualTo(expected));
        }

        [TestCase(1999, true)]
        [TestCase(500, false)] // on a beat, but not a downbeat
        public void TestIsHitObjectOnBeat_DownbeatsOnly(double time, bool expected)
        {
            // downbeatsOnly TRUE: beat length multiplied by the time signature numerator (4)
            Assert.That(OsuHitObjectGenerationUtils.IsHitObjectOnBeat(beatmapWithBeatLength(500), new HitCircle { StartTime = time }, true), Is.EqualTo(expected));
        }

        // ---- RandomGaussian ----

        [Test]
        public void TestRandomGaussian_UsesBoxMullerTransform()
        {
            // x1 = 1 - 0.5, x2 = 1 - 0.25 => sqrt(-2 ln 0.5) * sin(1.5 pi) = -1.17741
            var rng = new SequenceRandom(0.5, 0.25);

            Assert.That(OsuHitObjectGenerationUtils.RandomGaussian(rng), Is.EqualTo(-1.17741f).Within(1e-4));
        }

        [Test]
        public void TestRandomGaussian_ScalesAndShiftsByMeanAndDeviation()
        {
            var rng = new SequenceRandom(0.5, 0.25);

            Assert.That(OsuHitObjectGenerationUtils.RandomGaussian(rng, 10, 2), Is.EqualTo(10 + 2 * -1.17741f).Within(1e-3));
        }

        [Test]
        public void TestRandomGaussian_SeededDistributionHasRequestedMoments()
        {
            var rng = new Random(1234);
            var samples = Enumerable.Range(0, 5000).Select(_ => OsuHitObjectGenerationUtils.RandomGaussian(rng, 3, 2)).ToArray();

            double mean = samples.Average();
            double std = Math.Sqrt(samples.Sum(s => (s - mean) * (s - mean)) / samples.Length);

            Assert.That(mean, Is.EqualTo(3).Within(0.2));
            Assert.That(std, Is.EqualTo(2).Within(0.2));
        }

        private class SequenceRandom : Random
        {
            private readonly double[] values;
            private int index;

            public SequenceRandom(params double[] values)
            {
                this.values = values;
            }

            public override double NextDouble() => values[index++ % values.Length];
        }
    }
}
