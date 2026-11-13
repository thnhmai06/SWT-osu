// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Transforms;
using osu.Game.Graphics.UserInterface;
using osu.Game.Utils;
using osuTK.Graphics;

namespace osu.Game.Tests.Utils
{
    [TestFixture]
    public class SmallUtilsTest
    {
        private CultureInfo previousCulture = null!;
        private CultureInfo previousUICulture = null!;

        [SetUp]
        public void SetUp()
        {
            previousCulture = CultureInfo.CurrentCulture;
            previousUICulture = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
        }

        [TearDown]
        public void TearDown()
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUICulture;
        }

        #region OrdinalSortByCaseStringComparer

        // CF: OrdinalSortByCaseStringComparer.Compare | result == 0 | FALSE
        [Test]
        public void TestComparerDifferentLetters() => Assert.That(OrdinalSortByCaseStringComparer.DEFAULT.Compare("a", "B"), Is.LessThan(0));

        // CF: OrdinalSortByCaseStringComparer.Compare | result == 0 | TRUE
        [Test]
        public void TestComparerLowercaseBeforeUppercase()
        {
            Assert.That(OrdinalSortByCaseStringComparer.DEFAULT.Compare("a", "A"), Is.LessThan(0));
            Assert.That(OrdinalSortByCaseStringComparer.DEFAULT.Compare("A", "a"), Is.GreaterThan(0));
            Assert.That(OrdinalSortByCaseStringComparer.DEFAULT.Compare("a", "a"), Is.EqualTo(0));
        }

        #endregion

        #region HumanizerUtils

        [Test]
        public void TestHumanizeTimeSpan() => Assert.That(HumanizerUtils.Humanize(TimeSpan.FromHours(2)), Is.EqualTo("2 hours"));

        [Test]
        public void TestHumanizeDate() => Assert.That(HumanizerUtils.Humanize(DateTimeOffset.UtcNow.AddHours(-3).AddMinutes(-5)), Is.EqualTo("3 hours ago"));

        #endregion

        #region LegacyUtils

        // CF: LegacyUtils.InterpolateNonLinear | startColour == endColour | TRUE
        [Test]
        public void TestInterpolateSameColour() => Assert.That(LegacyUtils.InterpolateNonLinear(5, Color4.Red, Color4.Red, 0, 10), Is.EqualTo(Color4.Red));

        // CF: LegacyUtils.InterpolateNonLinear | startColour == endColour | FALSE
        // CF: LegacyUtils.InterpolateNonLinear | duration == 0 || current == 0 | TRUE
        // CF: LegacyUtils.InterpolateNonLinear | duration == 0 | TRUE
        // CF: LegacyUtils.InterpolateNonLinear | current == 0 | TRUE
        [Test]
        public void TestInterpolateAtStartOrZeroDuration()
        {
            Assert.That(LegacyUtils.InterpolateNonLinear(0, Color4.Black, Color4.White, 0, 10), Is.EqualTo(Color4.Black));
            Assert.That(LegacyUtils.InterpolateNonLinear(5, Color4.Black, Color4.White, 10, 10), Is.EqualTo(Color4.Black));
        }

        // CF: LegacyUtils.InterpolateNonLinear | duration == 0 || current == 0 | FALSE
        [Test]
        public void TestInterpolateMidpointAndClamp()
        {
            var mid = LegacyUtils.InterpolateNonLinear(5, Color4.Black, Color4.White, 0, 10);
            Assert.That(mid.R, Is.EqualTo(0.5f).Within(1e-5));
            Assert.That(LegacyUtils.InterpolateNonLinear(100, Color4.Black, Color4.White, 0, 10, Easing.None), Is.EqualTo(Color4.White));
            Assert.That(LegacyUtils.InterpolateNonLinear(-100, Color4.Black, Color4.White, 0, 10).R, Is.EqualTo(0f).Within(1e-5));
        }

        [Test]
        public void TestInterpolateColour4Overloads()
        {
            Assert.That(LegacyUtils.InterpolateNonLinear(5, Colour4.Black, Colour4.Black, 0, 10), Is.EqualTo(Colour4.Black));
            Assert.That(LegacyUtils.InterpolateNonLinear(0, Colour4.Black, Colour4.White, 0, 10), Is.EqualTo(Colour4.Black));
            Assert.That(LegacyUtils.InterpolateNonLinear(5, Colour4.Black, Colour4.White, 10, 10), Is.EqualTo(Colour4.Black));
            Assert.That(LegacyUtils.InterpolateNonLinear(5, Colour4.Black, Colour4.White, 0, 10).R, Is.EqualTo(0.5f).Within(1e-5));
            Assert.That(LegacyUtils.InterpolateNonLinear(50, Colour4.Black, Colour4.White, 0, 10, new DefaultEasingFunction(Easing.None)), Is.EqualTo(Colour4.White));
        }

        #endregion

        #region StatelessRNG

        [Test]
        public void TestRngDeterministic()
        {
            Assert.That(StatelessRNG.NextULong(1, 2), Is.EqualTo(StatelessRNG.NextULong(1, 2)));
            Assert.That(StatelessRNG.NextULong(1, 2), Is.Not.EqualTo(StatelessRNG.NextULong(2, 1)));
            Assert.That(StatelessRNG.NextULong(0), Is.Not.EqualTo(0UL));
        }

