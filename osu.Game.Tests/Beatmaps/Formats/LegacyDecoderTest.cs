// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using osu.Game.Audio;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Rulesets.Objects.Legacy;
using osuTK.Graphics;

namespace osu.Game.Tests.Beatmaps.Formats
{
    [TestFixture]
    public class LegacyDecoderTest
    {
        private class Model : IHasComboColours, IHasCustomColours
        {
            public IReadOnlyList<Color4>? ComboColours => CustomComboColours;
            public List<Color4> CustomComboColours { get; } = new List<Color4>();
            public Dictionary<string, Color4> CustomColours { get; } = new Dictionary<string, Color4>();
            public readonly List<string> Lines = new List<string>();
            public readonly List<LegacyDecoder<Model>.Section> LineSections = new List<LegacyDecoder<Model>.Section>();
            public readonly List<bool> Primary = new List<bool>();
        }

        private class TestDecoder : LegacyDecoder<Model>
        {
            public readonly List<Section> BegunSections = new List<Section>();

            public TestDecoder(int version = LATEST_VERSION)
                : base(version)
            {
            }

            public int Version => FormatVersion;

            protected override void OnBeginNewSection(Section section) => BegunSections.Add(section);

            protected override void ParseLine(Model output, Section section, string line, bool isPrimaryStream)
            {
                output.Lines.Add(line);
                output.LineSections.Add(section);
                output.Primary.Add(isPrimaryStream);

                if (line.StartsWith("throw", StringComparison.Ordinal))
                    throw new InvalidOperationException("boom");

                base.ParseLine(output, section, line, isPrimaryStream);
            }

            public bool Skip(string line) => ShouldSkipLine(line);
            public string Strip(string line) => StripComments(line);
            public KeyValuePair<string, string> Split(string line, char separator = ':', bool trim = true) => SplitKeyVal(line, separator, trim);
            public string Clean(string path) => CleanFilename(path);
            public void Colours<T>(T output, string line, bool allowAlpha) => HandleColours(output, line, allowAlpha);
        }

        private static (Model model, TestDecoder decoder) decode(string content, params string[] others)
        {
            var decoder = new TestDecoder();
            var model = decoder.Decode(reader(content), others.Select(reader).ToArray());
            return (model, decoder);
        }

