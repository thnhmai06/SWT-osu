// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using osu.Game.IO.Legacy;

namespace osu.Game.Tests.IO.Legacy
{
    [TestFixture]
    public class SerializationReaderWriterTest
    {
        private class Item : ILegacySerializable
        {
            public int Value;

            public void ReadFromStream(SerializationReader sr)
            {
                Value = sr.ReadInt32();
                if (Value == 13) throw new InvalidDataException("unlucky");
            }

            public void WriteToStream(SerializationWriter sw) => sw.Write(Value);
        }

        private static (SerializationWriter writer, MemoryStream stream) newWriter()
        {
            var ms = new MemoryStream();
            return (new SerializationWriter(ms, true), ms);
        }

        private static SerializationReader reader(MemoryStream ms)
        {
            ms.Position = 0;
            return new SerializationReader(ms);
        }

        // CF: SerializationWriter.GetWriter | creates memory backed writer | non-null
        [Test]
        public void TestGetWriter()
        {
            using var w = SerializationWriter.GetWriter();
            w.Write(1);

            Assert.That(w.BaseStream, Is.TypeOf<MemoryStream>());
            Assert.That(w.BaseStream.Length, Is.EqualTo(4));
        }

        // CF: SerializationWriter.Write(string) | str == null | TRUE
        // CF: SerializationReader.ReadString | ReadByte() == 0 | TRUE
        [Test]
        public void TestNullStringRoundTrip()
        {
            var (w, ms) = newWriter();
            w.Write((string)null!);

            Assert.That(ms.ToArray(), Is.EqualTo(new byte[] { (byte)ObjType.NullType }));
            Assert.That(reader(ms).ReadString(), Is.Null);
        }

        // CF: SerializationWriter.Write(string) | str == null | FALSE
        // CF: SerializationReader.ReadString | ReadByte() == 0 | FALSE
        [Test]
        public void TestStringRoundTrip()
        {
            var (w, ms) = newWriter();
            w.Write("héllo");

            Assert.That(ms.ToArray()[0], Is.EqualTo((byte)ObjType.StringType));
            Assert.That(reader(ms).ReadString(), Is.EqualTo("héllo"));
        }

        // CF: SerializationWriter.Write(byte[]) | b == null | TRUE
        // CF: SerializationReader.ReadByteArray | len < 0 | TRUE
        // CF: SerializationReader.ReadByteArray | len > 0 | FALSE
        [Test]
        public void TestNullByteArray()
        {
            var (w, ms) = newWriter();
            w.Write((byte[])null!);

            Assert.That(reader(ms).ReadByteArray(), Is.Null);
        }

