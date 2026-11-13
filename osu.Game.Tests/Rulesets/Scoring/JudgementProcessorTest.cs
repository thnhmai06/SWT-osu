// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Online.Spectator;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Replays;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Tests.Rulesets.Scoring
{
    [TestFixture]
    public class JudgementProcessorTest
    {
        private class TestProcessor : JudgementProcessor
        {
            public readonly List<HitObject> Applied = new List<HitObject>();
            public readonly List<bool> SimulatingWhenApplied = new List<bool>();
            public int Reverted;
            public bool ReturnNullResult;

            public int ExposedMaxHits => MaxHits;
            public bool ExposedIsSimulating => IsSimulating;

            protected override void ApplyResultInternal(JudgementResult result)
            {
                Applied.Add(result.HitObject);
                SimulatingWhenApplied.Add(IsSimulating);
            }

            protected override void RevertResultInternal(JudgementResult result) => Reverted++;

            protected override JudgementResult CreateResult(HitObject hitObject, Judgement judgement) => ReturnNullResult ? null! : base.CreateResult(hitObject, judgement);
        }

        private static Beatmap beatmapWithSlider(out Slider slider)
        {
            slider = new Slider
            {
                StartTime = 0,
                Path = new SliderPath(PathType.LINEAR, new[] { osuTK.Vector2.Zero, new osuTK.Vector2(200, 0) }),
            };
            slider.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());

            var beatmap = new Beatmap();
            beatmap.HitObjects.Add(slider);
            beatmap.HitObjects.Add(new HitCircle { StartTime = 5000 });
            return beatmap;
        }

        // CF: JudgementProcessor.ApplyBeatmap | enumerates nested objects before their parents | loop n
        [Test]
        public void TestApplyBeatmapSimulatesNestedObjectsFirstAndStoresMaxHits()
        {
            var processor = new TestProcessor();
            var beatmap = beatmapWithSlider(out var slider);

            processor.ApplyBeatmap(beatmap);

            int expected = slider.NestedHitObjects.Count + 2;
            Assert.That(processor.ExposedMaxHits, Is.EqualTo(expected));
            Assert.That(processor.JudgedHits, Is.Zero, "judged hit counter is reset after the simulation");
            Assert.That(processor.ExposedIsSimulating, Is.False);
            Assert.That(processor.SimulatingWhenApplied, Has.All.True);

            var order = processor.Applied;
            Assert.That(order.IndexOf(slider.NestedHitObjects.First()), Is.LessThan(order.IndexOf(slider)));
            Assert.That(order.IndexOf(slider), Is.LessThan(order.IndexOf(beatmap.HitObjects[1])));
        }

        // CF: JudgementProcessor.SimulateAutoplay | result == null | TRUE
        [Test]
        public void TestMissingResultThrows()
        {
            var processor = new TestProcessor { ReturnNullResult = true };

            Assert.That(() => processor.ApplyBeatmap(beatmapWithSlider(out _)), Throws.InstanceOf<InvalidOperationException>());
        }

        // CF: JudgementProcessor.SimulateAutoplay | result == null | FALSE
        [Test]
        public void TestEmptyBeatmapHasNoMaxHits()
        {
            var processor = new TestProcessor();

            processor.ApplyBeatmap(new Beatmap());

            Assert.That(processor.ExposedMaxHits, Is.Zero);
        }

        // CF: JudgementProcessor.ApplyResult | result.Type == HitResult.LegacyComboIncrease | TRUE
        [Test]
        public void TestLegacyComboIncreaseCannotBeApplied()
        {
            var processor = new TestProcessor();
            var obj = new HitCircle();
#pragma warning disable CS0618
            var result = new JudgementResult(obj, obj.Judgement) { Type = HitResult.LegacyComboIncrease };
#pragma warning restore CS0618

            Assert.That(() => processor.ApplyResult(result), Throws.InstanceOf<ArgumentException>());
            Assert.That(processor.JudgedHits, Is.Zero);
        }

        // CF: JudgementProcessor.ApplyResult | result.Type == HitResult.LegacyComboIncrease | FALSE
        [Test]
        public void TestApplyAndRevertTrackCountAndRaiseEvents()
        {
            var processor = new TestProcessor();
            var obj = new HitCircle();
            var result = new JudgementResult(obj, obj.Judgement) { Type = HitResult.Great };
            var raised = new List<string>();
            processor.NewJudgement += r => raised.Add("new");
            processor.JudgementReverted += r => raised.Add("reverted");

            processor.ApplyResult(result);
            Assert.That(processor.JudgedHits, Is.EqualTo(1));

            processor.RevertResult(result);

            Assert.That(processor.JudgedHits, Is.Zero);
            Assert.That(processor.Reverted, Is.EqualTo(1));
            Assert.That(raised, Is.EqualTo(new[] { "new", "reverted" }));
        }

        // CF: JudgementProcessor.ResetFromReplayFrame | frame.Header == null | TRUE
        [Test]
        public void TestResetFromFrameWithoutHeaderKeepsCount()
        {
            var processor = new TestProcessor();
            var obj = new HitCircle();
            processor.ApplyResult(new JudgementResult(obj, obj.Judgement) { Type = HitResult.Great });

            processor.ResetFromReplayFrame(new ReplayFrame(0));

            Assert.That(processor.JudgedHits, Is.EqualTo(1));
        }

        // CF: JudgementProcessor.ResetFromReplayFrame | frame.Header == null | FALSE
        // CF: JudgementProcessor.ResetFromReplayFrame | foreach statistic | loop n
        [Test]
        public void TestResetFromFrameSumsStatistics()
        {
            var processor = new TestProcessor();
            var header = new FrameHeader(0, 1, 0, 0, new Dictionary<HitResult, int> { [HitResult.Great] = 5, [HitResult.Ok] = 2, [HitResult.Miss] = 1 }, new ScoreProcessorStatistics(), DateTimeOffset.UnixEpoch, [], null, null);

            processor.ResetFromReplayFrame(new ReplayFrame(0) { Header = header });

            Assert.That(processor.JudgedHits, Is.EqualTo(8));
        }

        // CF: JudgementProcessor.ResetFromReplayFrame | foreach statistic | loop 0
        [Test]
        public void TestResetFromFrameWithEmptyStatisticsZeroesCount()
        {
            var processor = new TestProcessor();
            var obj = new HitCircle();
            processor.ApplyResult(new JudgementResult(obj, obj.Judgement) { Type = HitResult.Great });
            var header = new FrameHeader(0, 1, 0, 0, new Dictionary<HitResult, int>(), new ScoreProcessorStatistics(), DateTimeOffset.UnixEpoch, [], null, null);

            processor.ResetFromReplayFrame(new ReplayFrame(0) { Header = header });

            Assert.That(processor.JudgedHits, Is.Zero);
        }

        // CF: JudgementProcessor.Update | JudgedHits == MaxHits && (JudgedHits == 0 || lastAppliedResult.TimeAbsolute < Clock.CurrentTime) | infeasible: Update() needs a loaded Component clock
        [Test]
        public void TestCompletionBindableInitiallyFalse()
        {
            Assert.That(new TestProcessor().HasCompleted.Value, Is.False);
        }
    }
}
