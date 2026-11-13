// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Game.Localisation;

namespace osu.Game.Tests.Localisation
{
    [TestFixture]
    public class ResourceManagerLocalisationStoreTest
    {
        [Test]
        public void TestEffectiveCulture()
        {
            Assert.That(new ResourceManagerLocalisationStore("de").EffectiveCulture.Name, Is.EqualTo("de"));
        }

        // CF: ResourceManagerLocalisationStore.getInternal | split.Length < 2 | TRUE
        [Test]
        public void TestLookupWithoutNamespaceSeparator()
        {
            Assert.That(new ResourceManagerLocalisationStore("de").Get("nothing"), Is.Null);
        }

        // CF: ResourceManagerLocalisationStore.getInternal | split.Length < 2 | FALSE
        // CF: ResourceManagerLocalisationStore.getInternal | !resourceManagers.TryGetValue(ns) | TRUE
        // CF: ResourceManagerLocalisationStore.getInternal | matchingAssembly != null | FALSE
        // CF: ResourceManagerLocalisationStore.getInternal | !string.IsNullOrEmpty(assemblyName) | FALSE
        // CF: ResourceManagerLocalisationStore.getInternal | manager == null | TRUE
        [Test]
        public void TestUnknownNamespaceHasNoAssembly()
        {
            Assert.That(new ResourceManagerLocalisationStore("de").Get("No.Such.Assembly.Anywhere:key"), Is.Null);
        }

        // CF: ResourceManagerLocalisationStore.getInternal | matchingAssembly != null | TRUE
        // CF: ResourceManagerLocalisationStore.getInternal | manager == null | FALSE
        // CF: ResourceManagerLocalisationStore.getInternal | EffectiveCulture.Name == "en" | FALSE
        // CF: ResourceManagerLocalisationStore.getInternal | manager.GetString throws MissingManifestResourceException | TRUE
        [Test]
        public void TestNamespaceWithoutManifestIsIgnored()
        {
            Assert.That(new ResourceManagerLocalisationStore("de").Get("osu.Game.Localisation.NoSuchResource:key"), Is.Null);
        }

        // CF: ResourceManagerLocalisationStore.getInternal | EffectiveCulture.Name == "en" | TRUE
        [Test]
        public void TestEnglishAlwaysFallsBackToCode()
        {
            Assert.That(new ResourceManagerLocalisationStore("en").Get("osu.Game.Localisation.CommonStrings:cancel"), Is.Null);
        }

        // CF: ResourceManagerLocalisationStore.Get | lookupCache.TryGetValue(lookup, out cached) | TRUE
        // CF: ResourceManagerLocalisationStore.Get | lookupCache.TryGetValue(lookup, out cached) | FALSE
        [Test]
        public void TestRepeatedLookupIsCached()
        {
            var store = new ResourceManagerLocalisationStore("de");
            Assert.That(store.Get("osu.Game.Localisation.NoSuchResource:key"), Is.Null);
            Assert.That(store.Get("osu.Game.Localisation.NoSuchResource:key"), Is.Null);
        }

        [Test]
        public async Task TestGetAsyncMatchesGet()
        {
            Assert.That(await new ResourceManagerLocalisationStore("de").GetAsync("nothing"), Is.Null);
        }

        [Test]
        public void TestStreamsNotImplemented()
        {
            var store = new ResourceManagerLocalisationStore("de");
            Assert.Throws<NotImplementedException>(() => store.GetStream("x"));
            Assert.Throws<NotImplementedException>(() => store.GetAvailableResources());
            Assert.DoesNotThrow(() => store.Dispose());
        }
    }
}
