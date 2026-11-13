// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Tests.Rulesets.Mods
{
    [TestFixture]
    public class ModWithVisibilityAdjustmentTest
    {
        private class TestMod : ModWithVisibilityAdjustment
        {
            private readonly Func<HitObject, bool>? predicate;

            public TestMod(Func<HitObject, bool>? predicate = null)
            {
                this.predicate = predicate;
            }

            public override string Name => "test";
            public override string Acronym => "TT";
            public override osu.Framework.Localisation.LocalisableString Description => "test";
            public override double ScoreMultiplier => 1;

            public HitObject? First => FirstObject;

            protected override bool IsFirstAdjustableObject(HitObject hitObject) => predicate?.Invoke(hitObject) ?? base.IsFirstAdjustableObject(hitObject);

            protected override void ApplyIncreasedVisibilityState(DrawableHitObject hitObject, ArmedState state)
            {
            }

            protected override void ApplyNormalVisibilityState(DrawableHitObject hitObject, ArmedState state)
            {
            }
        }

        private static Beatmap beatmap(params HitObject[] objects)
        {
            var b = new Beatmap();
            b.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });
            b.HitObjects.AddRange(objects);
            return b;
        }

        // CF: ModWithVisibilityAdjustment.ApplyToBeatmap | IsFirstAdjustableObject(h) | TRUE
        [Test]
        public void TestFirstObjectIsFirstByDefault()
        {
            var first = new HitCircle { StartTime = 100 };
            var mod = new TestMod();

            mod.ApplyToBeatmap(beatmap(first, new HitCircle { StartTime = 200 }));

            Assert.That(mod.First, Is.SameAs(first));
        }

        // CF: ModWithVisibilityAdjustment.ApplyToBeatmap | IsFirstAdjustableObject(h) | FALSE
        [Test]
        public void TestSkipsObjectsThatAreNotAdjustable()
        {
            var target = new HitCircle { StartTime = 300 };
            var mod = new TestMod(h => h.StartTime >= 250);

            mod.ApplyToBeatmap(beatmap(new HitCircle { StartTime = 100 }, target));

            Assert.That(mod.First, Is.SameAs(target));
        }

        // CF: ModWithVisibilityAdjustment.ApplyToBeatmap | nestedResult != null | TRUE
        [Test]
        public void TestSearchesNestedObjects()
        {
            var slider = new Slider
            {
                StartTime = 0,
                Path = new SliderPath(PathType.LINEAR, new[] { osuTK.Vector2.Zero, new osuTK.Vector2(200, 0) }),
            };
            slider.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());
            var mod = new TestMod(h => h is SliderTick);

            mod.ApplyToBeatmap(beatmap(new HitCircle { StartTime = 5000 }, slider));

            Assert.That(mod.First, Is.InstanceOf<SliderTick>());
            Assert.That(mod.First, Is.SameAs(slider.NestedHitObjects.OfType<SliderTick>().First()));
        }

        // CF: ModWithVisibilityAdjustment.ApplyToBeatmap | nestedResult != null | FALSE
        // CF: ModWithVisibilityAdjustment.ApplyToBeatmap | foreach h in hitObjects | loop 0
        [Test]
        public void TestNoAdjustableObject()
        {
            var mod = new TestMod(_ => false);
            mod.ApplyToBeatmap(beatmap(new HitCircle()));
            Assert.That(mod.First, Is.Null);

            mod.ApplyToBeatmap(beatmap());
            Assert.That(mod.First, Is.Null);
        }
    }
}
