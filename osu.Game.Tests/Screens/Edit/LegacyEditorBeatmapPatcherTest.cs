// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Formats;
using osu.Game.Beatmaps.Timing;
using osu.Game.IO;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osuTK;
using Decoder = osu.Game.Beatmaps.Formats.Decoder;

namespace osu.Game.Tests.Screens.Edit
{
    [TestFixture]
    public class LegacyEditorBeatmapPatcherTest
    {
        private LegacyEditorBeatmapPatcher patcher = null!;
        private EditorBeatmap current = null!;

        [SetUp]
        public void SetUp()
        {
            patcher = new LegacyEditorBeatmapPatcher(current = new EditorBeatmap(new OsuBeatmap
            {
                BeatmapInfo = { Ruleset = new OsuRuleset().RulesetInfo }
            }));
        }

        private static OsuHitObject circle(double time, bool newCombo = false) => new HitCircle { StartTime = time, NewCombo = newCombo };

        // CF: LegacyEditorBeatmapPatcher.Patch | no differences | no throw
        [Test]
        public void TestNoChanges() => runTest(new OsuBeatmap());

        // CF: LegacyEditorBeatmapPatcher.processHitObjects | addedIndices.Count > 0 | TRUE
        // CF: LegacyEditorBeatmapPatcher.processHitObjects | loop over added indices | loop 1
        // CF: LegacyEditorBeatmapPatcher.processHitObjects | loop over removed indices | loop 0
        [Test]
        public void TestAddHitObject() => runTest(new OsuBeatmap { HitObjects = { circle(1000, true) } });

        // CF: LegacyEditorBeatmapPatcher.processHitObjects | addedIndices.Count > 0 | FALSE
        // CF: LegacyEditorBeatmapPatcher.processHitObjects | loop over removed indices | loop 1
        [Test]
        public void TestDeleteHitObject()
        {
            current.AddRange(new[] { circle(1000, true), circle(2000), circle(3000) });
            runTest(new OsuBeatmap { HitObjects = { (OsuHitObject)current.HitObjects[0], (OsuHitObject)current.HitObjects[2] } });
        }

        // CF: LegacyEditorBeatmapPatcher.processHitObjects | loop over added indices | loop n
        // CF: LegacyEditorBeatmapPatcher.processHitObjects | loop over removed indices | loop n
        [Test]
        public void TestAddAndDeleteMultipleHitObjects()
        {
            current.AddRange(new[] { circle(500, true), circle(1000), circle(1500), circle(2000), circle(2250), circle(2500) });

            runTest(new OsuBeatmap
            {
                HitObjects =
                {
                    circle(750, true),
                    (OsuHitObject)current.HitObjects[1],
                    (OsuHitObject)current.HitObjects[4],
                    circle(2650),
                    circle(4000),
                }
            });
        }

        // CF: LegacyEditorBeatmapPatcher.processHitObjectLocalData | ReferenceEquals(oldObject, newObject) | FALSE
        // CF: LegacyEditorBeatmapPatcher.processHitObjectLocalData | oldObject is IHasRepeats && newObject is IHasRepeats | FALSE
        [Test]
        public void TestChangeSamples()
        {
            current.AddRange(new[] { circle(1000, true), circle(2000), circle(3000) });

            runTest(new OsuBeatmap
            {
                HitObjects =
                {
                    (OsuHitObject)current.HitObjects[0],
                    new HitCircle { StartTime = 2000, Samples = { new HitSampleInfo(HitSampleInfo.HIT_FINISH) } },
                    (OsuHitObject)current.HitObjects[2],
                }
            });
        }

        // CF: LegacyEditorBeatmapPatcher.processHitObjectLocalData | oldObject is IHasSliderVelocity && newObject is IHasSliderVelocity | TRUE
        // CF: LegacyEditorBeatmapPatcher.processHitObjectLocalData | oldObject is IHasRepeats && newObject is IHasRepeats | TRUE
        [Test]
        public void TestChangeSliderVelocityAndPath()
        {
            current.AddRange(new OsuHitObject[]
            {
                circle(1000, true),
                new Slider { StartTime = 2000, Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(50, 0) }) },
            });