        private static LineBufferedReader reader(string content) => new LineBufferedReader(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)));

        [Test]
        public void TestVersionStored()
        {
            Assert.That(new TestDecoder(7).Version, Is.EqualTo(7));
            Assert.That(new TestDecoder().Version, Is.EqualTo(LegacyDecoder<Model>.LATEST_VERSION));
        }

        // CF: LegacyDecoder.ShouldSkipLine | string.IsNullOrWhiteSpace(line) | TRUE
        // CF: LegacyDecoder.ShouldSkipLine | line.TrimStart().StartsWith("//") | TRUE
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("// comment")]
        [TestCase("   // indented comment")]
        public void TestSkipped(string line)
        {
            Assert.That(new TestDecoder().Skip(line), Is.True);
        }

        // CF: LegacyDecoder.ShouldSkipLine | string.IsNullOrWhiteSpace(line) | FALSE
        // CF: LegacyDecoder.ShouldSkipLine | line.TrimStart().StartsWith("//") | FALSE
        [TestCase("a: b")]
        [TestCase("a // not at start")]
        public void TestNotSkipped(string line)
        {
            Assert.That(new TestDecoder().Skip(line), Is.False);
        }

        // CF: LegacyDecoder.StripComments | index > 0 | TRUE
        // CF: LegacyDecoder.StripComments | index > 0 | FALSE
        [TestCase("a//b", "a")]
        [TestCase("a: b // c", "a: b ")]
        [TestCase("//a", "//a")]
        [TestCase("abc", "abc")]
        public void TestStripComments(string input, string expected)
        {
            Assert.That(new TestDecoder().Strip(input), Is.EqualTo(expected));
        }

        // CF: LegacyDecoder.ParseStreamInto | while ((line = stream.ReadLine()) != null) | loop 0
        [Test]
        public void TestEmptyStream()
        {
            var (model, decoder) = decode("");

            Assert.That(model.Lines, Is.Empty);
            Assert.That(decoder.BegunSections, Is.Empty);
        }

        // CF: LegacyDecoder.ParseStreamInto | while ((line = stream.ReadLine()) != null) | loop n
        // CF: LegacyDecoder.ParseStreamInto | ShouldSkipLine(line) | TRUE
        // CF: LegacyDecoder.ParseStreamInto | ShouldSkipLine(line) | FALSE
        // CF: LegacyDecoder.ParseStreamInto | section != Section.Metadata | TRUE
        // CF: LegacyDecoder.ParseStreamInto | line.StartsWith('[') && line.EndsWith(']') | FALSE
        [Test]
        public void TestSkippedLinesAndCommentsStripped()
        {
            var (model, _) = decode("\n// c\nkey: value // trailing   \n");

            Assert.That(model.Lines, Is.EqualTo(new[] { "key: value" }), "comment removed and the remainder right-trimmed");
            Assert.That(model.LineSections.Single(), Is.EqualTo(LegacyDecoder<Model>.Section.General));
        }

        // CF: LegacyDecoder.ParseStreamInto | section != Section.Metadata | FALSE
        // CF: LegacyDecoder.ParseStreamInto | line.StartsWith('[') && line.EndsWith(']') | TRUE
        // CF: LegacyDecoder.ParseStreamInto | !Enum.TryParse(line[1..^1], out section) | FALSE
        [Test]
        public void TestMetadataSectionKeepsComments()
        {
            var (model, decoder) = decode("[Metadata]\nTitle:A // B ");

            Assert.That(decoder.BegunSections, Is.EqualTo(new[] { LegacyDecoder<Model>.Section.Metadata }));
            Assert.That(model.Lines, Is.EqualTo(new[] { "Title:A // B" }), "comments retained in metadata, whitespace still trimmed");
            Assert.That(model.LineSections.Single(), Is.EqualTo(LegacyDecoder<Model>.Section.Metadata));
        }

        // CF: LegacyDecoder.ParseStreamInto | line.StartsWith('[') | TRUE (line.EndsWith(']') FALSE)
        [TestCase("[Open")]
        [TestCase("Close]")]
        public void TestHalfBracketedLineIsNotASection(string line)
        {
            var (model, decoder) = decode(line);

            Assert.That(decoder.BegunSections, Is.Empty);
            Assert.That(model.Lines, Is.EqualTo(new[] { line }));
        }

        // CF: LegacyDecoder.ParseStreamInto | !Enum.TryParse(line[1..^1], out section) | TRUE
        [Test]
        public void TestUnknownSectionFallsBackToDefault()
        {
            var (_, decoder) = decode("[Colours]\n[Bogus]");

            Assert.That(decoder.BegunSections, Is.EqualTo(new[] { LegacyDecoder<Model>.Section.Colours, LegacyDecoder<Model>.Section.General }));
        }

        // CF: LegacyDecoder.ParseStreamInto | ParseLine try/catch | case catch Exception
        // CF: LegacyDecoder.ParseStreamInto | line.Length <= line_length_limit | TRUE
        // CF: LegacyDecoder.ParseStreamInto | line.Length <= line_length_limit | FALSE
        [Test]
        public void TestParseErrorsAreSwallowedAndParsingContinues()
        {
            string longLine = "throw" + new string('x', 100);

            var (model, _) = decode($"throw short\n{longLine}\nafter");

            Assert.That(model.Lines, Is.EqualTo(new[] { "throw short", longLine, "after" }));
        }

        [Test]
        public void TestPrimaryStreamFlagPerStream()
        {
            var (model, _) = decode("a", "b");

            Assert.That(model.Primary, Is.EqualTo(new[] { true, false }));
        }

        // CF: LegacyDecoder.ParseLine | case Section.Colours | case Colours
        [Test]
        public void TestColoursSectionDispatchedToColourHandling()
        {
            var (model, _) = decode("[Colours]\nCombo1 : 255,128,0\nSliderBorder: 1,2,3");

            Assert.That(model.CustomComboColours, Is.EqualTo(new[] { new Color4(255, 128, 0, 255) }));
            Assert.That(model.CustomColours["SliderBorder"], Is.EqualTo(new Color4(1, 2, 3, 255)));
        }

        // CF: LegacyDecoder.ParseLine | switch (section) | default
        [Test]
        public void TestOtherSectionsIgnoredByBaseParseLine()
        {
            var (model, _) = decode("[Difficulty]\nCombo1: 1,2,3");

            Assert.That(model.CustomComboColours, Is.Empty);
            Assert.That(model.CustomColours, Is.Empty);
        }

        // CF: LegacyDecoder.HandleColours | isCombo | TRUE
        // CF: LegacyDecoder.HandleColours | key startsWith Combo && int.TryParse && 1 <= index <= MAX_COMBO_COLOUR_COUNT | TRUE
        [TestCase("Combo1")]
        [TestCase("Combo8")]
        public void TestComboColourIndexInRange(string key)
        {
            var model = new Model();
            new TestDecoder().Colours(model, $"{key}: 10,20,30", false);

            Assert.That(model.CustomComboColours, Has.Count.EqualTo(1));
            Assert.That(model.CustomColours, Is.Empty);
        }

        // CF: LegacyDecoder.HandleColours | key startsWith Combo | FALSE
        // CF: LegacyDecoder.HandleColours | int.TryParse(pair.Key[5..], out comboIndex) | FALSE
        // CF: LegacyDecoder.HandleColours | comboIndex >= 1 | FALSE
        // CF: LegacyDecoder.HandleColours | comboIndex <= MAX_COMBO_COLOUR_COUNT | FALSE
        // CF: LegacyDecoder.HandleColours | isCombo | FALSE
        [TestCase("Combo0")]
        [TestCase("Combo9")]
        [TestCase("ComboX")]
        [TestCase("Other")]
        public void TestNonComboKeysAreCustomColours(string key)
        {
            var model = new Model();
            new TestDecoder().Colours(model, $"{key}: 10,20,30", false);

            Assert.That(model.CustomComboColours, Is.Empty);
            Assert.That(model.CustomColours[key], Is.EqualTo(new Color4(10, 20, 30, 255)));
        }

        // CF: LegacyDecoder.HandleColours | output is not IHasComboColours | TRUE
        // CF: LegacyDecoder.HandleColours | output is not IHasCustomColours | TRUE
        [Test]
        public void TestOutputWithoutColourInterfacesIsIgnored()
        {
            var decoder = new TestDecoder();

            Assert.That(() => decoder.Colours(new object(), "Combo1: 1,2,3", false), Throws.Nothing);
            Assert.That(() => decoder.Colours(new object(), "Other: 1,2,3", false), Throws.Nothing);
        }

        // CF: LegacyDecoder.convertSettingStringToColor4 | allowAlpha && split.Length == 4 | TRUE
        [Test]
        public void TestAlphaParsedWhenAllowed()
        {
            var model = new Model();
            new TestDecoder().Colours(model, "Other: 1,2,3,4", true);

            Assert.That(model.CustomColours["Other"], Is.EqualTo(new Color4(1, 2, 3, 4)));
        }

        // CF: LegacyDecoder.convertSettingStringToColor4 | allowAlpha && split.Length == 4 | FALSE
        [TestCase(false, "1,2,3,4")]
        [TestCase(true, "1,2,3")]
        public void TestAlphaDefaultsToOpaque(bool allowAlpha, string value)
        {
            var model = new Model();
            new TestDecoder().Colours(model, $"Other: {value}", allowAlpha);

            Assert.That(model.CustomColours["Other"].A, Is.EqualTo(1f));
        }

        // CF: LegacyDecoder.convertSettingStringToColor4 | split.Length != 3 && split.Length != 4 | TRUE
        [TestCase("1,2")]
        [TestCase("1,2,3,4,5")]
        [TestCase("")]
        public void TestWrongComponentCount(string value)
        {
            Assert.That(() => new TestDecoder().Colours(new Model(), $"Other: {value}", true), Throws.InstanceOf<InvalidOperationException>().With.Message.Contain("incorrect format"));
        }

        // CF: LegacyDecoder.convertSettingStringToColor4 | split.Length != 3 && split.Length != 4 | FALSE
        // CF: LegacyDecoder.convertSettingStringToColor4 | catch | case catch
        [TestCase("256,0,0")]
        [TestCase("a,b,c")]
        [TestCase("-1,0,0")]
        public void TestNon8BitComponents(string value)
        {
            Assert.That(() => new TestDecoder().Colours(new Model(), $"Other: {value}", false), Throws.InstanceOf<InvalidOperationException>().With.Message.Contain("8-bit"));
        }

        // CF: LegacyDecoder.SplitKeyVal | split.Length > 1 | TRUE
        // CF: LegacyDecoder.SplitKeyVal | shouldTrim | TRUE
        [Test]
        public void TestSplitKeyValTrimsAndKeepsLaterSeparators()
        {
            var pair = new TestDecoder().Split("  Key :  a:b  ");

            Assert.That(pair.Key, Is.EqualTo("Key"));
            Assert.That(pair.Value, Is.EqualTo("a:b"));
        }

        // CF: LegacyDecoder.SplitKeyVal | split.Length > 1 | FALSE
        [Test]
        public void TestSplitKeyValWithoutSeparator()
        {
            var pair = new TestDecoder().Split("Key");

            Assert.That(pair.Key, Is.EqualTo("Key"));
            Assert.That(pair.Value, Is.Empty);
        }

        // CF: LegacyDecoder.SplitKeyVal | shouldTrim | FALSE
        [Test]
        public void TestSplitKeyValCustomSeparatorNoTrim()
        {
            var pair = new TestDecoder().Split(" k , v ", ',', false);

            Assert.That(pair.Key, Is.EqualTo(" k "));
            Assert.That(pair.Value, Is.EqualTo(" v "));
        }

        [Test]
        public void TestCleanFilename()
        {
            var decoder = new TestDecoder();

            Assert.That(decoder.Clean("\"folder\\\\file.png\""), Is.EqualTo("folder/file.png"), "doubled backslash collapses, quotes trimmed, path standardised");
            Assert.That(decoder.Clean("plain.png"), Is.EqualTo("plain.png"));
        }

        private static LegacyDecoder<Model>.LegacySampleControlPoint sample(int custom, string bank = "normal", int volume = 100)
            => new LegacyDecoder<Model>.LegacySampleControlPoint { CustomSampleBank = custom, SampleBank = bank, SampleVolume = volume };

        // CF: LegacySampleControlPoint.ApplyTo | hitSampleInfo is ConvertHitObjectParser.LegacyHitSampleInfo legacy | TRUE
        // CF: LegacySampleControlPoint.ApplyTo | legacy.CustomSampleBank > 0 | FALSE
        // CF: LegacySampleControlPoint.ApplyTo | hitSampleInfo.Volume > 0 | FALSE
        // CF: LegacySampleControlPoint.ApplyTo | legacy.BankSpecified | FALSE
        [Test]
        public void TestApplyToFillsMissingValuesFromControlPoint()
        {
            var applied = (ConvertHitObjectParser.LegacyHitSampleInfo)sample(2, "drum", 70).ApplyTo(new ConvertHitObjectParser.LegacyHitSampleInfo("hitnormal"));

            Assert.That(applied.CustomSampleBank, Is.EqualTo(2));
            Assert.That(applied.Volume, Is.EqualTo(70));
            Assert.That(applied.Bank, Is.EqualTo("drum"));
        }

        // CF: LegacySampleControlPoint.ApplyTo | legacy.CustomSampleBank > 0 | TRUE
        // CF: LegacySampleControlPoint.ApplyTo | hitSampleInfo.Volume > 0 | TRUE
        // CF: LegacySampleControlPoint.ApplyTo | legacy.BankSpecified | TRUE
        [Test]
        public void TestApplyToKeepsExplicitValues()
        {
            var info = new ConvertHitObjectParser.LegacyHitSampleInfo("hitnormal", "soft", 40, customSampleBank: 3);

            var applied = (ConvertHitObjectParser.LegacyHitSampleInfo)sample(2, "drum", 70).ApplyTo(info);

            Assert.That(applied.CustomSampleBank, Is.EqualTo(3));
            Assert.That(applied.Volume, Is.EqualTo(40));
            Assert.That(applied.Bank, Is.EqualTo("soft"));
        }

        // CF: LegacySampleControlPoint.ApplyTo | hitSampleInfo is ConvertHitObjectParser.LegacyHitSampleInfo legacy | FALSE
        [Test]
        public void TestApplyToPlainSampleUsesBaseBehaviour()
        {
            var applied = sample(2, "drum", 70).ApplyTo(new HitSampleInfo("hitnormal", volume: 0));

            Assert.That(applied, Is.Not.InstanceOf<ConvertHitObjectParser.LegacyHitSampleInfo>());
            Assert.That(applied.Volume, Is.EqualTo(70));
        }

        // CF: LegacySampleControlPoint.IsRedundant | base.IsRedundant(existing) | FALSE
        // CF: LegacySampleControlPoint.IsRedundant | existing is LegacySampleControlPoint existingSample | FALSE
        // CF: LegacySampleControlPoint.IsRedundant | CustomSampleBank == existingSample.CustomSampleBank | FALSE
        // CF: LegacySampleControlPoint.IsRedundant | CustomSampleBank == existingSample.CustomSampleBank | TRUE
        [Test]
        public void TestIsRedundant()
        {
            Assert.That(sample(1).IsRedundant(sample(1)), Is.True);
            Assert.That(sample(1).IsRedundant(sample(2)), Is.False);
            Assert.That(sample(1).IsRedundant(sample(1, "drum")), Is.False, "base values differ");
            Assert.That(sample(1).IsRedundant(new osu.Game.Beatmaps.ControlPoints.SampleControlPoint()), Is.False, "not a legacy point");
            Assert.That(sample(1).IsRedundant(null), Is.False);
        }

        [Test]
        public void TestCopyFromAndEquality()
        {
            var a = sample(3, "soft", 55);
            var b = sample(0);

            b.CopyFrom(a);

            Assert.That(b.CustomSampleBank, Is.EqualTo(3));
            Assert.That(b.SampleBank, Is.EqualTo("soft"));
            Assert.That(b.SampleVolume, Is.EqualTo(55));
            Assert.That(b.Equals(a), Is.True);
            Assert.That(b.Equals((osu.Game.Beatmaps.ControlPoints.ControlPoint)a), Is.True);
            Assert.That(b.GetHashCode(), Is.EqualTo(a.GetHashCode()));
            Assert.That(sample(4, "soft", 55).Equals(a), Is.False);
            Assert.That(a.Equals(new osu.Game.Beatmaps.ControlPoints.SampleControlPoint()), Is.False);
        }
    }
}
