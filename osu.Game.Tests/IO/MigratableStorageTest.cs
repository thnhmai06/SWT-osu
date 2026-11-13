// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using NUnit.Framework;
using osu.Framework.Platform;
using osu.Game.IO;

namespace osu.Game.Tests.IO
{
    [TestFixture]
    public class MigratableStorageTest
    {
        private string root = null!;
        private string source = null!;
        private NativeStorage destinationStorage = null!;
        private string destination = null!;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "osu-migrate-" + Guid.NewGuid().ToString("N"));
            source = Path.Combine(root, "source");
            destination = Path.Combine(root, "destination");
            Directory.CreateDirectory(source);
            destinationStorage = new NativeStorage(destination);
        }

        [TearDown]
        public void TearDown() => Directory.Delete(root, true);

        private class TestMigratable : MigratableStorage
        {
            public TestMigratable(Storage storage)
                : base(storage)
            {
            }

            public override string[] IgnoreDirectories => new[] { "keep-dir" };
            public override string[] IgnoreFiles => new[] { "keep.txt" };
            public override string[] IgnoreSuffixes => new[] { ".tmp" };
        }

        private TestMigratable create() => new TestMigratable(new NativeStorage(source));

        // CF: MigratableStorage.Migrate | sourceUri == destinationUri | TRUE
        // CF: MigratableStorage.Migrate | destination equals source | throws ArgumentException
        [Test]
        public void TestMigrateToSameLocationThrows() => Assert.Throws<ArgumentException>(() => create().Migrate(new NativeStorage(source)));

        // CF: MigratableStorage.Migrate | sourceUri.IsBaseOf(destinationUri) | TRUE
        // CF: MigratableStorage.Migrate | destination inside source | throws ArgumentException
        [Test]
        public void TestMigrateIntoSubdirectoryThrows() => Assert.Throws<ArgumentException>(() => create().Migrate(new NativeStorage(Path.Combine(source, "inner"))));

        // CF: MigratableStorage.Migrate | destination.Exists | TRUE
        // CF: MigratableStorage.Migrate | destination.GetFiles().Length > 0 | TRUE
        // CF: MigratableStorage.Migrate | destination has files | throws ArgumentException
        [Test]
        public void TestMigrateToDestinationWithFilesThrows()
        {
            Directory.CreateDirectory(destination);
            File.WriteAllText(Path.Combine(destination, "x"), "x");

            Assert.Throws<ArgumentException>(() => create().Migrate(destinationStorage));
        }

        // CF: MigratableStorage.Migrate | destination.GetFiles().Length > 0 | FALSE
        // CF: MigratableStorage.Migrate | destination.GetDirectories().Length > 0 | TRUE
        [Test]
        public void TestMigrateToDestinationWithDirectoriesThrows()
        {
            Directory.CreateDirectory(Path.Combine(destination, "d"));

            Assert.Throws<ArgumentException>(() => create().Migrate(destinationStorage));
        }

        // CF: MigratableStorage.Migrate | destination.GetDirectories().Length > 0 | FALSE
        // CF: MigratableStorage.CopyRecursive | !destination.Exists | FALSE
        [Test]
        public void TestMigrateToEmptyExistingDestination()
        {
            Directory.CreateDirectory(destination);
            File.WriteAllText(Path.Combine(source, "a.txt"), "a");

            Assert.That(create().Migrate(destinationStorage), Is.True);
            Assert.That(File.ReadAllText(Path.Combine(destination, "a.txt")), Is.EqualTo("a"));
        }

        // CF: MigratableStorage.Migrate | destination.Exists | FALSE
        // CF: MigratableStorage.CopyRecursive | !destination.Exists | TRUE
        // CF: MigratableStorage.CopyRecursive | foreach files | loop n
        // CF: MigratableStorage.CopyRecursive | foreach directories | loop n
        // CF: MigratableStorage.DeleteRecursive | target empty after deletion | TRUE
        [Test]
        public void TestMigrateCopiesEverythingAndRemovesSource()
        {
            File.WriteAllText(Path.Combine(source, "a.txt"), "a");
            Directory.CreateDirectory(Path.Combine(source, "dir", "nested"));
            File.WriteAllText(Path.Combine(source, "dir", "nested", "b.txt"), "b");
            var storage = create();

            bool result = storage.Migrate(destinationStorage);

            Assert.That(result, Is.True);
            Assert.That(File.Exists(Path.Combine(destination, "a.txt")), Is.True);
            Assert.That(File.ReadAllText(Path.Combine(destination, "dir", "nested", "b.txt")), Is.EqualTo("b"));
            Assert.That(Directory.Exists(source), Is.False);
            Assert.That(storage.GetFullPath("a.txt"), Is.EqualTo(Path.Combine(destination, "a.txt")));
        }

        // CF: MigratableStorage.CopyRecursive | topLevelExcludes && IgnoreFiles.Contains | TRUE
        // CF: MigratableStorage.CopyRecursive | IgnoreSuffixes.Any file | TRUE
        // CF: MigratableStorage.CopyRecursive | topLevelExcludes && IgnoreDirectories.Contains | TRUE
        // CF: MigratableStorage.CopyRecursive | IgnoreSuffixes.Any directory | TRUE
        // CF: MigratableStorage.DeleteRecursive | topLevelExcludes && IgnoreFiles.Contains | TRUE
        // CF: MigratableStorage.DeleteRecursive | topLevelExcludes && IgnoreDirectories.Contains | TRUE
        // CF: MigratableStorage.DeleteRecursive | IgnoreSuffixes.Any | TRUE
        // CF: MigratableStorage.DeleteRecursive | target empty after deletion | FALSE
        [Test]
        public void TestIgnoredEntriesAreNeitherCopiedNorDeleted()
        {
            File.WriteAllText(Path.Combine(source, "keep.txt"), "k");
            File.WriteAllText(Path.Combine(source, "scratch.tmp"), "t");
            File.WriteAllText(Path.Combine(source, "move.txt"), "m");
            Directory.CreateDirectory(Path.Combine(source, "keep-dir"));
            Directory.CreateDirectory(Path.Combine(source, "cache.tmp"));

            bool result = create().Migrate(destinationStorage);

            Assert.That(result, Is.True);
            Assert.That(File.Exists(Path.Combine(destination, "move.txt")), Is.True);
            Assert.That(File.Exists(Path.Combine(destination, "keep.txt")), Is.False);
            Assert.That(File.Exists(Path.Combine(destination, "scratch.tmp")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(destination, "keep-dir")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(destination, "cache.tmp")), Is.False);

            Assert.That(File.Exists(Path.Combine(source, "keep.txt")), Is.True);
            Assert.That(File.Exists(Path.Combine(source, "scratch.tmp")), Is.True);
            Assert.That(Directory.Exists(Path.Combine(source, "keep-dir")), Is.True);
            Assert.That(File.Exists(Path.Combine(source, "move.txt")), Is.False);
        }

        // CF: MigratableStorage.CopyRecursive | topLevelExcludes && IgnoreFiles.Contains | FALSE
        // CF: MigratableStorage.CopyRecursive | topLevelExcludes && IgnoreDirectories.Contains | FALSE
        [Test]
        public void TestIgnoreListsOnlyApplyAtTopLevel()
        {
            Directory.CreateDirectory(Path.Combine(source, "dir", "keep-dir"));
            File.WriteAllText(Path.Combine(source, "dir", "keep.txt"), "k");

            create().Migrate(destinationStorage);

            Assert.That(File.Exists(Path.Combine(destination, "dir", "keep.txt")), Is.True);
            Assert.That(Directory.Exists(Path.Combine(destination, "dir", "keep-dir")), Is.True);
        }

        // CF: MigratableStorage.CopyRecursive | foreach files | loop 0
        // CF: MigratableStorage.CopyRecursive | foreach directories | loop 0
        [Test]
        public void TestMigrateEmptySource()
        {
            bool result = create().Migrate(destinationStorage);

            Assert.That(result, Is.True);
            Assert.That(Directory.Exists(destination), Is.True);
        }
    }
}
