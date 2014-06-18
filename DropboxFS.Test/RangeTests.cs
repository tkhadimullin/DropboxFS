using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DropboxFS.Test
{
    [TestClass]
    public class RangeTests
    {
        [TestMethod]
        public void Range_IsValid()
        {
            var a = new Range<int>(10,1);
            Assert.IsFalse(a.IsValid());
        }

        [TestMethod]
        public void Range_ContainsValue()
        {
            var a = new Range<int>(1, 10);
            Assert.IsTrue(a.ContainsValue(5));
        }

        [TestMethod]
        public void Range_ContainsRange()
        {
            var a = new Range<int>(1, 100);
            var b = new Range<int>(2, 99);
            var c = new Range<int>(0, 10);
            Assert.IsTrue(a.ContainsRange(b));
            Assert.IsFalse(a.ContainsRange(c));
        }

        [TestMethod]
        public void Range_OverlapsWith()
        {
            var a = new Range<int>(1, 100);
            var b = new Range<int>(2, 99);
            var c = new Range<int>(0, 10);
            var d = new Range<int>(90, 101);
            var e = new Range<int>(15, 16);
            Assert.IsTrue(a.OverlapsWith(b));
            Assert.IsTrue(a.OverlapsWith(c));
            Assert.IsTrue(a.OverlapsWith(d));
            Assert.IsTrue(e.OverlapsWith(a));
        }

        [TestMethod]
        public void Range_IsInsideRange()
        {
            var a = new Range<int>(1, 100);
            var b = new Range<int>(2, 99);
            var c = new Range<int>(0, 10);
            var d = new Range<int>(90, 101);
            var e = new Range<int>(0, 101);
            Assert.IsTrue(b.IsInsideRange(a));
            Assert.IsFalse(c.IsInsideRange(a));
            Assert.IsFalse(d.IsInsideRange(a));
            Assert.IsFalse(e.IsInsideRange(a));
        }
    }
}
