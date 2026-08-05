using System;
using System.IO;
using NUnit.Framework;
using Shift.Progression;

namespace Shift.Tests.EditMode
{
    public class RunRecordStoreTests
    {
        private string _directory;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "shift-records-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        private RunRecord Record(float total, params float[] splits)
        {
            return new RunRecord("zone01", total, splits, DateTime.UtcNow);
        }

        [Test]
        public void Submit_ThenGetBest_RoundTripsThroughDisk()
        {
            RunRecordStore writer = new RunRecordStore(_directory);
            Assert.IsTrue(writer.Submit(Record(42.5f, 10f, 20f)));

            // A fresh instance, so this reads the file rather than the in-memory list.
            RunRecordStore reader = new RunRecordStore(_directory);
            RunRecord best = reader.GetBest("zone01");

            Assert.IsNotNull(best);
            Assert.AreEqual(42.5f, best.TotalSeconds, 0.001f);
            Assert.AreEqual(20f, best.SplitAt(1), 0.001f);
        }

        [Test]
        public void Submit_SlowerRun_DoesNotOverwrite()
        {
            RunRecordStore store = new RunRecordStore(_directory);
            store.Submit(Record(30f));

            Assert.IsFalse(store.Submit(Record(45f)));
            Assert.AreEqual(30f, store.GetBest("zone01").TotalSeconds, 0.001f);
        }

        [Test]
        public void Submit_FasterRun_Overwrites()
        {
            RunRecordStore store = new RunRecordStore(_directory);
            store.Submit(Record(30f));

            Assert.IsTrue(store.Submit(Record(25f)));
            Assert.AreEqual(25f, store.GetBest("zone01").TotalSeconds, 0.001f);
        }

        [Test]
        public void GetBest_UnknownZone_IsNullNotAnException()
        {
            RunRecordStore store = new RunRecordStore(_directory);
            Assert.IsNull(store.GetBest("nope"));
        }

        [Test]
        public void MissingFile_YieldsNoRecords()
        {
            RunRecordStore store = new RunRecordStore(_directory);
            Assert.IsNull(store.GetBest("zone01"));
        }

        /// <summary>A damaged file must cost you your records, never your ability to start the game.</summary>
        [Test]
        public void CorruptFile_YieldsNoRecordsAndStillAcceptsSubmissions()
        {
            File.WriteAllText(Path.Combine(_directory, "records.json"), "{ this is not json");

            RunRecordStore store = new RunRecordStore(_directory);
            Assert.IsNull(store.GetBest("zone01"));
            Assert.IsTrue(store.Submit(Record(12f)));
            Assert.AreEqual(12f, store.GetBest("zone01").TotalSeconds, 0.001f);
        }

        [Test]
        public void Submit_KeepsZonesIndependent()
        {
            RunRecordStore store = new RunRecordStore(_directory);
            store.Submit(new RunRecord("zone01", 30f, new float[0], DateTime.UtcNow));
            store.Submit(new RunRecord("zone02", 90f, new float[0], DateTime.UtcNow));

            Assert.AreEqual(30f, store.GetBest("zone01").TotalSeconds, 0.001f);
            Assert.AreEqual(90f, store.GetBest("zone02").TotalSeconds, 0.001f);
        }
    }
}
