// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Mods
{
    [TestFixture]
    public class OsuModHardRockMirrorTest
    {
        // ---- HardRock: difficulty ----

        [Test]
        public void TestHardRock_ScalesBelowCap()
        {
            // Math.Min picks the scaled value: OD/AR x1.4, CS x1.3
            var difficulty = new BeatmapDifficulty { OverallDifficulty = 5, CircleSize = 5, ApproachRate = 5 };

            new OsuModHardRock().ApplyToDifficulty(difficulty);

            Assert.That(difficulty.OverallDifficulty, Is.EqualTo(7).Within(1e-4));
            Assert.That(difficulty.CircleSize, Is.EqualTo(6.5f).Within(1e-4));
            Assert.That(difficulty.ApproachRate, Is.EqualTo(7).Within(1e-4));
        }

        [Test]
        public void TestHardRock_ClampsAtTen()
        {
            // Math.Min picks the constant 10
            var difficulty = new BeatmapDifficulty { OverallDifficulty = 9, CircleSize = 9, ApproachRate = 9 };

            new OsuModHardRock().ApplyToDifficulty(difficulty);

            Assert.That(difficulty.OverallDifficulty, Is.EqualTo(10));
            Assert.That(difficulty.CircleSize, Is.EqualTo(10));
            Assert.That(difficulty.ApproachRate, Is.EqualTo(10));
        }

        // ---- HardRock: objects ----

        [Test]
        public void TestHardRock_FlipsCirclesVertically()
        {
            var circle = new HitCircle { Position = new Vector2(100, 50) };

            new OsuModHardRock().ApplyToHitObject(circle);

            Assert.That(circle.Position, Is.EqualTo(new Vector2(100, 334)));
        }

        [Test]
        public void TestHardRock_FlipsSliderPathVertically()
        {
            var slider = new Slider { Position = new Vector2(100, 50), Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(60, 30) }) };

            new OsuModHardRock().ApplyToHitObject(slider);

            Assert.That(slider.Position, Is.EqualTo(new Vector2(100, 334)));
            Assert.That(slider.Path.ControlPoints[1].Position, Is.EqualTo(new Vector2(60, -30)));
        }

        [Test]
        public void TestHardRock_IncompatibleWithMirror()
        {
            Assert.That(new OsuModHardRock().IncompatibleMods, Does.Contain(typeof(ModMirror)));
        }

        // ---- Mirror: switch over MirrorType ----

        private static Vector2 mirrored(OsuModMirror.MirrorType type)
        {
            var circle = new HitCircle { Position = new Vector2(100, 50) };
            new OsuModMirror { Reflection = { Value = type } }.ApplyToHitObject(circle);
            return circle.Position;
        }

        [Test]
        public void TestMirror_Horizontal() => Assert.That(mirrored(OsuModMirror.MirrorType.Horizontal), Is.EqualTo(new Vector2(412, 50)));

        [Test]
        public void TestMirror_Vertical() => Assert.That(mirrored(OsuModMirror.MirrorType.Vertical), Is.EqualTo(new Vector2(100, 334)));

        [Test]
        public void TestMirror_Both() => Assert.That(mirrored(OsuModMirror.MirrorType.Both), Is.EqualTo(new Vector2(412, 334)));

        [Test]
        public void TestMirror_UndefinedValue_LeavesObjectUntouched()
        {
            // switch: no case matches
            Assert.That(mirrored((OsuModMirror.MirrorType)42), Is.EqualTo(new Vector2(100, 50)));
        }

        [Test]
        public void TestMirror_SliderPathFollowsAxis()
        {
            var slider = new Slider { Position = new Vector2(100, 50), Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(60, 30) }) };

            new OsuModMirror { Reflection = { Value = OsuModMirror.MirrorType.Horizontal } }.ApplyToHitObject(slider);

            Assert.That(slider.Path.ControlPoints[1].Position, Is.EqualTo(new Vector2(-60, 30)));
        }

        [Test]
        public void TestMirror_IncompatibleWithHardRock()
        {
            Assert.That(new OsuModMirror().IncompatibleMods, Does.Contain(typeof(ModHardRock)));
        }

        [Test]
        public void TestDefaultMirrorAxis_IsHorizontal()
        {
            Assert.That(new OsuModMirror().Reflection.Value, Is.EqualTo(OsuModMirror.MirrorType.Horizontal));
        }
    }
}