        // CF: SerializationWriter.Write(byte[]) | b == null | FALSE
        // CF: SerializationWriter.Write(byte[]) | len > 0 | TRUE
        // CF: SerializationReader.ReadByteArray | len > 0 | TRUE
        [Test]
        public void TestByteArrayRoundTrip()
        {
            var (w, ms) = newWriter();
            w.Write(new byte[] { 1, 2, 3 });

            Assert.That(reader(ms).ReadByteArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        }

        // CF: SerializationWriter.Write(byte[]) | len > 0 | FALSE
        // CF: SerializationReader.ReadByteArray | len < 0 | FALSE
        [Test]
        public void TestEmptyByteArray()
        {
            var (w, ms) = newWriter();
            w.Write(Array.Empty<byte>());

            Assert.That(ms.Length, Is.EqualTo(4));
            Assert.That(reader(ms).ReadByteArray(), Is.Empty);
        }

        // CF: SerializationWriter.WriteByteArray | b == null | TRUE
        // CF: SerializationWriter.WriteByteArray | b == null | FALSE
        // CF: SerializationWriter.WriteByteArray | len > 0 | TRUE
        // CF: SerializationWriter.WriteByteArray | len > 0 | FALSE
        [Test]
        public void TestWriteByteArray()
        {
            var (w, ms) = newWriter();
            w.WriteByteArray(null!);
            w.WriteByteArray(Array.Empty<byte>());
            w.WriteByteArray(new byte[] { 9 });

            var r = reader(ms);
            Assert.That(r.ReadByteArray(), Is.Null);
            Assert.That(r.ReadByteArray(), Is.Empty);
            Assert.That(r.ReadByteArray(), Is.EqualTo(new byte[] { 9 }));
        }

        // CF: SerializationWriter.Write(char[]) | c == null | TRUE
        // CF: SerializationReader.ReadCharArray | len < 0 | TRUE
        [Test]
        public void TestNullCharArray()
        {
            var (w, ms) = newWriter();
            w.Write((char[])null!);

            Assert.That(reader(ms).ReadCharArray(), Is.Null);
        }

        // CF: SerializationWriter.Write(char[]) | c == null | FALSE
        // CF: SerializationWriter.Write(char[]) | len > 0 | TRUE
        // CF: SerializationReader.ReadCharArray | len > 0 | TRUE
        [Test]
        public void TestCharArrayRoundTrip()
        {
            var (w, ms) = newWriter();
            w.Write(new[] { 'a', 'b' });

            Assert.That(reader(ms).ReadCharArray(), Is.EqualTo(new[] { 'a', 'b' }));
        }

        // CF: SerializationWriter.Write(char[]) | len > 0 | FALSE
        // CF: SerializationReader.ReadCharArray | len > 0 | FALSE
        // CF: SerializationReader.ReadCharArray | len < 0 | FALSE
        [Test]
        public void TestEmptyCharArray()
        {
            var (w, ms) = newWriter();
            w.Write(Array.Empty<char>());

            Assert.That(reader(ms).ReadCharArray(), Is.Empty);
        }

        // CF: SerializationReader.ReadDateTime | ticks < 0 | FALSE
        [Test]
        public void TestDateTimeRoundTrip()
        {
            var (w, ms) = newWriter();
            var dt = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            w.Write(dt);

            var read = reader(ms).ReadDateTime();
            Assert.That(read, Is.EqualTo(dt));
            Assert.That(read.Kind, Is.EqualTo(DateTimeKind.Utc));
        }

        // CF: SerializationReader.ReadDateTime | ticks < 0 | TRUE
        // CF: SerializationReader.ReadDateTime | negative ticks | throws IOException
        [Test]
        public void TestNegativeTicksThrow()
        {
            var (w, ms) = newWriter();
            w.Write(-5L);

            Assert.Throws<IOException>(() => reader(ms).ReadDateTime());
        }

        // CF: SerializationWriter.Write(List) | c == null | TRUE
        // CF: SerializationReader.ReadBList | count < 0 | TRUE
        [Test]
        public void TestNullLegacyList()
        {
            var (w, ms) = newWriter();
            w.Write((List<Item>)null!);

            Assert.That(reader(ms).ReadBList<Item>(), Is.Null);
        }

        // CF: SerializationWriter.Write(List) | c == null | FALSE
        // CF: SerializationWriter.Write(List) | for items | loop n
        // CF: SerializationReader.ReadBList | count < 0 | FALSE
        // CF: SerializationReader.ReadBList | for items | loop n
        [Test]
        public void TestLegacyListRoundTrip()
        {
            var (w, ms) = newWriter();
            w.Write(new List<Item> { new Item { Value = 1 }, new Item { Value = 2 } });

            Assert.That(reader(ms).ReadBList<Item>()!.Select(i => i.Value), Is.EqualTo(new[] { 1, 2 }));
        }

        // CF: SerializationWriter.Write(List) | for items | loop 0
        // CF: SerializationReader.ReadBList | for items | loop 0
        [Test]
        public void TestEmptyLegacyList()
        {
            var (w, ms) = newWriter();
            w.Write(new List<Item>());

            Assert.That(reader(ms).ReadBList<Item>(), Is.Empty);
        }

        // CF: SerializationReader.ReadBList | item read throws | throws InvalidDataException
        // CF: SerializationReader.ReadBList | skipErrors | FALSE
        [Test]
        public void TestLegacyListErrorsPropagate()
        {
            var (w, ms) = newWriter();
            w.Write(new List<Item> { new Item { Value = 13 } });

            Assert.Throws<InvalidDataException>(() => reader(ms).ReadBList<Item>());
        }

        // CF: SerializationReader.ReadBList | skipErrors | TRUE
        [Test]
        public void TestLegacyListSkipsErrors()
        {
            var (w, ms) = newWriter();
            w.Write(new List<Item> { new Item { Value = 1 }, new Item { Value = 13 }, new Item { Value = 3 } });

            Assert.That(reader(ms).ReadBList<Item>(true)!.Select(i => i.Value), Is.EqualTo(new[] { 1, 3 }));
        }

        // CF: SerializationReader.ReadList | count < 0 | TRUE
        [Test]
        public void TestNullObjectList()
        {
            var (w, ms) = newWriter();
            w.Write(-1);

            Assert.That(reader(ms).ReadList<int>(), Is.Null);
        }

        // CF: SerializationReader.ReadList | count < 0 | FALSE
        // CF: SerializationReader.ReadList | for items | loop n
        [Test]
        public void TestObjectList()
        {
            var (w, ms) = newWriter();
            w.Write(2);
            w.WriteObject(5);
            w.WriteObject(6);

            Assert.That(reader(ms).ReadList<int>(), Is.EqualTo(new[] { 5, 6 }));
        }

        // CF: SerializationReader.ReadList | for items | loop 0
        [Test]
        public void TestEmptyObjectList()
        {
            var (w, ms) = newWriter();
            w.Write(0);

            Assert.That(reader(ms).ReadList<int>(), Is.Empty);
        }

        // CF: SerializationWriter.Write(IDictionary) | d == null | TRUE
        // CF: SerializationReader.ReadDictionary | count < 0 | TRUE
        [Test]
        public void TestNullDictionary()
        {
            var (w, ms) = newWriter();
            w.Write((IDictionary<string, int>)null!);

            Assert.That(reader(ms).ReadDictionary<string, int>(), Is.Null);
        }

        // CF: SerializationWriter.Write(IDictionary) | d == null | FALSE
        // CF: SerializationWriter.Write(IDictionary) | foreach pairs | loop n
        // CF: SerializationReader.ReadDictionary | count < 0 | FALSE
        // CF: SerializationReader.ReadDictionary | for items | loop n
        [Test]
        public void TestDictionaryRoundTrip()
        {
            var (w, ms) = newWriter();
            w.Write<string, int>(new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 });

            var read = reader(ms).ReadDictionary<string, int>()!;
            Assert.That(read, Is.EquivalentTo(new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 }));
        }

