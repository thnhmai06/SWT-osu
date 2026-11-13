// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Mania.Beatmaps;
using osu.Game.Rulesets.Mania.Objects;

namespace osu.Game.Rulesets.Mania.Tests.Beatmaps
{
    [TestFixture]
    public class ManiaBeatmapTest
    {
        // CF: ManiaBeatmap.GetStatistics | Math.Max(1, notes + holdNotes) | non-null
        [Test]
        public void TestStatisticsEmptyBeatmapDoesNotDivideByZero()
        {
            var stats = new ManiaBeatmap(new StageDefinition(4)).GetStatistics().ToArray();
            Assert.That(stats.Select(s => s.Content), Is.EqualTo(new[] { "0", "0" }));
            Assert.That(stats.Select(s => s.BarDisplayLength), Is.All.EqualTo(0f));
        }

        // CF: ManiaBeatmap.GetStatistics | Math.Max(1, notes + holdNotes) | non-null
        [Test]
        public void TestStatisticsCounts()
        {
            var b = new ManiaBeatmap(new StageDefinition(4)) { HitObjects = { new Note(), new Note(), new Note(), new HoldNote() } };
            var stats = b.GetStatistics().ToArray();

            Assert.That(stats.Select(s => s.Content), Is.EqualTo(new[] { "3", "1" }));
            Assert.That(stats[0].BarDisplayLength, Is.EqualTo(0.75f));
            Assert.That(stats[1].BarDisplayLength, Is.EqualTo(0.25f));
        }

        // CF: ManiaBeatmap.GetStageForColumnIndex | foreach stage | loop n
        // CF: ManiaBeatmap.GetStageForColumnIndex | column < stage.Columns | TRUE
        // CF: ManiaBeatmap.GetStageForColumnIndex | column < stage.Columns | FALSE
        [Test]
        public void TestGetStageForColumnIndex()
        {
            var first = new StageDefinition(4);
            var second = new StageDefinition(2);
            var b = new ManiaBeatmap(first);
            b.Stages.Add(second);

            Assert.That(b.TotalColumns, Is.EqualTo(6));
            Assert.That(b.GetStageForColumnIndex(0), Is.SameAs(first));
            Assert.That(b.GetStageForColumnIndex(3), Is.SameAs(first));
            Assert.That(b.GetStageForColumnIndex(4), Is.SameAs(second));
            Assert.That(b.GetStageForColumnIndex(5), Is.SameAs(second));
        }

        // CF: ManiaBeatmap.GetStageForColumnIndex | foreach stage | loop 0..n exhausted
        // CF: ManiaBeatmap.GetStageForColumnIndex | column exceeds all stages | throws ArgumentOutOfRangeException
        [Test]
        public void TestGetStageForColumnIndexOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ManiaBeatmap(new StageDefinition(4)).GetStageForColumnIndex(4));
        }
    }
}
