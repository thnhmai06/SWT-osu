// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Utils;

namespace osu.Game.Tests.Utils
{
    [TestFixture]
    public class NamingUtilsTest
    {
        // CF: NamingUtils.GetNextBestName | bestNumber == 0 | TRUE
        // CF: NamingUtils.findBestNumber | foreach existingNames | loop 0
        [Test]
        public void TestNameNoExisting()
        {
            Assert.That(NamingUtils.GetNextBestName(new string[0], "Name"), Is.EqualTo("Name"));
        }

        // CF: NamingUtils.findBestNumber | !match.Success | TRUE
        // CF: NamingUtils.findBestNumber | foreach existingNames | loop 1
        [Test]
        public void TestNameUnrelatedExisting()
        {
            Assert.That(NamingUtils.GetNextBestName(new[] { "Other" }, "Name"), Is.EqualTo("Name"));
        }

        // CF: NamingUtils.GetNextBestName | bestNumber == 0 | FALSE
        // CF: NamingUtils.findBestNumber | !match.Success | FALSE
        // CF: NamingUtils.findBestNumber | string.IsNullOrEmpty(copyNumberString) | TRUE
        // CF: NamingUtils.findBestNumber | foreach existingNames | loop n
        [Test]
        public void TestNameBaselineTaken()
        {
            Assert.That(NamingUtils.GetNextBestName(new[] { "Name", "other" }, "Name"), Is.EqualTo("Name (1)"));
            Assert.That(NamingUtils.GetNextBestName(new[] { "NAME" }, "name"), Is.EqualTo("name (1)"));
        }

        // CF: NamingUtils.findBestNumber | string.IsNullOrEmpty(copyNumberString) | FALSE
        // CF: NamingUtils.findBestNumber | while takenNumbers.Contains(bestNumber) | loop n
        [Test]
        public void TestNameFillsFirstGap()
        {
            Assert.That(NamingUtils.GetNextBestName(new[] { "Name", "Name (1)", "Name (3)" }, "Name"), Is.EqualTo("Name (2)"));
            // "(0)" and non-copy suffixes are not copy numbers.
            Assert.That(NamingUtils.GetNextBestName(new[] { "Name (0)", "Name (01)" }, "Name"), Is.EqualTo("Name"));
        }

        [Test]
        public void TestNameWithRegexCharacters()
        {
            Assert.That(NamingUtils.GetNextBestName(new[] { "a.b", "axb" }, "a.b"), Is.EqualTo("a.b (1)"));
        }

        // CF: NamingUtils.GetNextBestFilename | bestNumber == 0 | TRUE
        [Test]
        public void TestFilenameFree()
        {
            Assert.That(NamingUtils.GetNextBestFilename(new[] { "a.png" }, "b.png"), Is.EqualTo("b.png"));
        }

        // CF: NamingUtils.GetNextBestFilename | bestNumber == 0 | FALSE
        [Test]
        public void TestFilenameTaken()
        {
            Assert.That(NamingUtils.GetNextBestFilename(new[] { "a.png", "a (1).png", "A.PNG" }, "a.png"), Is.EqualTo("a (2).png"));
            Assert.That(NamingUtils.GetNextBestFilename(new[] { "a.jpg" }, "a.png"), Is.EqualTo("a.png"));
        }
    }
}
