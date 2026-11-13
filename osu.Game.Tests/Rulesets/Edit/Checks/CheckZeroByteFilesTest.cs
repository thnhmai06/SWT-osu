// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Edit.Checks;

namespace osu.Game.Tests.Rulesets.Edit.Checks
{
    [TestFixture]
    public class CheckZeroByteFilesTest
    {
        private readonly CheckZeroByteFiles check = new CheckZeroByteFiles();

        // CF: CheckZeroByteFiles.Run | beatmapSet != null | FALSE
        [Test]
        public void TestNoBeatmapSet()
        {
            Assert.That(check.Run(CheckTestUtils.Context(new Beatmap())), Is.Empty);
        }

        // CF: CheckZeroByteFiles.Run | beatmapSet != null | TRUE
        // CF: CheckZeroByteFiles.Run | foreach file in beatmapSet.Files | loop 0
        [Test]
        public void TestEmptySet()
        {
            Assert.That(check.Run(CheckFakes.Context(new Beatmap(), new Dictionary<string, byte[]?>())), Is.Empty);
        }

        // CF: CheckZeroByteFiles.Run | foreach file in beatmapSet.Files | loop n
        // CF: CheckZeroByteFiles.Run | data?.Length == 0 | TRUE
        // CF: CheckZeroByteFiles.Run | data?.Length == 0 | FALSE
        [Test]
        public void TestOnlyZeroByteFilesReported()
        {
            var context = CheckFakes.Context(new Beatmap(), new Dictionary<string, byte[]?>
            {
                ["empty.png"] = [],
                ["full.png"] = [1, 2, 3],
                ["unreadable.png"] = null, // stream resolves to null
            });

            var issue = check.Run(context).Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckZeroByteFiles.IssueTemplateZeroBytes>());
            Assert.That(issue.Arguments, Is.EqualTo(new object[] { "empty.png" }));
            Assert.That(check.PossibleTemplates.Count(), Is.EqualTo(1));
        }
    }
}
