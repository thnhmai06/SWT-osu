// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using osu.Game.IO;
using osu.Game.Skinning;
using osuTK.Graphics;

namespace osu.Game.Tests.Skinning
{
    [TestFixture]
    public class LegacyManiaSkinDecoderTest
    {
        private const float scale = LegacySkin.STABLE_MAGIC_SCALE_FACTOR;

        private static List<LegacyManiaSkinConfiguration> decode(string text)
        {
            using var reader = new LineBufferedReader(new MemoryStream(Encoding.UTF8.GetBytes(text)));
            return new LegacyManiaSkinDecoder().Decode(reader);
        }

        private static LegacyManiaSkinConfiguration single(string body) => decode("[Mania]\nKeys: 4\n" + body)[0];

        // CF: LegacyManiaSkinDecoder.ParseLine | section == Section.Mania | FALSE
        [Test]
        public void TestOtherSectionsIgnored()
        {
            Assert.That(decode("[General]\nKeys: 4\n"), Is.Empty);
        }

        // CF: LegacyManiaSkinDecoder.ParseLine | pair.Key | case Keys
        // CF: LegacyManiaSkinDecoder.ParseLine | output.All(c => c.Keys != currentConfig.Keys) | TRUE
        [Test]
        public void TestKeysCreatesConfiguration()
        {
            var configs = decode("[Mania]\nKeys: 7\n");
            Assert.That(configs, Has.Count.EqualTo(1));
            Assert.That(configs[0].Keys, Is.EqualTo(7));
        }

        // CF: LegacyManiaSkinDecoder.ParseLine | output.All(c => c.Keys != currentConfig.Keys) | FALSE
        [Test]
        public void TestDuplicateKeysIgnored()
        {
            var configs = decode("[Mania]\nKeys: 4\nBarlineHeight: 2\n[Mania]\nKeys: 4\nBarlineHeight: 3\n");
            Assert.That(configs, Has.Count.EqualTo(1));
            Assert.That(configs[0].BarLineHeight, Is.EqualTo(2));
        }

        // CF: LegacyManiaSkinDecoder.ParseLine | pair.Key | default
        // CF: LegacyManiaSkinDecoder.ParseLine | currentConfig != null | FALSE
        // CF: LegacyManiaSkinDecoder.ParseLine | currentConfig != null | TRUE
        // CF: LegacyManiaSkinDecoder.flushPendingLines | foreach pendingLines | loop n
        [Test]
        public void TestLinesBeforeKeysAreHeldUntilKeysSeen()
        {
            var configs = decode("[Mania]\nBarlineHeight: 5\nKeys: 4\n");
            Assert.That(configs[0].BarLineHeight, Is.EqualTo(5));
        }

        // CF: LegacyManiaSkinDecoder.OnBeginNewSection | pending lines discarded | TRUE
        [Test]
        public void TestPendingLinesDiscardedOnNewSection()
        {
            var configs = decode("[Mania]\nBarlineHeight: 5\n[General]\n[Mania]\nKeys: 4\n");
            Assert.That(configs[0].BarLineHeight, Is.EqualTo(1.2f));
        }

        // CF: LegacyManiaSkinDecoder.flushPendingLines | foreach pendingLines | loop 0
        [Test]
        public void TestNoLinesNothingToFlush()
        {
            Assert.That(single(string.Empty).BarLineHeight, Is.EqualTo(1.2f));
        }

        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key | case ColumnLineWidth
        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key | case ColumnSpacing
        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key | case ColumnWidth
        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key | case LightingNWidth
        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key | case LightingLWidth
        // CF: LegacyManiaSkinDecoder.parseArrayValue | applyScaleFactor | TRUE
        // CF: LegacyManiaSkinDecoder.parseArrayValue | applyScaleFactor | FALSE
        // CF: LegacyManiaSkinDecoder.parseArrayValue | loop over values | loop n
        [Test]
        public void TestArrayValues()
        {
            var c = single("ColumnLineWidth: 1,2,3,4,5\nColumnSpacing: 1,2,3\nColumnWidth: 10,20,30,40\nLightingNWidth: 5,6,7,8\nLightingLWidth: 9,9,9,9\n");
            Assert.That(c.ColumnLineWidth, Is.EqualTo(new[] { 1f, 2, 3, 4, 5 }));
            Assert.That(c.ColumnSpacing, Is.EqualTo(new[] { 1 * scale, 2 * scale, 3 * scale }));
            Assert.That(c.ColumnWidth, Is.EqualTo(new[] { 10 * scale, 20 * scale, 30 * scale, 40 * scale }));
            Assert.That(c.ExplosionWidth, Is.EqualTo(new[] { 5 * scale, 6 * scale, 7 * scale, 8 * scale }));
            Assert.That(c.HoldNoteLightWidth, Is.EqualTo(new[] { 9 * scale, 9 * scale, 9 * scale, 9 * scale }));
        }

