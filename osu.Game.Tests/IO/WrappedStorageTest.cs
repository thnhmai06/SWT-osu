// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Platform;
using osu.Game.IO;

namespace osu.Game.Tests.IO
{
    [TestFixture]
    public class WrappedStorageTest
    {
        private string root = null!;
        private NativeStorage native = null!;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "osu-wrapped-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            native = new NativeStorage(root);
        }

        [TearDown]
        public void TearDown() => Directory.Delete(root, true);

        private class ExposedStorage : WrappedStorage
        {
            public ExposedStorage(Storage storage, string? subPath = null)
                : base(storage, subPath)
            {
            }

            public string? Mutate(string? path) => MutatePath(path!);
        }

        // CF: WrappedStorage.MutatePath | path == null | TRUE
        [Test]
        public void TestMutateNullStaysNull() => Assert.That(new ExposedStorage(native, "sub").Mutate(null), Is.Null);

        // CF: WrappedStorage.MutatePath | path == null | FALSE
        // CF: WrappedStorage.MutatePath | !string.IsNullOrEmpty(subPath) | TRUE
        [Test]
        public void TestMutateCombinesSubPath() => Assert.That(new ExposedStorage(native, "sub").Mutate("a.txt"), Is.EqualTo(Path.Combine("sub", "a.txt")));

        // CF: WrappedStorage.MutatePath | !string.IsNullOrEmpty(subPath) | FALSE
        [Test]
        public void TestMutateWithoutSubPathIsIdentity() => Assert.That(new ExposedStorage(native).Mutate("a.txt"), Is.EqualTo("a.txt"));

        // CF: WrappedStorage.GetFullPath | forwards mutated path | no throw
        // CF: WrappedStorage.Exists | forwards mutated path | no throw
        // CF: WrappedStorage.ExistsDirectory | forwards mutated path | no throw
        [Test]
        public void TestPathsAreResolvedUnderSubPath()
        {
            Directory.CreateDirectory(Path.Combine(root, "sub", "dir"));
            File.WriteAllText(Path.Combine(root, "sub", "a.txt"), "x");
            var storage = new WrappedStorage(native, "sub");

            Assert.That(storage.GetFullPath("a.txt"), Is.EqualTo(Path.Combine(root, "sub", "a.txt")));
            Assert.That(storage.Exists("a.txt"), Is.True);
            Assert.That(storage.Exists("missing.txt"), Is.False);
            Assert.That(storage.ExistsDirectory("dir"), Is.True);
            Assert.That(storage.ExistsDirectory("nope"), Is.False);
        }

        // CF: WrappedStorage.Delete | forwards mutated path | no throw
        // CF: WrappedStorage.DeleteDirectory | forwards mutated path | no throw
        [Test]
        public void TestDeleteForwards()
        {
            Directory.CreateDirectory(Path.Combine(root, "sub", "dir"));
            File.WriteAllText(Path.Combine(root, "sub", "a.txt"), "x");
            var storage = new WrappedStorage(native, "sub");

            storage.Delete("a.txt");
            storage.DeleteDirectory("dir");

            Assert.That(File.Exists(Path.Combine(root, "sub", "a.txt")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(root, "sub", "dir")), Is.False);
        }

        // CF: WrappedStorage.GetFiles | ToLocalRelative | loop n
        // CF: WrappedStorage.GetDirectories | ToLocalRelative | loop n
        [Test]
        public void TestListingsAreRelativeToWrappedRoot()
        {
            Directory.CreateDirectory(Path.Combine(root, "sub", "d1"));
            File.WriteAllText(Path.Combine(root, "sub", "a.txt"), "x");
            File.WriteAllText(Path.Combine(root, "sub", "b.txt"), "x");
            var storage = new WrappedStorage(native, "sub");

            Assert.That(storage.GetFiles(string.Empty).OrderBy(f => f).ToArray(), Is.EqualTo(new[] { "a.txt", "b.txt" }));
            Assert.That(storage.GetDirectories(string.Empty).ToArray(), Is.EqualTo(new[] { "d1" }));
        }

        // CF: WrappedStorage.GetFiles | ToLocalRelative | loop 0
        [Test]
        public void TestEmptyListing()
        {
            Directory.CreateDirectory(Path.Combine(root, "sub"));

            Assert.That(new WrappedStorage(native, "sub").GetFiles(string.Empty), Is.Empty);
        }

        // CF: WrappedStorage.GetStream | forwards mutated path | no throw
        // CF: WrappedStorage.Move | forwards mutated paths | no throw
        [Test]
        public void TestStreamAndMove()
        {
            Directory.CreateDirectory(Path.Combine(root, "sub"));
            var storage = new WrappedStorage(native, "sub");

            using (var stream = storage.GetStream("a.txt", FileAccess.Write, FileMode.Create))
                stream.WriteByte(7);

            storage.Move("a.txt", "b.txt");

            Assert.That(File.ReadAllBytes(Path.Combine(root, "sub", "b.txt")), Is.EqualTo(new byte[] { 7 }));
            Assert.That(storage.Exists("a.txt"), Is.False);
        }

        // CF: WrappedStorage.GetStorageForDirectory | string.IsNullOrEmpty(path) | throws ArgumentException
        [Test]
        public void TestStorageForEmptyDirectoryThrows()
        {
            Assert.Throws<ArgumentException>(() => new WrappedStorage(native).GetStorageForDirectory(string.Empty));
            Assert.Throws<ArgumentNullException>(() => new WrappedStorage(native).GetStorageForDirectory(null!));
        }

        // CF: WrappedStorage.GetStorageForDirectory | !path.EndsWith(DirectorySeparatorChar) | TRUE
        [Test]
        public void TestStorageForDirectoryCreatesIt()
        {
            var child = new WrappedStorage(native, "sub").GetStorageForDirectory("child");

            Assert.That(Directory.Exists(Path.Combine(root, "sub", "child")), Is.True);
            Assert.That(child.GetFullPath("."), Does.StartWith(Path.Combine(root, "sub", "child")));
        }

        // CF: WrappedStorage.GetStorageForDirectory | !path.EndsWith(DirectorySeparatorChar) | FALSE
        [Test]
        public void TestStorageForDirectoryWithTrailingSeparator()
        {
            new WrappedStorage(native).GetStorageForDirectory("child" + Path.DirectorySeparatorChar);

            Assert.That(Directory.Exists(Path.Combine(root, "child")), Is.True);
        }
    }
}
