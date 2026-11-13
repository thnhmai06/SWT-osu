// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Game.IO.Archives;

namespace osu.Game.Tests.IO.Archives
{
    [TestFixture]
    public class ZipArchiveReaderTest
    {
        private static ZipArchiveReader create(params (string name, string content)[] entries)
        {
            var ms = new MemoryStream();

            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                foreach (var (name, content) in entries)
                {
                    var entry = zip.CreateEntry(name);
                    using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
                    writer.Write(content);
                }
            }

            ms.Position = 0;
            return new ZipArchiveReader(ms, "test.zip");
        }

        // CF: ZipArchiveReader.GetStream | entry == null | null
        [Test]
        public void TestMissingEntryReturnsNull()
        {
            using var reader = create(("a.txt", "x"));

            Assert.That(reader.GetStream("missing.txt"), Is.Null);
        }

        // CF: ZipArchiveReader.GetStream | entry.Size > MaximumEntrySize | FALSE
        // CF: ZipArchiveReader.GetStream | entry.Size > 0 | TRUE
        [Test]
        public void TestReadsEntryContent()
        {
            using var reader = create(("a.txt", "hello"));

            using var stream = reader.GetStream("a.txt");
            Assert.That(new StreamReader(stream).ReadToEnd(), Is.EqualTo("hello"));
        }

        // CF: ZipArchiveReader.GetStream | entry.Size > 0 | FALSE
        [Test]
        public void TestEmptyEntryReturnsEmptyStream()
        {
            using var reader = create(("empty.txt", string.Empty));

            using var stream = reader.GetStream("empty.txt");
            Assert.That(stream.Length, Is.EqualTo(0));
        }

        // CF: ZipArchiveReader.GetStream | entry.Size > MaximumEntrySize | TRUE
        // CF: ZipArchiveReader.GetStream | oversized entry | throws InsufficientMemoryException
        [Test]
        public void TestOversizedEntryThrows()
        {
            using var reader = create(("a.txt", "hello"));
            reader.MaximumEntrySize = 2;

            Assert.Throws<InsufficientMemoryException>(() => reader.GetStream("a.txt"));
        }

        // CF: ZipArchiveReader.Filenames | !e.IsDirectory | TRUE
        // CF: ZipArchiveReader.Filenames | !e.IsDirectory | FALSE
        [Test]
        public void TestFilenamesExcludeDirectoriesAndSystemFiles()
        {
            using var reader = create(("a.txt", "x"), ("dir/", string.Empty), ("dir/b.txt", "y"), ("__MACOSX/._a.txt", "z"));

            Assert.That(reader.Filenames.OrderBy(n => n).ToArray(), Is.EqualTo(new[] { "a.txt", "dir/b.txt" }));
            Assert.That(reader.GetAvailableResources().Count(), Is.EqualTo(2));
        }

        // CF: ZipArchiveReader.Dispose | archive and stream disposed | no throw
        [Test]
        public void TestDisposeClosesStream()
        {
            var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
                zip.CreateEntry("a.txt");

            ms.Position = 0;
            new ZipArchiveReader(ms).Dispose();

            Assert.That(ms.CanRead, Is.False);
        }

        // CF: ArchiveReader.Get | input?.ReadAllBytesToArray() | non-null
        [Test]
        public void TestGetBytes()
        {
            using var reader = create(("a.txt", "hi"));

            Assert.That(reader.Get("a.txt"), Is.EqualTo(Encoding.UTF8.GetBytes("hi")));
        }

        // CF: ArchiveReader.Get | input?.ReadAllBytesToArray() | null
        [Test]
        public void TestGetBytesMissingIsNull()
        {
            using var reader = create(("a.txt", "hi"));

            Assert.That(reader.Get("nope"), Is.Null);
        }

        // CF: ArchiveReader.GetAsync | input == null | FALSE
        [Test]
        public async Task TestGetAsyncBytes()
        {
            using var reader = create(("a.txt", "hi"));

            Assert.That(await reader.GetAsync("a.txt"), Is.EqualTo(Encoding.UTF8.GetBytes("hi")));
        }

        // CF: ArchiveReader.GetAsync | input == null | TRUE
        [Test]
        public async Task TestGetAsyncMissingIsNull()
        {
            using var reader = create(("a.txt", "hi"));

            Assert.That(await reader.GetAsync("nope"), Is.Null);
        }

        // CF: ArchiveReader.ctor | name assigned | no throw
        [Test]
        public void TestName()
        {
            using var reader = create(("a.txt", "hi"));

            Assert.That(reader.Name, Is.EqualTo("test.zip"));
        }
    }
}