        // CF: StatelessRNG.NextInt | maxValue <= 0 | throws ArgumentOutOfRangeException
        [Test]
        public void TestRngNextIntInvalidMax()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => StatelessRNG.NextInt(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => StatelessRNG.NextInt(-1, 1));
        }

        // CF: StatelessRNG.NextInt | maxValue > 0 | no throw
        [Test]
        public void TestRngNextIntInRange()
        {
            for (int seed = 0; seed < 100; seed++)
                Assert.That(StatelessRNG.NextInt(7, seed), Is.InRange(0, 6));
        }

        [Test]
        public void TestRngNextSingleRanges()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                Assert.That(StatelessRNG.NextSingle(seed), Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
                Assert.That(StatelessRNG.NextSingle(10f, 20f, seed), Is.InRange(10f, 20f));
            }
        }

        #endregion

        #region ColourUtils

        private static readonly (float, Color4)[] gradient = { (0f, Color4.Black), (1f, Color4.White), (2f, Color4.Red) };

        // CF: ColourUtils.SampleFromLinearGradient | point < gradient[0].position | TRUE
        [Test]
        public void TestGradientBeforeStart() => Assert.That(osu.Game.Utils.ColourUtils.SampleFromLinearGradient(gradient, -5), Is.EqualTo(Color4.Black));

        // CF: ColourUtils.SampleFromLinearGradient | loop over stops | loop n
        // CF: ColourUtils.SampleFromLinearGradient | point >= endStop.position | TRUE
        // CF: ColourUtils.SampleFromLinearGradient | point >= endStop.position | FALSE
        [Test]
        public void TestGradientInterpolatesBetweenStops()
        {
            Assert.That(osu.Game.Utils.ColourUtils.SampleFromLinearGradient(gradient, 0.5f).R, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(osu.Game.Utils.ColourUtils.SampleFromLinearGradient(gradient, 1.5f).G, Is.GreaterThan(0f).And.LessThan(1f));
        }

        // CF: ColourUtils.SampleFromLinearGradient | loop over stops | loop 0
        [Test]
        public void TestGradientAfterEnd() => Assert.That(osu.Game.Utils.ColourUtils.SampleFromLinearGradient(gradient, 9), Is.EqualTo(Color4.Red));

        #endregion

        #region TernaryStateUtils

        // CF: TernaryStateUtils.GetTernaryState | selection.Any(func) | FALSE
        [Test]
        public void TestTernaryNone() => Assert.That(new[] { 1, 3 }.GetTernaryState(x => x % 2 == 0), Is.EqualTo(TernaryState.False));

        // CF: TernaryStateUtils.GetTernaryState | selection.Any(func) | TRUE
        // CF: TernaryStateUtils.GetTernaryState | selection.All(func) | TRUE
        [Test]
        public void TestTernaryAll() => Assert.That(new[] { 2, 4 }.GetTernaryState(x => x % 2 == 0), Is.EqualTo(TernaryState.True));

        // CF: TernaryStateUtils.GetTernaryState | selection.All(func) | FALSE
        [Test]
        public void TestTernarySome() => Assert.That(new[] { 2, 3 }.GetTernaryState(x => x % 2 == 0), Is.EqualTo(TernaryState.Indeterminate));

        #endregion

        #region FilesystemSanityCheckHelpers

        // CF: FilesystemSanityCheckHelpers.IncursPathTraversalRisk | path.Contains("../") | TRUE
        // CF: FilesystemSanityCheckHelpers.IncursPathTraversalRisk | path.Contains("..\\") | TRUE
        // CF: FilesystemSanityCheckHelpers.IncursPathTraversalRisk | Path.IsPathRooted(path) | TRUE
        // CF: FilesystemSanityCheckHelpers.IncursPathTraversalRisk | all conditions | FALSE
        [TestCase("../x", true)]
        [TestCase("a/../x", true)]
        [TestCase("a\\..\\x", true)]
        [TestCase("/etc/passwd", true)]
        [TestCase("sub/file.txt", false)]
        public void TestPathTraversal(string path, bool risky) => Assert.That(FilesystemSanityCheckHelpers.IncursPathTraversalRisk(path), Is.EqualTo(risky));

        // CF: FilesystemSanityCheckHelpers.IsSubDirectory | parentInfo.FullName == childInfo.FullName | TRUE
        // CF: FilesystemSanityCheckHelpers.IsSubDirectory | childInfo != null | FALSE
        // CF: FilesystemSanityCheckHelpers.IsSubDirectory | parentInfo.FullName == childInfo.FullName | FALSE
        [Test]
        public void TestIsSubDirectory()
        {
            string root = Path.Combine(Path.GetTempPath(), "fs-sanity");
            Assert.That(FilesystemSanityCheckHelpers.IsSubDirectory(root, root), Is.True);
            Assert.That(FilesystemSanityCheckHelpers.IsSubDirectory(root, Path.Combine(root, "a", "b")), Is.True);
            Assert.That(FilesystemSanityCheckHelpers.IsSubDirectory(root, Path.Combine(root, "a", "..", "..", "other")), Is.False);
            Assert.That(FilesystemSanityCheckHelpers.IsSubDirectory(Path.Combine(root, "a"), root), Is.False);
        }

        #endregion
    }
}
