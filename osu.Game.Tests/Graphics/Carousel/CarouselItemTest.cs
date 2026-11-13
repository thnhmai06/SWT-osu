// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Graphics.Carousel;

namespace osu.Game.Tests.Graphics.Carousel
{
    [TestFixture]
    public class CarouselItemTest
    {
        [Test]
        public void TestDefaults()
        {
            var model = new object();
            var item = new CarouselItem(model);
            Assert.That(item.Model, Is.SameAs(model));
            Assert.That(item.DrawHeight, Is.EqualTo(CarouselItem.DEFAULT_HEIGHT));
            Assert.That(item.IsVisible, Is.True);
            Assert.That(item.IsExpanded, Is.False);
        }

        // CF: CarouselItem.CompareTo | other == null | TRUE
        [Test]
        public void TestCompareToNull() => Assert.That(new CarouselItem(1).CompareTo(null), Is.GreaterThan(0));

        // CF: CarouselItem.CompareTo | other == null | FALSE
        [Test]
        public void TestCompareByYPosition()
        {
            var a = new CarouselItem(1) { CarouselYPosition = 5 };
            var b = new CarouselItem(2) { CarouselYPosition = 10 };
            Assert.That(a.CompareTo(b), Is.LessThan(0));
            Assert.That(b.CompareTo(a), Is.GreaterThan(0));
            Assert.That(a.CompareTo(new CarouselItem(3) { CarouselYPosition = 5 }), Is.EqualTo(0));
        }
    }
}
