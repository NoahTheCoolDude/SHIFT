using NUnit.Framework;
using Shift.Progression;

namespace Shift.Tests.EditMode
{
    public class RunClockTests
    {
        [Test]
        public void Advance_Accumulates()
        {
            RunClock clock = new RunClock();
            clock.Advance(0.5f);
            clock.Advance(0.25f);

            Assert.AreEqual(0.75f, clock.Elapsed, 0.0001f);
        }

        [Test]
        public void Advance_IgnoresNonPositiveDeltas()
        {
            RunClock clock = new RunClock();
            clock.Advance(1f);
            clock.Advance(-5f);
            clock.Advance(0f);

            Assert.AreEqual(1f, clock.Elapsed, 0.0001f);
        }

        [Test]
        public void Mark_RecordsCumulativeTimeNotSegment()
        {
            RunClock clock = new RunClock(2);

            clock.Advance(10f);
            clock.Mark(0);
            clock.Advance(5f);
            clock.Mark(1);

            Assert.AreEqual(10f, clock.Splits[0], 0.0001f);
            Assert.AreEqual(15f, clock.Splits[1], 0.0001f,
                "Splits are cumulative — segment times are derivable, comparisons are not.");
        }

        [Test]
        public void Mark_IsIdempotentPerIndex()
        {
            RunClock clock = new RunClock(1);

            clock.Advance(3f);
            clock.Mark(0);
            clock.Advance(7f);
            clock.Mark(0);

            Assert.AreEqual(3f, clock.Splits[0], 0.0001f,
                "Re-entering a checkpoint must not overwrite the first crossing.");
        }

        /// <summary>The guarantee that makes sequence breaks legal.</summary>
        [Test]
        public void Mark_OutOfOrder_BothLand_AndSkippedStaysUnreached()
        {
            RunClock clock = new RunClock(4);

            clock.Advance(8f);
            clock.Mark(3);
            clock.Advance(2f);
            clock.Mark(1);

            Assert.AreEqual(8f, clock.Splits[3], 0.0001f);
            Assert.AreEqual(10f, clock.Splits[1], 0.0001f);
            Assert.AreEqual(RunClock.Unreached, clock.Splits[0]);
            Assert.AreEqual(RunClock.Unreached, clock.Splits[2]);
            Assert.IsFalse(clock.WasReached(2));
        }

        [Test]
        public void Mark_BeyondSplitCount_GrowsTheList()
        {
            RunClock clock = new RunClock(1);
            clock.Advance(4f);
            clock.Mark(3);

            Assert.AreEqual(4, clock.Splits.Count);
            Assert.AreEqual(4f, clock.Splits[3], 0.0001f);
        }

        [Test]
        public void Reset_ClearsElapsedAndSplitsButKeepsShape()
        {
            RunClock clock = new RunClock(3);
            clock.Advance(5f);
            clock.Mark(0);

            clock.Reset();

            Assert.AreEqual(0f, clock.Elapsed);
            Assert.AreEqual(3, clock.Splits.Count);
            Assert.AreEqual(RunClock.Unreached, clock.Splits[0]);
        }
    }
}
