// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Mania.Beatmaps;
using osu.Game.Rulesets.Mania.Mods;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mania.Scoring;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Rulesets.Mania.Tests.Mods
{
    /// <summary>Covers <see cref="ManiaModScoreV2"/> and <see cref="ManiaModClassic"/>, which flip flags on the hit windows of every object.</summary>
    [TestFixture]
    public class ManiaModHitWindowStateTest
    {
        private static ManiaBeatmap create(bool isMania, out Note note, out HoldNote hold)
        {
            note = new Note { StartTime = 0 };
            hold = new HoldNote { StartTime = 1000, Duration = 500 };

            var beatmap = new ManiaBeatmap(new StageDefinition(4))
            {
                BeatmapInfo = { Ruleset = isMania ? new ManiaRuleset().RulesetInfo : new RulesetInfo { OnlineID = 0 } },
                HitObjects = { note, hold }
            };

            foreach (var h in beatmap.HitObjects)
                h.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());

            return beatmap;
        }

        private static ManiaHitWindows w(HitObject o) => (ManiaHitWindows)o.HitWindows;

        // CF: ManiaModScoreV2.ApplyToBeatmap | foreach hit object | loop 0
        [Test]
        public void TestScoreV2EmptyBeatmap()
        {
            Assert.That(() => new ManiaModScoreV2().ApplyToBeatmap(new ManiaBeatmap(new StageDefinition(4))), Throws.Nothing);
        }

        // CF: ManiaModScoreV2.ApplyToBeatmap | foreach hit object | loop n
        // CF: ManiaModScoreV2.ApplyToBeatmap | case Note | case Note
        // CF: ManiaModScoreV2.ApplyToBeatmap | case HoldNote | case HoldNote
        [Test]
        public void TestScoreV2()
        {
            var beatmap = create(true, out var note, out var hold);
            new ManiaModScoreV2().ApplyToBeatmap(beatmap);

            Assert.That(w(note).ScoreV2Active, Is.True);
            Assert.That(w(hold.Head).ScoreV2Active, Is.True);
            Assert.That(w(hold.Tail).ScoreV2Active, Is.True);
        }

        // CF: ManiaModScoreV2.ApplyToBeatmap | switch(ho) no case matches | default
        [Test]
        public void TestScoreV2IgnoresOtherObjects()
        {
            var beatmap = new ManiaBeatmap(new StageDefinition(4)) { HitObjects = { new HoldNoteBody() } };
            Assert.That(() => new ManiaModScoreV2().ApplyToBeatmap(beatmap), Throws.Nothing);
        }

        // CF: ManiaModClassic.ApplyToBeatmap | foreach hit object | loop 0
        [Test]
        public void TestClassicEmptyBeatmap()
        {
            Assert.That(() => new ManiaModClassic().ApplyToBeatmap(new ManiaBeatmap(new StageDefinition(4))), Throws.Nothing);
        }

        // CF: ManiaModClassic.ApplyToBeatmap | isConvert = !Ruleset.Equals(mania) | FALSE
        // CF: ManiaModClassic.ApplyToBeatmap | foreach hit object | loop n
        // CF: ManiaModClassic.ApplyToBeatmap | case Note | case Note
        // CF: ManiaModClassic.ApplyToBeatmap | case HoldNote | case HoldNote
        [Test]
        public void TestClassicOnNativeBeatmap()
        {
            var beatmap = create(true, out var note, out var hold);
            new ManiaModClassic().ApplyToBeatmap(beatmap);

            foreach (var windows in new[] { w(note), w(hold.Head), w(hold.Tail) })
            {
                Assert.That(windows.ClassicModActive, Is.True);
                Assert.That(windows.IsConvert, Is.False);
            }
        }

        // CF: ManiaModClassic.ApplyToBeatmap | isConvert = !Ruleset.Equals(mania) | TRUE
        [Test]
        public void TestClassicOnConvertedBeatmap()
        {
            var beatmap = create(false, out var note, out var hold);
            new ManiaModClassic().ApplyToBeatmap(beatmap);

            foreach (var windows in new[] { w(note), w(hold.Head), w(hold.Tail) })
            {
                Assert.That(windows.ClassicModActive, Is.True);
                Assert.That(windows.IsConvert, Is.True);
            }
        }

        // CF: ManiaModClassic.ApplyToBeatmap | switch(ho) no case matches | default
        [Test]
        public void TestClassicIgnoresOtherObjects()
        {
            var beatmap = new ManiaBeatmap(new StageDefinition(4)) { HitObjects = { new HoldNoteBody() } };
            Assert.That(() => new ManiaModClassic().ApplyToBeatmap(beatmap), Throws.Nothing);
        }
    }
}
