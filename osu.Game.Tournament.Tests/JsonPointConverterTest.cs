// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Drawing;
using Newtonsoft.Json;
using NUnit.Framework;

namespace osu.Game.Tournament.Tests
{
    [TestFixture]
    public class JsonPointConverterTest
    {
        private static Point read(string json) => JsonConvert.DeserializeObject<Point>(json, new JsonPointConverter());

        // CF: JsonPointConverter.WriteJson | serialises as { X, Y } object
        [Test]
        public void TestWriteUsesObjectFormat()
        {
            Assert.That(JsonConvert.SerializeObject(new Point(3, 4), new JsonPointConverter()), Is.EqualTo("{\"X\":3,\"Y\":4}"));
        }

        // CF: JsonPointConverter.ReadJson | reader.TokenType != StartObject TRUE -> string "x,y" representation
        [Test]
        public void TestReadStringRepresentation()
        {
            Assert.That(read("\"3,4\""), Is.EqualTo(new Point(3, 4)));
        }

        // CF: JsonPointConverter.ReadJson | TokenType != StartObject FALSE; PropertyName X and Y cases
        [Test]
        public void TestReadObjectRepresentation()
        {
            Assert.That(read("{\"X\":3,\"Y\":4}"), Is.EqualTo(new Point(3, 4)));
        }

        // CF: ReadJson | switch case "Y" only; X stays default
        [Test]
        public void TestReadObjectWithOnlyY()
        {
            Assert.That(read("{\"Y\":4}"), Is.EqualTo(new Point(0, 4)));
        }

        // CF: ReadJson | switch no matching case (unknown property name)
        [Test]
        public void TestReadObjectIgnoresUnknownProperty()
        {
            Assert.That(read("{\"Z\":9,\"X\":1}"), Is.EqualTo(new Point(1, 0)));
        }

        // CF: ReadJson | val == null TRUE -> continue (property skipped)
        [Test]
        public void TestReadObjectSkipsNullValue()
        {
            Assert.That(read("{\"X\":null,\"Y\":2}"), Is.EqualTo(new Point(0, 2)));
        }

        // CF: ReadJson | while (reader.Read()) / EndObject -> empty object yields default point
        [Test]
        public void TestReadEmptyObjectYieldsDefaultPoint()
        {
            Assert.That(read("{}"), Is.EqualTo(new Point()));
        }

        // CF: ReadJson | write -> read round trip
        [Test]
        public void TestRoundTrip()
        {
            var converter = new JsonPointConverter();
            var json = JsonConvert.SerializeObject(new Point(-7, 12), converter);

            Assert.That(JsonConvert.DeserializeObject<Point>(json, converter), Is.EqualTo(new Point(-7, 12)));
        }
    }
}
