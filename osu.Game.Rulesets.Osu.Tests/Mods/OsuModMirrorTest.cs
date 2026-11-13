// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Mods
{
    [TestFixture]
    public class OsuModMirrorTest
    {
        private static Vector2 apply(OsuModMirror.MirrorType type)
        {
            var mod = new OsuModMirror { Reflection = { Value = type } };
            var circle = new HitCircle { Position = new Vector2(100, 50) };
            mod.ApplyToHitObject(circle);
            return circle.Position;
        }

        // CF: OsuModMirror.ApplyToHitObject | switch (Reflection.Value) | case Horizontal
        [Test]
        public void TestHorizontal() => Assert.That(apply(OsuModMirror.MirrorType.Horizontal), Is.EqualTo(new Vector2(412, 50)));

        // CF: OsuModMirror.ApplyToHitObject | switch (Reflection.Value) | case Vertical
        [Test]
        public void TestVertical() => Assert.That(apply(OsuModMirror.MirrorType.Vertical), Is.EqualTo(new Vector2(100, 334)));

        // CF: OsuModMirror.ApplyToHitObject | switch (Reflection.Value) | case Both
        [Test]
        public void TestBoth() => Assert.That(apply(OsuModMirror.MirrorType.Both), Is.EqualTo(new Vector2(412, 334)));

        // CF: OsuModMirror.ApplyToHitObject | switch (Reflection.Value) | default
        [Test]
        public void TestUnknownReflectionLeavesObjectUntouched() => Assert.That(apply((OsuModMirror.MirrorType)99), Is.EqualTo(new Vector2(100, 50)));
    }
}