        // CF: LegacyManiaSkinDecoder.parseArrayValue | i >= output.Length | TRUE
        // CF: LegacyManiaSkinDecoder.parseArrayValue | i >= output.Length | FALSE
        // CF: LegacyManiaSkinDecoder.parseArrayValue | !float.TryParse(...) | TRUE
        // CF: LegacyManiaSkinDecoder.parseArrayValue | !float.TryParse(...) | FALSE
        [Test]
        public void TestArrayValueExtraAndInvalidEntries()
        {
            var c = single("ColumnWidth: 10,abc,30,40,50,60\n");
            Assert.That(c.ColumnWidth, Is.EqualTo(new[] { 10 * scale, 0, 30 * scale, 40 * scale }));
        }

        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key | case BarlineHeight
        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key | case HitPosition
        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key | case LightPosition
        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key | case ComboPosition
        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key | case ScorePosition
        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key | case WidthForNoteHeightScale
        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key | case ColumnStart
        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key | case ColumnRight
        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key | case StageSeparation
        [Test]
        public void TestScalarValues()
        {
            var c = single("BarlineHeight: 2.5\nHitPosition: 400\nLightPosition: 410\nComboPosition: 100\nScorePosition: 200\nWidthForNoteHeightScale: 10\nColumnStart: 50\nColumnRight: 60\nStageSeparation: 70\n");
            Assert.That(c.BarLineHeight, Is.EqualTo(2.5f));
            Assert.That(c.HitPosition, Is.EqualTo((480 - 400) * scale).Within(1e-3));
            Assert.That(c.LightPosition, Is.EqualTo((480 - 410) * scale).Within(1e-3));
            Assert.That(c.ComboPosition, Is.EqualTo(100 * scale).Within(1e-3));
            Assert.That(c.ScorePosition, Is.EqualTo(200 * scale).Within(1e-3));
            Assert.That(c.WidthForNoteHeightScale, Is.EqualTo(10 * scale).Within(1e-3));
            Assert.That(c.ColumnStart, Is.EqualTo(50));
            Assert.That(c.ColumnRight, Is.EqualTo(60));
            Assert.That(c.StageSeparation, Is.EqualTo(70));
        }

        [Test]
        public void TestHitPositionIsClamped()
        {
            Assert.That(single("HitPosition: 100\n").HitPosition, Is.EqualTo((480 - 240) * scale).Within(1e-3));
            Assert.That(single("HitPosition: 900\n").HitPosition, Is.EqualTo(0).Within(1e-3));
        }

        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key | case JudgementLine
        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key | case KeysUnderNotes
        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key | case UpsideDown
        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key | case SeparateScore
        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key | case SplitStages
        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Value == "1" | TRUE
        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Value == "1" | FALSE
        [Test]
        public void TestBooleanValues()
        {
            var on = single("KeysUnderNotes: 1\nUpsideDown: 1\nSplitStages: 1\nJudgementLine: 0\nSeparateScore: 0\n");
            Assert.That(on.KeysUnderNotes, Is.True);
            Assert.That(on.UpsideDown, Is.True);
            Assert.That(on.SplitStages, Is.True);
            Assert.That(on.ShowJudgementLine, Is.False);
            Assert.That(on.SeparateScore, Is.False);

            var off = single("JudgementLine: 1\nSeparateScore: 1\n");
            Assert.That(off.ShowJudgementLine, Is.True);
            Assert.That(off.SeparateScore, Is.True);
        }

        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key | case NoteBodyStyle
        // CF: LegacyManiaSkinDecoder.flushPendingLines | Enum.TryParse<LegacyNoteBodyStyle> | TRUE
        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key | case SpecialStyle
        // CF: LegacyManiaSkinDecoder.flushPendingLines | Enum.TryParse<LegacySpecialStyle> | TRUE
        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key | case ComboBurstStyle
        // CF: LegacyManiaSkinDecoder.flushPendingLines | Enum.TryParse<LegacyComboBurstStyle> | TRUE
        [Test]
        public void TestEnumValues()
        {
            var c = single("NoteBodyStyle: 3\nSpecialStyle: 2\nComboBurstStyle: 1\n");
            Assert.That(c.NoteBodyStyle, Is.EqualTo(LegacyManiaSkinConfiguration.LegacyNoteBodyStyle.RepeatBottom));
            Assert.That(c.SpecialStyle, Is.EqualTo(LegacyManiaSkinConfiguration.LegacySpecialStyle.Right));
            Assert.That(c.ComboBurstStyle, Is.EqualTo(LegacyManiaSkinConfiguration.LegacyComboBurstStyle.Right));
        }

