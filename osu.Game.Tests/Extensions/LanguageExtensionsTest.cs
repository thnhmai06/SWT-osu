// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;
using NUnit.Framework;
using osu.Framework.Localisation;
using osu.Game.Extensions;
using osu.Game.Localisation;

namespace osu.Game.Tests.Extensions
{
    [TestFixture]
    public class LanguageExtensionsTest
    {
        // CF: LanguageExtensions.ToCultureCode | language == Language.zh_hant | TRUE
        [Test]
        public void TestTraditionalChineseCultureCode()
        {
            Assert.That(Language.zh_hant.ToCultureCode(), Is.EqualTo("zh-tw"));
        }

        // CF: LanguageExtensions.ToCultureCode | language == Language.zh_hant | FALSE
        [TestCase(Language.en, "en")]
        [TestCase(Language.pt_br, "pt-br")]
        public void TestCultureCodeUnderscoreReplaced(Language language, string expected)
        {
            Assert.That(language.ToCultureCode(), Is.EqualTo(expected));
        }

        // CF: LanguageExtensions.TryParseCultureCode | cultureCode == "zh-tw" | TRUE
        [Test]
        public void TestParseTraditionalChinese()
        {
            Assert.That(LanguageExtensions.TryParseCultureCode("zh-tw", out var language), Is.True);
            Assert.That(language, Is.EqualTo(Language.zh_hant));
        }

        // CF: LanguageExtensions.TryParseCultureCode | cultureCode == "zh-tw" | FALSE
        [Test]
        public void TestParseOthers()
        {
            Assert.That(LanguageExtensions.TryParseCultureCode("pt-br", out var language), Is.True);
            Assert.That(language, Is.EqualTo(Language.pt_br));
            Assert.That(LanguageExtensions.TryParseCultureCode("xx-nonexistent", out _), Is.False);
        }

        // CF: LanguageExtensions.GetLanguageFor | TryParseCultureCode(frameworkLocale) | TRUE
        [Test]
        public void TestGetLanguageFromFrameworkLocale()
        {
            Assert.That(LanguageExtensions.GetLanguageFor("pt-br", new LocalisationParameters(null, false)), Is.EqualTo(Language.pt_br));
        }

        // CF: LanguageExtensions.GetLanguageFor | TryParseCultureCode(frameworkLocale) | FALSE
        // CF: LanguageExtensions.GetLanguageFor | localisationParameters.Store != null | FALSE
        [Test]
        public void TestGetLanguageNoStoreFallsBackToEnglish()
        {
            Assert.That(LanguageExtensions.GetLanguageFor("", new LocalisationParameters(null, false)), Is.EqualTo(Language.en));
        }

        // CF: LanguageExtensions.GetLanguageFor | localisationParameters.Store != null | TRUE
        // CF: LanguageExtensions.GetLanguageFor | TryParseCultureCode(Store.EffectiveCulture.Name) | TRUE
        [Test]
        public void TestGetLanguageFromStoreCulture()
        {
            var store = new FakeStore(new CultureInfo("de"));
            Assert.That(LanguageExtensions.GetLanguageFor("", new LocalisationParameters(store, false)), Is.EqualTo(Language.de));
        }

        // CF: LanguageExtensions.GetLanguageFor | TryParseCultureCode(Store.EffectiveCulture.Name) | FALSE
        [Test]
        public void TestGetLanguageUnknownStoreCultureFallsBackToEnglish()
        {
            var store = new FakeStore(CultureInfo.InvariantCulture);
            Assert.That(LanguageExtensions.GetLanguageFor("", new LocalisationParameters(store, false)), Is.EqualTo(Language.en));
        }

        private class FakeStore : ILocalisationStore
        {
            public FakeStore(CultureInfo culture)
            {
                EffectiveCulture = culture;
            }

            public CultureInfo EffectiveCulture { get; }

            public string? Get(string lookup) => null;

            public System.Threading.Tasks.Task<string?> GetAsync(string lookup, System.Threading.CancellationToken cancellationToken = default) => System.Threading.Tasks.Task.FromResult<string?>(null);

            public System.IO.Stream? GetStream(string name) => null;

            public System.Collections.Generic.IEnumerable<string> GetAvailableResources() => System.Array.Empty<string>();

            public void Dispose()
            {
            }
        }
    }
}