            runTest(new OsuBeatmap
            {
                HitObjects =
                {
                    (OsuHitObject)current.HitObjects[0],
                    new Slider
                    {
                        StartTime = 2000,
                        SliderVelocityMultiplier = 2,
                        Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(100, 0) }),
                    },
                }
            });
        }

        // CF: LegacyEditorBeatmapPatcher.processHitObjectLocalData | ReferenceEquals(oldObject, newObject) | TRUE
        [Test]
        public void TestInsertedObjectsAreNotModifiedAgain()
        {
            current.AddRange(new[] { circle(1000, true) });
            runTest(new OsuBeatmap { HitObjects = { (OsuHitObject)current.HitObjects[0], circle(2000) } });
        }

        // CF: LegacyEditorBeatmapPatcher.processTimingPoints | !oldGroup.Equals(newGroup) | TRUE
        // CF: LegacyEditorBeatmapPatcher.processTimingPoints | !newGroup.Equals(oldGroup) | TRUE
        // CF: LegacyEditorBeatmapPatcher.processTimingPoints | loop over groups | loop n
        [Test]
        public void TestTimingPointsAddedChangedAndRemoved()
        {
            current.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });
            current.ControlPointInfo.Add(10000, new TimingControlPoint { BeatLength = 400 });

            var patch = new OsuBeatmap();
            patch.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 600 });
            patch.ControlPointInfo.Add(5000, new TimingControlPoint { BeatLength = 300 });

            runTest(patch);

            Assert.That(current.ControlPointInfo.TimingPoints.Select(t => (t.Time, t.BeatLength)), Is.EqualTo(new[] { (0d, 600d), (5000d, 300d) }));
        }

        // CF: LegacyEditorBeatmapPatcher.processTimingPoints | !oldGroup.Equals(newGroup) | FALSE
        // CF: LegacyEditorBeatmapPatcher.processTimingPoints | !newGroup.Equals(oldGroup) | FALSE
        [Test]
        public void TestIdenticalTimingPointsLeftAlone()
        {
            current.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });

            var patch = new OsuBeatmap();
            patch.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });

            runTest(patch);
            Assert.That(current.ControlPointInfo.TimingPoints, Has.Count.EqualTo(1));
        }

        // CF: LegacyEditorBeatmapPatcher.processBreaks | newBreaks.Any(b => b.Equals(oldBreak)) | TRUE
        // CF: LegacyEditorBeatmapPatcher.processBreaks | newBreaks.Any(b => b.Equals(oldBreak)) | FALSE
        // CF: LegacyEditorBeatmapPatcher.processBreaks | editorBeatmap.Breaks.Any(b => b.Equals(newBreak)) | TRUE
        // CF: LegacyEditorBeatmapPatcher.processBreaks | editorBeatmap.Breaks.Any(b => b.Equals(newBreak)) | FALSE
        [Test]
        public void TestBreaksSynchronised()
        {
            current.Breaks.Add(new BreakPeriod(1000, 3000));
            current.Breaks.Add(new BreakPeriod(5000, 7000));

            runTest(new OsuBeatmap { Breaks = { new BreakPeriod(1000, 3000), new BreakPeriod(9000, 12000) } });

            Assert.That(current.Breaks.Select(b => (b.StartTime, b.EndTime)), Is.EquivalentTo(new[] { (1000d, 3000d), (9000d, 12000d) }));
        }

        // CF: LegacyEditorBeatmapPatcher.processBookmarks | newBookmarks.Contains(oldBookmark) | TRUE
        // CF: LegacyEditorBeatmapPatcher.processBookmarks | newBookmarks.Contains(oldBookmark) | FALSE
        // CF: LegacyEditorBeatmapPatcher.processBookmarks | editorBeatmap.Bookmarks.Contains(newBookmark) | TRUE
        // CF: LegacyEditorBeatmapPatcher.processBookmarks | editorBeatmap.Bookmarks.Contains(newBookmark) | FALSE
        // CF: LegacyEditorBeatmapPatcher.processBookmarks | idx < 0 | TRUE
        [Test]
        public void TestBookmarksSynchronised()
        {
            current.Bookmarks.Add(1000);
            current.Bookmarks.Add(2000);

            runTest(new OsuBeatmap { Bookmarks = new[] { 1500, 2000 } });

            Assert.That(current.Bookmarks, Is.EqualTo(new[] { 1500, 2000 }));
        }

        private void runTest(IBeatmap patch)
        {
            // the patch is round-tripped through the encoder/decoder so that decoder-applied defaults (e.g. new combo, default samples) are present.
            patch = decode(encode(patch));

            patcher.Patch(encode(current), encode(patch));

            Assert.That(Encoding.ASCII.GetString(encode(current)), Is.EqualTo(Encoding.ASCII.GetString(encode(patch))));
        }

        private static byte[] encode(IBeatmap beatmap)
        {
            using var encoded = new MemoryStream();

            using (var sw = new StreamWriter(encoded))
                new LegacyBeatmapEncoder(beatmap, null, null).Encode(sw);

            return encoded.ToArray();
        }

        private static IBeatmap decode(byte[] state)
        {
            using var stream = new MemoryStream(state);
            using var reader = new LineBufferedReader(stream);
            return Decoder.GetDecoder<Beatmap>(reader).Decode(reader);
        }
    }
}