        // CF: LegacyManiaSkinDecoder.flushPendingLines | Enum.TryParse<LegacyNoteBodyStyle> | FALSE
        // CF: LegacyManiaSkinDecoder.flushPendingLines | Enum.TryParse<LegacySpecialStyle> | FALSE
        // CF: LegacyManiaSkinDecoder.flushPendingLines | Enum.TryParse<LegacyComboBurstStyle> | FALSE
        [Test]
        public void TestInvalidEnumValuesIgnored()
        {
            var c = single("NoteBodyStyle: nope\nSpecialStyle: nope\nComboBurstStyle: nope\n");
            Assert.That(c.NoteBodyStyle, Is.Null);
            Assert.That(c.SpecialStyle, Is.Null);
            Assert.That(c.ComboBurstStyle, Is.Null);
        }

        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key | case LightFramePerSecond
        // CF: LegacyManiaSkinDecoder.flushPendingLines | lightFramePerSecond > 0 | TRUE
        // CF: LegacyManiaSkinDecoder.flushPendingLines | lightFramePerSecond > 0 | FALSE
        [Test]
        public void TestLightFramePerSecond()
        {
            Assert.That(single("LightFramePerSecond: 30\n").LightFramePerSecond, Is.EqualTo(30));
            Assert.That(single("LightFramePerSecond: 0\n").LightFramePerSecond, Is.EqualTo(24));
        }

        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key.StartsWith("Colour") | TRUE
        [Test]
        public void TestColours()
        {
            var c = single("Colour1: 255,0,0,128\n");
            Assert.That(c.CustomColours["Colour1"], Is.EqualTo(new Color4(255, 0, 0, 128)));
        }

        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key.StartsWith("NoteImage") | TRUE
        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key.StartsWith("KeyImage") | TRUE
        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key.StartsWith("Hit") | TRUE
        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key.StartsWith("Stage") | TRUE
        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key.StartsWith("Lighting") | TRUE
        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key | case WarningArrow
        [Test]
        public void TestImageLookups()
        {
            var c = single("NoteImage0: a\nKeyImage0: b\nHit300: c\nStageLeft: d\nLightingN: e\nWarningArrow: f\n");
            Assert.That(c.ImageLookups, Is.EquivalentTo(new Dictionary<string, string>
            {
                ["NoteImage0"] = "a",
                ["KeyImage0"] = "b",
                ["Hit300"] = "c",
                ["StageLeft"] = "d",
                ["LightingN"] = "e",
                ["WarningArrow"] = "f",
            }));
        }

        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key.StartsWith("KeyFlipWhenUpsideDown") | TRUE
        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key.StartsWith("NoteFlipWhenUpsideDown") | TRUE
        [Test]
        public void TestFlipSettings()
        {
            var c = single("KeyFlipWhenUpsideDown0: 1\nNoteFlipWhenUpsideDown0: 0\n");
            Assert.That(c.FlipSettings, Is.EquivalentTo(new Dictionary<string, string> { ["KeyFlipWhenUpsideDown0"] = "1", ["NoteFlipWhenUpsideDown0"] = "0" }));
        }

        // CF: LegacyManiaSkinDecoder.flushPendingLines | pair.Key | no match | no throw
        [Test]
        public void TestUnknownKeyIgnored()
        {
            Assert.That(single("Whatever: 1\n").Keys, Is.EqualTo(4));
        }
    }
}
