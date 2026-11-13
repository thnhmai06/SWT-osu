// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Pooling;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Tests.Rulesets.Objects.Pooling
{
    [TestFixture]
    public class HitObjectEntryManagerTest
    {
        private HitObjectEntryManager manager = null!;
        private List<(HitObjectLifetimeEntry entry, HitObject? parent)> added = null!;
        private List<(HitObjectLifetimeEntry entry, HitObject? parent)> removed = null!;

        [SetUp]
        public void SetUp()
        {
            manager = new HitObjectEntryManager();
            added = new List<(HitObjectLifetimeEntry, HitObject?)>();
            removed = new List<(HitObjectLifetimeEntry, HitObject?)>();
            manager.OnEntryAdded += (e, p) => added.Add((e, p));
            manager.OnEntryRemoved += (e, p) => removed.Add((e, p));
        }

        // CF: HitObjectEntryManager.Add | !entryMap.TryAdd(hitObject, entry) | FALSE
        // CF: HitObjectEntryManager.Add | parent != null | FALSE
        [Test]
        public void TestAddWithoutParent()
        {
            var entry = new HitObjectLifetimeEntry(new HitCircle());

            manager.Add(entry, null);

            Assert.That(manager.TryGet(entry.HitObject, out var found), Is.True);
            Assert.That(found, Is.SameAs(entry));
            Assert.That(manager.AllEntries, Is.EqualTo(new[] { entry }));
            Assert.That(added, Is.EqualTo(new[] { (entry, (HitObject?)null) }));
        }

        // CF: HitObjectEntryManager.Add | !entryMap.TryAdd(hitObject, entry) | TRUE
        [Test]
        public void TestAddingTwiceThrows()
        {
            var entry = new HitObjectLifetimeEntry(new HitCircle());
            manager.Add(entry, null);

            Assert.That(() => manager.Add(entry, null), Throws.InstanceOf<InvalidOperationException>());
            Assert.That(added, Has.Count.EqualTo(1));
        }

        // CF: HitObjectEntryManager.Add | parent != null | TRUE
        // CF: HitObjectEntryManager.Add | entryMap.TryGetValue(parent, out parentEntry) | TRUE
        [Test]
        public void TestChildIsAttachedToRegisteredParent()
        {
            var parent = new HitObjectLifetimeEntry(new HitCircle());
            var child = new HitObjectLifetimeEntry(new HitCircle());
            manager.Add(parent, null);

            manager.Add(child, parent.HitObject);

            Assert.That(parent.NestedEntries, Is.EqualTo(new[] { child }));
            Assert.That(added.Last().parent, Is.SameAs(parent.HitObject));
        }

        // CF: HitObjectEntryManager.Add | entryMap.TryGetValue(parent, out parentEntry) | FALSE
        [Test]
        public void TestChildOfUnregisteredParentIsStillAdded()
        {
            var child = new HitObjectLifetimeEntry(new HitCircle());
            var parent = new HitCircle();

            manager.Add(child, parent);

            Assert.That(manager.TryGet(child.HitObject, out _), Is.True);
            Assert.That(manager.Remove(child), Is.True);
            Assert.That(removed.Single().parent, Is.SameAs(parent), "the remembered parent is still reported on removal");
        }

        // CF: HitObjectEntryManager.TryGet | entryMap.TryGetValue | FALSE
        [Test]
        public void TestTryGetUnknown()
        {
            Assert.That(manager.TryGet(new HitCircle(), out var entry), Is.False);
            Assert.That(entry, Is.Null);
        }

        // CF: HitObjectEntryManager.Remove | entry is SyntheticHitObjectEntry | TRUE
        [Test]
        public void TestSyntheticEntriesAreNeverRemoved()
        {
            var entry = new SyntheticHitObjectEntry(new HitCircle());

            Assert.That(manager.Remove(entry), Is.False);
            Assert.That(removed, Is.Empty);
        }

        // CF: HitObjectEntryManager.Remove | !entryMap.Remove(hitObject) | TRUE
        [Test]
        public void TestRemovingUnknownEntryThrows()
        {
            Assert.That(() => manager.Remove(new HitObjectLifetimeEntry(new HitCircle())), Throws.InstanceOf<InvalidOperationException>());
        }

        // CF: HitObjectEntryManager.Remove | !entryMap.Remove(hitObject) | FALSE
        // CF: HitObjectEntryManager.Remove | parentMap.Remove(entry, out parent) && entryMap.TryGetValue(parent, out parentEntry) | TRUE
        // CF: HitObjectEntryManager.Remove | foreach childEntry in entry.NestedEntries | loop 0
        [Test]
        public void TestRemoveDetachesFromParent()
        {
            var parent = new HitObjectLifetimeEntry(new HitCircle());
            var child = new HitObjectLifetimeEntry(new HitCircle());
            manager.Add(parent, null);
            manager.Add(child, parent.HitObject);

            Assert.That(manager.Remove(child), Is.True);

            Assert.That(parent.NestedEntries, Is.Empty);
            Assert.That(manager.TryGet(child.HitObject, out _), Is.False);
            Assert.That(removed.Single().entry, Is.SameAs(child));
        }

        // CF: HitObjectEntryManager.Remove | parentMap.Remove(entry, out parent) | FALSE
        // CF: HitObjectEntryManager.Remove | foreach childEntry in entry.NestedEntries | loop n
        [Test]
        public void TestRemoveCascadesToChildren()
        {
            var parent = new HitObjectLifetimeEntry(new HitCircle());
            var child1 = new HitObjectLifetimeEntry(new HitCircle());
            var child2 = new HitObjectLifetimeEntry(new HitCircle());
            manager.Add(parent, null);
            manager.Add(child1, parent.HitObject);
            manager.Add(child2, parent.HitObject);

            manager.Remove(parent);

            Assert.That(manager.AllEntries, Is.Empty);
            Assert.That(removed.Select(r => r.entry), Is.EquivalentTo(new[] { parent, child1, child2 }));
        }

        // CF: HitObjectEntryManager.onDefaultsApplied | entryMap.TryGetValue(hitObject, out entry) | TRUE
        // CF: HitObjectEntryManager.onDefaultsApplied | foreach nested in previousEntries | loop n
        [Test]
        public void TestReapplyingDefaultsDropsNestedEntries()
        {
            var parentObject = new HitCircle();
            var parent = new HitObjectLifetimeEntry(parentObject);
            var child = new HitObjectLifetimeEntry(new HitCircle());
            manager.Add(parent, null);
            manager.Add(child, parentObject);

            parentObject.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());

            Assert.That(parent.NestedEntries, Is.Empty);
            Assert.That(manager.TryGet(child.HitObject, out _), Is.False);
            Assert.That(manager.TryGet(parentObject, out _), Is.True, "the parent itself stays");
            Assert.That(removed.Select(r => r.entry), Is.EqualTo(new[] { child }));
        }

        // CF: HitObjectEntryManager.onDefaultsApplied | foreach nested in previousEntries | loop 0
        [Test]
        public void TestReapplyingDefaultsWithoutNestedEntriesRemovesNothing()
        {
            var obj = new HitCircle();
            manager.Add(new HitObjectLifetimeEntry(obj), null);

            obj.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());

            Assert.That(removed, Is.Empty);
        }

        // CF: HitObjectEntryManager.onDefaultsApplied | entryMap.TryGetValue(hitObject, out entry) | infeasible: the handler is unsubscribed whenever the entry leaves entryMap
        [Test]
        public void TestRemovedEntryNoLongerReactsToDefaults()
        {
            var obj = new HitCircle();
            var entry = new HitObjectLifetimeEntry(obj);
            manager.Add(entry, null);
            manager.Remove(entry);

            Assert.That(() => obj.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty()), Throws.Nothing);
            Assert.That(removed, Has.Count.EqualTo(1));
        }
    }
}
