// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using System.Reflection;
using NUnit.Framework;
using osu.Framework.Localisation;
using osu.Game.Utils;

namespace osu.Game.Tests.Utils
{
    [TestFixture]
    public class FormatUtilsTest
    {
        private CultureInfo originalCulture = null!;

        [SetUp]
        public void SetUp()
        {
            originalCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        [TearDown]
        public void TearDown() => CultureInfo.CurrentCulture = originalCulture;

        // CF: FormatUtils.FloorToDecimalDigits | floor (not round) at given digits
        [TestCase(89.9999, 2u, 89.99)]
        [TestCase(1.239, 0u, 1.0)]
        [TestCase(5.5, 1u, 5.5)]
        public void TestFloorToDecimalDigits(double value, uint digits, double expected)
        {
            Assert.That(value.FloorToDecimalDigits(digits), Is.EqualTo(expected).Within(1e-9));
        }

        // CF: FormatUtils.FormatAccuracy | floors to 4 digits so 89.99999% never displays as 90%
        [Test]
        public void TestFormatAccuracyNeverRoundsUp()
        {
            Assert.That(0.8999999.FormatAccuracy().ToString(), Is.EqualTo("89.99%"));
        }

        // CF: FormatUtils.FormatStarRating | floors to 2 digits so 6.9999* never displays as 7.00*
        [Test]
        public void TestFormatStarRatingNeverRoundsUp()
        {
            Assert.That(6.9999.FormatStarRating().ToString(), Is.EqualTo("6.99"));
        }

        // CF: FormatUtils.FormatRank | rank < 10_000 TRUE -> one decimal
        [Test]
        public void TestFormatRankBelowTenThousandUsesOneDecimal()
        {
            Assert.That(1234.FormatRank(), Is.EqualTo("1.2k"));
        }

        // CF: FormatUtils.FormatRank | rank < 10_000 FALSE -> no decimals
        [Test]
        public void TestFormatRankAtOrAboveTenThousandUsesNoDecimals()
        {
            Assert.That(12345.FormatRank(), Is.EqualTo("12k"));
        }

        // CF: FormatUtils.FindPrecision | while (d != Round(d)) | zero iterations (integer)
        [Test]
        public void TestFindPrecisionOfIntegerIsZero()
        {
            Assert.That(FormatUtils.FindPrecision(5m), Is.EqualTo(0));
        }

        // CF: FormatUtils.FindPrecision | while (d != Round(d)) | repeats until whole
        [TestCase(0.5, 1)]
        [TestCase(0.25, 2)]
        [TestCase(1.125, 3)]
        public void TestFindPrecisionCountsDecimalPlaces(double value, int expected)
        {
            Assert.That(FormatUtils.FindPrecision((decimal)value), Is.EqualTo(expected));
        }

        // CF: FormatUtils.RoundBPM | default rate = 1
        [Test]
        public void TestRoundBpmDefaultRate()
        {
            Assert.That(FormatUtils.RoundBPM(120.4), Is.EqualTo(120));
            Assert.That(FormatUtils.RoundBPM(120.6), Is.EqualTo(121));
        }

        // CF: FormatUtils.RoundBPM | explicit rate multiplies before rounding
        [Test]
        public void TestRoundBpmWithRate()
        {
            Assert.That(FormatUtils.RoundBPM(100, 1.5), Is.EqualTo(150));
        }

        private static readonly DateTimeOffset date = new DateTimeOffset(2024, 3, 5, 0, 0, 0, TimeSpan.Zero);

        // LocalisableString.Data is internal to the framework; read it reflectively to reach the date formatter.
        private static ILocalisableStringData dataOf(LocalisableString value)
            => (ILocalisableStringData)typeof(LocalisableString).GetField("Data", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(value)!;

        private static string localise(CultureInfo? culture)
        {
            var parameters = new LocalisationParameters(culture == null ? null : new FakeStore(culture), false);
            return (dataOf(date.ToLocalisedMediumDate())).GetLocalised(parameters);
        }

        // CF: MediumFormattedDate.GetLocalised | cultureCode null/empty (no store) | default format
        [Test]
        public void TestMediumDateWithoutStoreUsesDefaultFormat() => Assert.That(localise(null), Is.EqualTo("5 Mar 2024"));

        // CF: MediumFormattedDate.GetLocalised | string.IsNullOrEmpty(cultureCode) TRUE (invariant culture name "")
        [Test]
        public void TestMediumDateWithInvariantCultureUsesDefaultFormat() => Assert.That(localise(CultureInfo.InvariantCulture), Is.EqualTo("5 Mar 2024"));

        // CF: MediumFormattedDate.GetLocalised | TryParseCultureCode FALSE (unmapped region code) | default format
        [Test]
        public void TestMediumDateWithUnparsableCultureUsesDefaultFormat() => Assert.That(localise(new CultureInfo("fr-CA")), Is.EqualTo("5 Mar 2024"));

        // CF: MediumFormattedDate.GetLocalised | all three conditions TRUE | mapped format
        [TestCase("de", "5. Mar 2024")]
        [TestCase("ja", "2024年3月5日")]
        [TestCase("es", "5 de Mar de 2024")]
        [TestCase("zh-TW", "2024年3月5日")]
        public void TestMediumDateWithMappedLanguageUsesLanguageFormat(string culture, string expected)
            => Assert.That(localise(new CultureInfo(culture)), Is.EqualTo(expected));

        // CF: MediumFormattedDate.Equals | other is MediumFormattedDate TRUE/FALSE, Date.Equals TRUE/FALSE
        [Test]
        public void TestMediumDateEquality()
        {
            var a = dataOf(date.ToLocalisedMediumDate());
            var same = dataOf(date.ToLocalisedMediumDate());
            var different = dataOf(date.AddDays(1).ToLocalisedMediumDate());

            Assert.That(a.Equals(same), Is.True);
            Assert.That(a.Equals(different), Is.False);
            Assert.That(a.Equals(null), Is.False);
        }

        private class FakeStore : ILocalisationStore
        {
            public FakeStore(CultureInfo culture) => EffectiveCulture = culture;

            public CultureInfo EffectiveCulture { get; }

            public string Get(string lookup) => lookup;

            public System.Threading.Tasks.Task<string> GetAsync(string lookup, System.Threading.CancellationToken cancellationToken = default)
                => System.Threading.Tasks.Task.FromResult(lookup);

            public System.IO.Stream GetStream(string name) => throw new NotSupportedException();

            public System.Collections.Generic.IEnumerable<string> GetAvailableResources() => System.Array.Empty<string>();

            public void Dispose()
            {
            }
        }
    }
}
