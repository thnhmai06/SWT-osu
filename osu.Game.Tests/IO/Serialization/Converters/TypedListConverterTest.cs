// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using osu.Game.IO.Serialization.Converters;

namespace osu.Game.Tests.IO.Serialization.Converters
{
    [TestFixture]
    public class TypedListConverterTest
    {
        public class Base
        {
            public int Value { get; set; }
        }

        public class Derived : Base
        {
            public string Extra { get; set; } = string.Empty;
        }

        private static string typeName(System.Type t) => $"{t.FullName}, {t.Assembly.GetName().Name}";

        private static IReadOnlyList<Base> read(string json, bool versioned = false)
            => JsonConvert.DeserializeObject<IReadOnlyList<Base>>(json, new TypedListConverter<Base>(versioned))!;

        // CF: TypedListConverter.WriteJson | foreach items | loop 0
        [Test]
        public void TestWriteEmpty()
        {
            string json = JsonConvert.SerializeObject(new List<Base>(), new TypedListConverter<Base>());

            Assert.That(json, Is.EqualTo("{\"$lookup_table\":[],\"$items\":[]}"));
        }

        // CF: TypedListConverter.WriteJson | foreach items | loop n
        // CF: TypedListConverter.WriteJson | typeId == -1 | TRUE
        // CF: TypedListConverter.WriteJson | typeId == -1 | FALSE
        // CF: TypedListConverter.WriteJson | requiresTypeVersion | FALSE
        [Test]
        public void TestWriteSharesLookupEntriesAndRoundTrips()
        {
            var items = new List<Base> { new Derived { Value = 1, Extra = "x" }, new Derived { Value = 2 }, new Base { Value = 3 } };

            string json = JsonConvert.SerializeObject(items, new TypedListConverter<Base>());
            var result = read(json);

            Assert.That(json, Does.Contain($"[\"{typeName(typeof(Derived))}\",\"{typeName(typeof(Base))}\"]"));
            Assert.That(result, Has.Count.EqualTo(3));
            Assert.That(result[0], Is.TypeOf<Derived>());
            Assert.That(((Derived)result[0]).Extra, Is.EqualTo("x"));
            Assert.That(result[2], Is.TypeOf<Base>());
            Assert.That(result[2].Value, Is.EqualTo(3));
        }

        // CF: TypedListConverter.WriteJson | requiresTypeVersion | TRUE
        [Test]
        public void TestWriteWithVersionAppendsAssemblyVersion()
        {
            string json = JsonConvert.SerializeObject(new List<Base> { new Base() }, new TypedListConverter<Base>(true));

            string version = typeof(Base).Assembly.GetName().Version!.ToString();
            Assert.That(json, Does.Contain($", {version}\""));
        }

        // CF: TypedListConverter.ReadJson | obj["$lookup_table"] == null | TRUE
        [Test]
        public void TestReadMissingLookupTableIsEmpty() => Assert.That(read("{\"$items\":[]}"), Is.Empty);

        // CF: TypedListConverter.ReadJson | obj["$lookup_table"] == null | FALSE
        // CF: TypedListConverter.ReadJson | lookupTable == null | TRUE
        [Test]
        public void TestReadNullLookupTableIsEmpty() => Assert.That(read("{\"$lookup_table\":null,\"$items\":[]}"), Is.Empty);

        // CF: TypedListConverter.ReadJson | lookupTable == null | FALSE
        // CF: TypedListConverter.ReadJson | obj["$items"] == null | TRUE
        [Test]
        public void TestReadMissingItemsIsEmpty() => Assert.That(read("{\"$lookup_table\":[]}"), Is.Empty);

        // CF: TypedListConverter.ReadJson | obj["$items"] == null | FALSE
        // CF: TypedListConverter.ReadJson | foreach tok | loop 0
        [Test]
        public void TestReadNoItemsIsEmpty() => Assert.That(read("{\"$lookup_table\":[],\"$items\":[]}"), Is.Empty);

        // CF: TypedListConverter.ReadJson | tok["$type"] == null | TRUE
        // CF: TypedListConverter.ReadJson | missing $type | throws JsonException
        [Test]
        public void TestReadMissingTypeThrows()
        {
            string json = $"{{\"$lookup_table\":[\"{typeName(typeof(Base))}\"],\"$items\":[{{\"Value\":1}}]}}";

            Assert.Throws<JsonException>(() => read(json));
        }

        // CF: TypedListConverter.ReadJson | type.IsAssignableTo(typeof(T)) | FALSE
        [Test]
        public void TestReadUnrelatedTypeIsSkipped()
        {
            string json = $"{{\"$lookup_table\":[\"{typeName(typeof(string))}\",\"{typeName(typeof(Base))}\"],\"$items\":[{{\"$type\":0}},{{\"$type\":1,\"Value\":4}}]}}";

            var result = read(json);

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Value, Is.EqualTo(4));
        }

        // CF: TypedListConverter.ReadJson | type.IsAssignableTo(typeof(T)) | TRUE
        [Test]
        public void TestReadAssignableTypeIsInstantiated()
        {
            string json = $"{{\"$lookup_table\":[\"{typeName(typeof(Derived))}\"],\"$items\":[{{\"$type\":0,\"Value\":9,\"Extra\":\"e\"}}]}}";

            var result = read(json);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(((Derived)result[0]).Extra, Is.EqualTo("e"));
        }
    }
}
