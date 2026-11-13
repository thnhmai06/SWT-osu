// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Game.Storyboards;
using osuTK;

namespace osu.Game.Tests.Storyboards
{
    [TestFixture]
    public class StoryboardExtensionsTest
    {
        private static readonly Vector2 identity = Vector2.One;

        // CF: StoryboardExtensions.AdjustOrigin | flipH ^ (vectorScale.X < 0) | FALSE
        // CF: StoryboardExtensions.AdjustOrigin | flipV ^ (vectorScale.Y < 0) | FALSE
        [TestCase(Anchor.TopLeft)]
        [TestCase(Anchor.Centre)]
        [TestCase(Anchor.BottomRight)]
        public void TestNoFlipKeepsOrigin(Anchor origin)
        {
            Assert.That(StoryboardExtensions.AdjustOrigin(origin, identity, false, false), Is.EqualTo(origin));
        }

        // CF: StoryboardExtensions.AdjustOrigin | origin.HasFlag(Anchor.x0) | TRUE
        [TestCase(Anchor.TopLeft, Anchor.TopRight)]
        [TestCase(Anchor.CentreLeft, Anchor.CentreRight)]
        [TestCase(Anchor.BottomLeft, Anchor.BottomRight)]
        public void TestHorizontalFlipSwapsLeftToRight(Anchor origin, Anchor expected)
        {
            Assert.That(StoryboardExtensions.AdjustOrigin(origin, identity, true, false), Is.EqualTo(expected));
        }

        // CF: StoryboardExtensions.AdjustOrigin | origin.HasFlag(Anchor.x0) | FALSE
        // CF: StoryboardExtensions.AdjustOrigin | origin.HasFlag(Anchor.x2) | TRUE
        [TestCase(Anchor.TopRight, Anchor.TopLeft)]
        [TestCase(Anchor.CentreRight, Anchor.CentreLeft)]
        [TestCase(Anchor.BottomRight, Anchor.BottomLeft)]
        public void TestHorizontalFlipSwapsRightToLeft(Anchor origin, Anchor expected)
        {
            Assert.That(StoryboardExtensions.AdjustOrigin(origin, identity, true, false), Is.EqualTo(expected));
        }

        // CF: StoryboardExtensions.AdjustOrigin | origin.HasFlag(Anchor.x2) | FALSE
        [TestCase(Anchor.TopCentre)]
        [TestCase(Anchor.Centre)]
        [TestCase(Anchor.BottomCentre)]
        public void TestHorizontalFlipLeavesCentreColumn(Anchor origin)
        {
            Assert.That(StoryboardExtensions.AdjustOrigin(origin, identity, true, false), Is.EqualTo(origin));
        }

        [Test]
        public void TestNegativeXScaleActsLikeHorizontalFlip()
        {
            Assert.That(StoryboardExtensions.AdjustOrigin(Anchor.TopLeft, new Vector2(-1, 1), false, false), Is.EqualTo(Anchor.TopRight));
        }

        [Test]
        public void TestFlipAndNegativeXScaleCancelOut()
        {
            Assert.That(StoryboardExtensions.AdjustOrigin(Anchor.TopLeft, new Vector2(-1, 1), true, false), Is.EqualTo(Anchor.TopLeft));
        }

        // CF: StoryboardExtensions.AdjustOrigin | flipV ^ (vectorScale.Y < 0) | TRUE
        // CF: StoryboardExtensions.AdjustOrigin | origin.HasFlag(Anchor.y0) | TRUE
        [TestCase(Anchor.TopLeft, Anchor.BottomLeft)]
        [TestCase(Anchor.TopCentre, Anchor.BottomCentre)]
        [TestCase(Anchor.TopRight, Anchor.BottomRight)]
        public void TestVerticalFlipSwapsTopToBottom(Anchor origin, Anchor expected)
        {
            Assert.That(StoryboardExtensions.AdjustOrigin(origin, identity, false, true), Is.EqualTo(expected));
        }

        // CF: StoryboardExtensions.AdjustOrigin | origin.HasFlag(Anchor.y0) | FALSE
        // CF: StoryboardExtensions.AdjustOrigin | origin.HasFlag(Anchor.y2) | TRUE
        [TestCase(Anchor.BottomLeft, Anchor.TopLeft)]
        [TestCase(Anchor.BottomCentre, Anchor.TopCentre)]
        [TestCase(Anchor.BottomRight, Anchor.TopRight)]
        public void TestVerticalFlipSwapsBottomToTop(Anchor origin, Anchor expected)
        {
            Assert.That(StoryboardExtensions.AdjustOrigin(origin, identity, false, true), Is.EqualTo(expected));
        }

        // CF: StoryboardExtensions.AdjustOrigin | origin.HasFlag(Anchor.y2) | FALSE
        [TestCase(Anchor.CentreLeft)]
        [TestCase(Anchor.Centre)]
        [TestCase(Anchor.CentreRight)]
        public void TestVerticalFlipLeavesCentreRow(Anchor origin)
        {
            Assert.That(StoryboardExtensions.AdjustOrigin(origin, identity, false, true), Is.EqualTo(origin));
        }

        [Test]
        public void TestNegativeYScaleActsLikeVerticalFlipAndBothAxesCombine()
        {
            Assert.That(StoryboardExtensions.AdjustOrigin(Anchor.TopLeft, new Vector2(1, -1), false, false), Is.EqualTo(Anchor.BottomLeft));
            Assert.That(StoryboardExtensions.AdjustOrigin(Anchor.TopLeft, identity, true, true), Is.EqualTo(Anchor.BottomRight));
        }
    }
}
