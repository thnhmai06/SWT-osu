// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.IO.Compression;
using NUnit.Framework;
using osu.Game.Database;
using osu.Game.IO.Archives;
using SharpCompress.Common;

namespace osu.Game.Tests.Database
{
    [TestFixture]
    public class ImportTaskTest
    {
        private string tempDir = null!;

        [SetUp]
        public void SetUp()
        {
            tempDir = Path.Combine(Path.GetTempPath(), "osu-importtask-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
        }

        [TearDown]
        public void TearDown() => Directory.Delete(tempDir, true);

        private static byte[] zipBytes()
        {
            var ms = new MemoryStream();

            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                using var w = new StreamWriter(zip.CreateEntry("a.txt").Open());
                w.Write("hi");
            }

            return ms.ToArray();
        }

        private string write(string name, byte[] content)
        {
            string path = Path.Combine(tempDir, name);
            File.WriteAllBytes(path, content);
            return path;
        }


        // CF: ImportTask.GetReader | Stream == null | TRUE
        // CF: ImportTask.GetReader | ZipUtils.IsZipArchive(Path) | TRUE
        [Test]
        public void TestZipPathUsesZipReader()
        {
            using var reader = new ImportTask(write("map.osz", zipBytes())).GetReader();

            Assert.That(reader, Is.TypeOf<ZipArchiveReader>());
            Assert.That(reader.Name, Is.EqualTo("map.osz"));
        }

        // CF: ImportTask.GetReader | ZipUtils.IsZipArchive(Path) | FALSE
        // CF: ImportTask.GetReader | Directory.Exists(Path) | TRUE
        [Test]
        public void TestDirectoryPathUsesDirectoryReader()
        {
            using var reader = new ImportTask(tempDir).GetReader();

            Assert.That(reader, Is.TypeOf<DirectoryArchiveReader>());
        }

        // CF: ImportTask.GetReader | Directory.Exists(Path) | FALSE
        // CF: ImportTask.GetReader | File.Exists(Path) | TRUE
        [Test]
        public void TestPlainFilePathUsesSingleFileReader()
        {
            using var reader = new ImportTask(write("map.osu", new byte[] { 1, 2 })).GetReader();

            Assert.That(reader, Is.TypeOf<SingleFileArchiveReader>());
        }

        // CF: ImportTask.GetReader | File.Exists(Path) | FALSE
        // CF: ImportTask.GetReader | invalid path | throws InvalidFormatException
        [Test]
        public void TestMissingPathThrows()
            => Assert.Throws<InvalidFormatException>(() => new ImportTask(Path.Combine(tempDir, "missing.osz")).GetReader());

        // CF: ImportTask.GetReader | Stream == null | FALSE
        // CF: ImportTask.GetReader | Stream is not MemoryStream | FALSE
        // CF: ImportTask.GetReader | ZipUtils.IsZipArchive(memoryStream) | TRUE
        [Test]
        public void TestZipStreamUsesZipReader()
        {
            using var reader = new ImportTask(new MemoryStream(zipBytes()), "stream.osz").GetReader();

            Assert.That(reader, Is.TypeOf<ZipArchiveReader>());
            Assert.That(reader.Name, Is.EqualTo("stream.osz"));
        }

        // CF: ImportTask.GetReader | ZipUtils.IsZipArchive(memoryStream) | FALSE
        [Test]
        public void TestNonZipStreamUsesMemoryStreamReader()
        {
            using var reader = new ImportTask(new MemoryStream(new byte[] { 1, 2, 3 }), "x.osu").GetReader();

            Assert.That(reader, Is.TypeOf<MemoryStreamArchiveReader>());
        }

        // CF: ImportTask.GetReader | Stream is not MemoryStream | TRUE
        [Test]
        public void TestNonMemoryStreamIsCopiedAndDisposed()
        {
            var source = new SeekableNonMemoryStream(new byte[] { 1, 2, 3 });

            using var reader = new ImportTask(source, "x.osu").GetReader();

            Assert.That(reader, Is.TypeOf<MemoryStreamArchiveReader>());
            Assert.That(source.Disposed, Is.True);
        }

        // CF: ImportTask.DeleteFile | File.Exists(Path) | TRUE
        [Test]
        public void TestDeleteFileRemovesFile()
        {
            string path = write("a.osz", new byte[] { 1 });

            new ImportTask(path).DeleteFile();

            Assert.That(File.Exists(path), Is.False);
        }

        // CF: ImportTask.DeleteFile | File.Exists(Path) | FALSE
        [Test]
        public void TestDeleteMissingFileDoesNotThrow() => Assert.DoesNotThrow(() => new ImportTask(Path.Combine(tempDir, "none")).DeleteFile());

        // CF: ImportTask.ToString | Path.GetFileName(Path) | non-null
        [Test]
        public void TestToStringIsFilename() => Assert.That(new ImportTask(Path.Combine(tempDir, "map.osz")).ToString(), Is.EqualTo("map.osz"));

        private class SeekableNonMemoryStream : Stream
        {
            private readonly MemoryStream inner;
            public bool Disposed { get; private set; }

            public SeekableNonMemoryStream(byte[] bytes) => inner = new MemoryStream(bytes);

            public override bool CanRead => true;
            public override bool CanSeek => true;
            public override bool CanWrite => false;
            public override long Length => inner.Length;
            public override long Position { get => inner.Position; set => inner.Position = value; }
            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
            public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                Disposed = true;
                base.Dispose(disposing);
            }
        }

    }
}