        // CF: SerializationWriter.Write(IDictionary) | foreach pairs | loop 0
        // CF: SerializationReader.ReadDictionary | for items | loop 0
        [Test]
        public void TestEmptyDictionary()
        {
            var (w, ms) = newWriter();
            w.Write<string, int>(new Dictionary<string, int>());

            Assert.That(reader(ms).ReadDictionary<string, int>(), Is.Empty);
        }

        // CF: SerializationWriter.WriteObject | obj == null | TRUE
        // CF: SerializationReader.ReadObject | switch type | default
        [Test]
        public void TestObjectNull()
        {
            var (w, ms) = newWriter();
            w.WriteObject(null!);

            Assert.That(reader(ms).ReadObject(), Is.Null);
        }

        // CF: SerializationWriter.WriteObject | obj == null | FALSE
        // CF: SerializationWriter.WriteObject | switch obj | case bool
        // CF: SerializationWriter.WriteObject | switch obj | case byte
        // CF: SerializationWriter.WriteObject | switch obj | case ushort
        // CF: SerializationWriter.WriteObject | switch obj | case uint
        // CF: SerializationWriter.WriteObject | switch obj | case ulong
        // CF: SerializationWriter.WriteObject | switch obj | case sbyte
        // CF: SerializationWriter.WriteObject | switch obj | case short
        // CF: SerializationWriter.WriteObject | switch obj | case int
        // CF: SerializationWriter.WriteObject | switch obj | case long
        // CF: SerializationWriter.WriteObject | switch obj | case char
        // CF: SerializationWriter.WriteObject | switch obj | case string
        // CF: SerializationWriter.WriteObject | switch obj | case float
        // CF: SerializationWriter.WriteObject | switch obj | case double
        // CF: SerializationWriter.WriteObject | switch obj | case decimal
        // CF: SerializationWriter.WriteObject | switch obj | case DateTime
        // CF: SerializationReader.ReadObject | switch type | case BoolType
        // CF: SerializationReader.ReadObject | switch type | case ByteType
        // CF: SerializationReader.ReadObject | switch type | case UInt16Type
        // CF: SerializationReader.ReadObject | switch type | case UInt32Type
        // CF: SerializationReader.ReadObject | switch type | case UInt64Type
        // CF: SerializationReader.ReadObject | switch type | case SByteType
        // CF: SerializationReader.ReadObject | switch type | case Int16Type
        // CF: SerializationReader.ReadObject | switch type | case Int32Type
        // CF: SerializationReader.ReadObject | switch type | case Int64Type
        // CF: SerializationReader.ReadObject | switch type | case CharType
        // CF: SerializationReader.ReadObject | switch type | case StringType
        // CF: SerializationReader.ReadObject | switch type | case SingleType
        // CF: SerializationReader.ReadObject | switch type | case DoubleType
        // CF: SerializationReader.ReadObject | switch type | case DecimalType
        // CF: SerializationReader.ReadObject | switch type | case DateTimeType
        [Test]
        public void TestObjectScalarRoundTrips()
        {
            object[] values =
            {
                true, (byte)1, (ushort)2, 3u, 4ul, (sbyte)-5, (short)-6, -7, -8L, 'x', "str", 1.5f, 2.25, 3.5m,
                new DateTime(2021, 5, 6, 0, 0, 0, DateTimeKind.Utc)
            };

            var (w, ms) = newWriter();
            foreach (object v in values) w.WriteObject(v);

            var r = reader(ms);
            foreach (object v in values)
            {
                object read = r.ReadObject();
                Assert.That(read, Is.EqualTo(v));
                Assert.That(read.GetType(), Is.EqualTo(v.GetType()));
            }

            Assert.That(r.RemainingBytes, Is.EqualTo(0));
        }

