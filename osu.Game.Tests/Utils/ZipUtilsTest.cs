// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.IO.Compression;
using System.Text;
using NUnit.Framework;
using osu.Game.Utils;

namespace osu.Game.Tests.Utils
{
    [TestFixture]
    public class ZipUtilsTest
    {
        private string tempDir = null!;

        [SetUp]
        public void SetUp() => tempDir = Directory.CreateTempSubdirectory("zip-utils-test").FullName;

        [TearDown]
        public void TearDown() => Directory.Delete(tempDir, true);

        private static byte[] createZip(bool withEntry)
        {
            var ms = new MemoryStream();

            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                if (withEntry)
                {
                    using var w = new StreamWriter(zip.CreateEntry("a.txt").Open());
                    w.Write("hello");
                }
            }

            return ms.ToArray();
        }

        // CF: ZipUtils.IsZipArchive | arc.Entries.Any() | TRUE
        // CF: ZipUtils.IsZipArchive | foreach arc.Entries | loop 1
        [Test]
        public void TestStreamValidZip()
        {
            var stream = new MemoryStream(createZip(true));
            Assert.That(ZipUtils.IsZipArchive(stream), Is.True);
            Assert.That(stream.Position, Is.EqualTo(0));
        }

        // CF: ZipUtils.IsZipArchive | arc.Entries.Any() | FALSE
        // CF: ZipUtils.IsZipArchive | foreach arc.Entries | loop 0
        [Test]
        public void TestStreamEmptyZip()
        {
            Assert.That(ZipUtils.IsZipArchive(new MemoryStream(createZip(false))), Is.False);
        }

        // CF: ZipUtils.IsZipArchive | catch Exception | TRUE
        [Test]
        public void TestStreamNotZip()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("definitely not a zip"));
            Assert.That(ZipUtils.IsZipArchive(stream), Is.False);
            Assert.That(stream.Position, Is.EqualTo(0));
        }

        // CF: ZipUtils.IsZipArchive | foreach arc.Entries | loop n
        [Test]
        public void TestPathMultipleEntries()
        {
            string path = Path.Combine(tempDir, "multi.zip");

            using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                zip.CreateEntry("a.txt");
                zip.CreateEntry("b.txt");
            }

            Assert.That(ZipUtils.IsZipArchive(path), Is.True);
        }

        // CF: ZipUtils.IsZipArchive | !File.Exists(path) | TRUE
        [Test]
        public void TestPathMissingFile()
        {
            Assert.That(ZipUtils.IsZipArchive(Path.Combine(tempDir, "missing.zip")), Is.False);
        }

        // CF: ZipUtils.IsZipArchive | !File.Exists(path) | FALSE
        // CF: ZipUtils.IsZipArchive | path: arc.Entries.Any() | TRUE
        [Test]
        public void TestPathValidZip()
        {
            string path = Path.Combine(tempDir, "ok.zip");
            File.WriteAllBytes(path, createZip(true));
            Assert.That(ZipUtils.IsZipArchive(path), Is.True);
        }

        // CF: ZipUtils.IsZipArchive | path: arc.Entries.Any() | FALSE
        [Test]
        public void TestPathEmptyZip()
        {
            string path = Path.Combine(tempDir, "empty.zip");
            File.WriteAllBytes(path, createZip(false));
            Assert.That(ZipUtils.IsZipArchive(path), Is.False);
        }

        // CF: ZipUtils.IsZipArchive | path: catch Exception | TRUE
        [Test]
        public void TestPathNotZip()
        {
            string path = Path.Combine(tempDir, "bad.zip");
            File.WriteAllText(path, "not a zip");
            Assert.That(ZipUtils.IsZipArchive(path), Is.False);
        }
    }
}
