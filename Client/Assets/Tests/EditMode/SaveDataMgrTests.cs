using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace Feather.Tests
{
    public sealed class SaveDataMgrTests
    {
        private string file;
        private ES3Settings settings;

        [SetUp]
        public void SetUp()
        {
            file = Path.Combine(Path.GetTempPath(), "FeatherSaveTests-" + Guid.NewGuid().ToString("N") + ".es3");
            settings = new ES3Settings(false)
            {
                path = file,
                location = ES3.Location.File,
                encryptionType = ES3.EncryptionType.AES,
                encryptionPassword = "regression-test"
            };
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var path in new[] { file, file + ".tmp", file + ".bak" })
                if (File.Exists(path)) File.Delete(path);
        }

        private SaveDataMgr Open()
        {
            var save = new SaveDataMgr();
            save.Initialize(settings);
            return save;
        }

        [Test]
        public void FailedRead_BlocksWritesUntilOriginalFileIsRestored()
        {
            var original = Open();
            original.LoadData(0);
            original.SetData("coins", 7);
            byte[] backup = File.ReadAllBytes(file);
            ES3.Save("data_0", "wrong-type", settings);
            byte[] damaged = File.ReadAllBytes(file);
            var save = Open();
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex("Failed to load save slot"));
            save.LoadData(0);
            Assert.That(save.IsReadOnly, Is.True);
            Assert.Throws<InvalidOperationException>(() => save.SetData("coins", 0));
            Assert.Throws<InvalidOperationException>(() => save.DeleteTempDataTable(0));
            CollectionAssert.AreEqual(damaged, File.ReadAllBytes(file));
            File.WriteAllBytes(file, backup);
            Assert.That(save.RetryRead(), Is.True);
            Assert.That(save.GetData("coins", 0), Is.EqualTo(7));
        }

        [Test]
        public void Shutdown_FlushesDeferredDataAndRejectsFurtherWrites()
        {
            var save = Open();
            save.LoadData(0);
            save.SetData("coins", 9, false);
            save.SetSystemData("setting", 4, false);
            save.Shutdown();
            save.Shutdown();
            Assert.Throws<ObjectDisposedException>(() => save.SetData("coins", 10));
            var reopened = Open();
            reopened.LoadData(0);
            Assert.That(reopened.GetData("coins", 0), Is.EqualTo(9));
            Assert.That(reopened.GetSystemData("setting", 0), Is.EqualTo(4));
        }

        [Test]
        public void DeleteSlot_RemovesItsKeyAndPreservesOtherDataAfterReopen()
        {
            var save = Open();
            save.SetSystemData("setting", 7, true);
            save.LoadData(0);
            save.SetData("coins", 10);
            save.LoadData(1);
            save.SetData("coins", 20);
            save.DeleteTempDataTable(0);

            Assert.That(ES3.KeyExists("data_0", settings), Is.False);
            var reopened = Open();
            Assert.That(reopened.GetSystemData("setting", 0), Is.EqualTo(7));
            reopened.LoadData(1);
            Assert.That(reopened.GetData("coins", 0), Is.EqualTo(20));
            reopened.LoadData(0);
            Assert.That(reopened.GetData("coins", 0), Is.Zero);
        }

        [Test]
        public void DeleteSlot_RemovesAnOrphanedKeyMissingFromTheIndex()
        {
            var save = Open();
            save.LoadData(0);
            save.SetData("coins", 10);
            save.SetSystemData("ExistSlots", new HashSet<int>(), true);
            Open().DeleteTempDataTable(0);
            Assert.That(ES3.KeyExists("data_0", settings), Is.False);
        }

        [Test]
        public void DeleteAllSlots_PreservesSystemSettings()
        {
            var save = Open();
            save.SetSystemData("setting", 7, true);
            foreach (int slot in new[] { 0, 1 })
            {
                save.LoadData(slot);
                save.SetData("coins", 10);
            }
            save.DeleteAllTempDataTable();
            Assert.That(save.CurrentTempSlotId, Is.Null);
            Assert.That(ES3.KeyExists("data_0", settings), Is.False);
            Assert.That(ES3.KeyExists("data_1", settings), Is.False);
            Assert.That(Open().GetSystemData("setting", 0), Is.EqualTo(7));
        }

        [Test]
        public void FailedDeletion_PreservesActiveDirtySlotForRetry()
        {
            var save = Open();
            save.LoadData(0);
            save.SetData("coins", 10, false);
            settings.location = ES3.Location.Resources;
            Assert.Throws<NotSupportedException>(() => save.DeleteTempDataTable(0));
            Assert.That(save.CurrentTempSlotId, Is.EqualTo(0));
            Assert.That(save.GetData("coins", 0), Is.EqualTo(10));
            settings.location = ES3.Location.File;
            save.ApplyChangesToDatabase();
            var reopened = Open();
            reopened.LoadData(0);
            Assert.That(reopened.GetData("coins", 0), Is.EqualTo(10));
            save.DeleteTempDataTable(0);
            Assert.That(ES3.KeyExists("data_0", settings), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SameValueImmediateSave_FlushesEarlierDeferredChange(bool system)
        {
            var save = Open();
            save.LoadData(0);
            if (system)
            {
                save.SetSystemData("value", 42, false);
                save.SetSystemData("value", 42, true);
            }
            else
            {
                save.SetData("value", 42, false, 3);
                save.SetData("value", 42, true, 3);
            }
            var reopened = Open();
            reopened.LoadData(0);
            Assert.That(system ? reopened.GetSystemData("value", 0) : reopened.GetData("value", 0, 3), Is.EqualTo(42));
        }

        [Test]
        public void SameCleanValue_DoesNotAttemptAnotherWrite()
        {
            var save = Open();
            save.LoadData(0);
            save.SetData("value", 42);
            save.SetSystemData("value", 42, true);
            settings.location = ES3.Location.Resources;
            Assert.DoesNotThrow(() => save.SetData("value", 42));
            Assert.DoesNotThrow(() => save.SetSystemData("value", 42, true));
        }
    }
}
