// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Mania.Tests.Objects
{
    [TestFixture]
    public class HoldNoteTest
    {
        private static HoldNote create(bool applyDefaults = true, double start = 1000, double duration = 500)
        {
            var hold = new HoldNote { StartTime = start, Duration = duration, Column = 2 };
            if (applyDefaults)
                hold.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());
            return hold;
        }

        // CF: HoldNote.EndTime | get => StartTime + Duration | non-null
        // CF: HoldNote.EndTime | set => Duration = value - StartTime | non-null
        // CF: HoldNote.Duration | Tail?.StartTime = EndTime | null
        // CF: HoldNote.StartTime | Head?.StartTime = value | null
        // CF: HoldNote.StartTime | Tail?.StartTime = EndTime | null
        // CF: HoldNote.Column | Head?.Column = value | null
        // CF: HoldNote.Column | Tail?.Column = value | null
        [Test]
        public void TestPropertiesBeforeNestedObjectsExist()
        {
            var hold = create(applyDefaults: false);
            Assert.That(hold.EndTime, Is.EqualTo(1500));

            hold.EndTime = 2000;
            Assert.That(hold.Duration, Is.EqualTo(1000));
            Assert.That(hold.Head, Is.Null);
            Assert.That(hold.Tail, Is.Null);
        }

        // CF: HoldNote.Duration | Tail?.StartTime = EndTime | non-null
        // CF: HoldNote.StartTime | Head?.StartTime = value | non-null
        // CF: HoldNote.StartTime | Tail?.StartTime = EndTime | non-null
        // CF: HoldNote.Column | Head?.Column = value | non-null
        // CF: HoldNote.Column | Tail?.Column = value | non-null
        [Test]
        public void TestChangesPropagateToNestedObjects()
        {
            var hold = create();
            Assert.That(hold.Head.StartTime, Is.EqualTo(1000));
            Assert.That(hold.Tail.StartTime, Is.EqualTo(1500));
            Assert.That(hold.Body.Duration, Is.EqualTo(500));

            hold.Duration = 700;
            Assert.That(hold.Tail.StartTime, Is.EqualTo(1700));

            hold.StartTime = 2000;
            Assert.That(hold.Head.StartTime, Is.EqualTo(2000));
            Assert.That(hold.Tail.StartTime, Is.EqualTo(2700));

            hold.Column = 5;
            Assert.That(hold.Head.Column, Is.EqualTo(5));
            Assert.That(hold.Tail.Column, Is.EqualTo(5));
        }

        // CF: HoldNote.CreateNestedHitObjects | NodeSamples ??= CreateDefaultNodeSamples (null) | TRUE
        // CF: HoldNote.GetNodeSamples | nodeIndex < NodeSamples?.Count | TRUE
        [Test]
        public void TestDefaultNodeSamplesCreated()
        {
            var hold = create();
            Assert.That(hold.NodeSamples, Has.Count.EqualTo(2));
            Assert.That(hold.Head.Samples, Is.EqualTo(hold.NodeSamples[0]));
            Assert.That(hold.Tail.Samples, Is.EqualTo(hold.NodeSamples[1]));
        }

        // CF: HoldNote.CreateNestedHitObjects | NodeSamples ??= CreateDefaultNodeSamples (null) | FALSE
        [Test]
        public void TestExplicitNodeSamplesAreKept()
        {
            var head = new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_WHISTLE) };
            var tail = new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_FINISH) };
            var hold = new HoldNote { StartTime = 0, Duration = 100, NodeSamples = new List<IList<HitSampleInfo>> { head, tail } };
            hold.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());

            Assert.That(hold.Head.Samples, Is.EqualTo(head));
            Assert.That(hold.Tail.Samples, Is.EqualTo(tail));
        }

        // CF: HoldNote.GetNodeSamples | nodeIndex < NodeSamples?.Count | FALSE
        [Test]
        public void TestGetNodeSamplesFallsBackToSamples()
        {
            var hold = create(applyDefaults: false);
            hold.Samples.Add(new HitSampleInfo(HitSampleInfo.HIT_CLAP));

            // NodeSamples is null (null side of ?.)
            Assert.That(hold.GetNodeSamples(0), Is.SameAs(hold.Samples));

            // index out of range
            hold.NodeSamples = HoldNote.CreateDefaultNodeSamples(hold);
            Assert.That(hold.GetNodeSamples(5), Is.SameAs(hold.Samples));
            Assert.That(hold.GetNodeSamples(1), Is.SameAs(hold.NodeSamples[1]));
        }

        // CF: HoldNote.CreateJudgement | IgnoreJudgement | non-null
        // CF: HoldNote.CreateHitWindows | HitWindows.Empty | non-null
        // CF: HoldNote.MaximumJudgementOffset | Tail.MaximumJudgementOffset | non-null
        [Test]
        public void TestJudgementAndHitWindows()
        {
            var hold = create();
            Assert.That(hold.CreateJudgement(), Is.TypeOf<IgnoreJudgement>());
            Assert.That(hold.HitWindows, Is.SameAs(HitWindows.Empty));
            Assert.That(hold.MaximumJudgementOffset, Is.EqualTo(hold.Tail.MaximumJudgementOffset));
        }
    }
}
