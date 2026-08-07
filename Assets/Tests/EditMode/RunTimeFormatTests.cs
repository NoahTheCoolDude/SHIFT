using NUnit.Framework;
using Shift.Progression;

namespace Shift.Tests.EditMode
{
    public class RunTimeFormatTests
    {
        [Test]
        public void Format_Zero()
        {
            Assert.AreEqual("0:00.00", RunTimeFormat.Format(0f));
        }

        [Test]
        public void Format_UsesMinutesAndHundredths()
        {
            Assert.AreEqual("1:23.45", RunTimeFormat.Format(83.45f));
        }

        [Test]
        public void Format_PadsSecondsBelowTen()
        {
            Assert.AreEqual("2:05.00", RunTimeFormat.Format(125f));
        }

        [Test]
        public void Format_UnreachedRendersAsDash()
        {
            Assert.AreEqual(RunTimeFormat.Unreached, RunTimeFormat.Format(RunClock.Unreached));
        }

        [Test]
        public void Delta_SignsAheadAndBehind()
        {
            Assert.AreEqual("-1.50", RunTimeFormat.Delta(10f, 11.5f));
            Assert.AreEqual("+2.25", RunTimeFormat.Delta(12.25f, 10f));
        }

        [Test]
        public void Delta_IsEmptyWhenEitherSideIsUnreached()
        {
            Assert.IsEmpty(RunTimeFormat.Delta(RunClock.Unreached, 10f));
            Assert.IsEmpty(RunTimeFormat.Delta(10f, RunClock.Unreached));
        }
    }
}
