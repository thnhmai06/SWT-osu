// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Catch.Edit;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch.UI;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osuTK;

namespace osu.Game.Rulesets.Catch.Tests.Edit
{
    [TestFixture]
    public class CatchHitObjectUtilsTest
    {
        // CF: CatchHitObjectUtils.GetPositionRange | switch case Fruit | taken
        [Test]
        public void TestFruitOccupiesSinglePosition()
        {
            var range = CatchHitObjectUtils.GetPositionRange(new Fruit { OriginalX = 120 });

            Assert.That(range.Min, Is.EqualTo(120f));
            Assert.That(range.Max, Is.EqualTo(120f));
        }

        // CF: CatchHitObjectUtils.GetPositionRange | switch case Droplet, droplet is TinyDroplet | FALSE
        [Test]
        public void TestDropletOccupiesSinglePosition()
        {
            var range = CatchHitObjectUtils.GetPositionRange(new Droplet { OriginalX = 77 });

            Assert.That(range.Min, Is.EqualTo(77f));
            Assert.That(range.Max, Is.EqualTo(77f));
        }

        // CF: CatchHitObjectUtils.GetPositionRange | switch case Droplet, droplet is TinyDroplet | TRUE
        [Test]
        public void TestTinyDropletIsExcludedAndReturnsEmptyRange()
        {
            var range = CatchHitObjectUtils.GetPositionRange(new TinyDroplet { OriginalX = 77 });

            Assert.That(range.Length, Is.EqualTo(0f));
            Assert.That(range.Min, Is.GreaterThan(range.Max));
        }

        // CF: CatchHitObjectUtils.GetPositionRange | switch case JuiceStream | taken (recurses into nested objects)
        [Test]
        public void TestJuiceStreamSpansNestedObjectsExcludingTinyDroplets()
        {
            var stream = new JuiceStream
            {
                OriginalX = 50,
                StartTime = 0,
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(100, 0) }),
            };
            stream.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());

            var range = CatchHitObjectUtils.GetPositionRange(stream);

            // Nested objects are laid along x = 50..150 (head fruit at 50, tail fruit at 150).
            Assert.That(range.Min, Is.EqualTo(50f));
            Assert.That(range.Max, Is.EqualTo(150f));
        }

        // CF: CatchHitObjectUtils.GetPositionRange | switch case BananaShower | taken
        [Test]
        public void TestBananaShowerOccupiesWholePlayfieldWidth()
        {
            var range = CatchHitObjectUtils.GetPositionRange(new BananaShower());

            Assert.That(range.Min, Is.EqualTo(0f));
            Assert.That(range.Max, Is.EqualTo(CatchPlayfield.WIDTH));
        }

        // CF: CatchHitObjectUtils.GetPositionRange | switch default (Banana is neither Fruit/Droplet/JuiceStream/BananaShower) | taken
        [Test]
        public void TestBananaFallsToDefaultAndReturnsEmptyRange()
        {
            var range = CatchHitObjectUtils.GetPositionRange(new Banana { OriginalX = 30 });

            Assert.That(range.Min, Is.GreaterThan(range.Max));
        }

        // CF: CatchHitObjectUtils.GetPositionRange | switch default (plain HitObject) | taken
        [Test]
        public void TestPlainHitObjectFallsToDefaultAndReturnsEmptyRange()
        {
            var range = CatchHitObjectUtils.GetPositionRange(new HitObject());

            Assert.That(range.Min, Is.GreaterThan(range.Max));
        }

        // CF: CatchHitObjectUtils.GetPositionRange(IEnumerable) | Aggregate over empty sequence | returns EMPTY seed
        [Test]
        public void TestEmptySequenceReturnsEmptyRange()
        {
            var range = CatchHitObjectUtils.GetPositionRange(Enumerable.Empty<HitObject>());

            Assert.That(range.Min, Is.GreaterThan(range.Max));
        }

        // CF: CatchHitObjectUtils.GetPositionRange(IEnumerable) | Aggregate over mixed sequence | union ignores tiny droplet
        [Test]
        public void TestMixedSequenceUnionsFruitsAndIgnoresTinyDroplets()
        {
            var range = CatchHitObjectUtils.GetPositionRange(new HitObject[]
            {
                new Fruit { OriginalX = 200 },
                new TinyDroplet { OriginalX = 5 },
                new Fruit { OriginalX = 40 },
            });

            Assert.That(range.Min, Is.EqualTo(40f));
            Assert.That(range.Max, Is.EqualTo(200f));
        }
    }
}