        // CF: SerializationWriter.WriteObject | switch obj | case byte[]
        // CF: SerializationWriter.WriteObject | switch obj | case char[]
        [Test]
        public void TestObjectArraysWriteTypeTag()
        {
            var (w, ms) = newWriter();
            w.WriteObject(new byte[] { 1 });
            Assert.That(ms.ToArray()[0], Is.EqualTo((byte)ObjType.ByteArrayType));

            var (w2, ms2) = newWriter();
            w2.WriteObject(new[] { 'a' });
            Assert.That(ms2.ToArray()[0], Is.EqualTo((byte)ObjType.CharArrayType));
        }

        // CF: SerializationReader.ReadObject | switch type | case ByteArrayType
        // CF: SerializationReader.ReadObject | switch type | case CharArrayType
        [Test]
        public void TestReadObjectArrays()
        {
            var ms = new MemoryStream();
            var w = new SerializationWriter(ms, true);
            w.Write((byte)ObjType.ByteArrayType);
            w.Write(new byte[] { 4, 5 });
            w.Write((byte)ObjType.CharArrayType);
            w.Write(new[] { 'q' });

            var r = reader(ms);
            Assert.That(r.ReadObject(), Is.EqualTo(new byte[] { 4, 5 }));
            Assert.That(r.ReadObject(), Is.EqualTo(new[] { 'q' }));
        }

        // CF: SerializationWriter.WriteObject | switch obj | default
        // CF: SerializationWriter.WriteObject | unsupported type | throws IOException
        [Test]
        public void TestWriteObjectUnsupportedThrows()
        {
            var (w, _) = newWriter();

            Assert.Throws<IOException>(() => w.WriteObject(new object()));
        }

        // CF: SerializationReader.ReadObject | switch type | case OtherType
        // CF: SerializationReader.ReadObject | other type | throws IOException
        [Test]
        public void TestReadObjectOtherTypeThrows()
        {
            var ms = new MemoryStream(new[] { (byte)ObjType.OtherType });

            Assert.Throws<IOException>(() => new SerializationReader(ms).ReadObject());
        }

        // CF: SerializationWriter.WriteRawBytes | writes without prefix | no throw
        // CF: SerializationWriter.WriteUtf8 | writes encoded string without prefix | no throw
        [Test]
        public void TestRawAndUtf8()
        {
            var (w, ms) = newWriter();
            w.WriteRawBytes(new byte[] { 1, 2 });
            w.WriteUtf8("ab");

            Assert.That(ms.ToArray(), Is.EqualTo(new byte[] { 1, 2, 97, 98 }));
        }
    }
}
