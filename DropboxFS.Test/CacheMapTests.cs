using System.Collections.Generic;
using System.Linq;
using DropboxFS.FileCache;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DropboxFS.Test
{
    [TestClass]
    public class CacheMapTests
    {
        [TestMethod]
        public void CachedFileChunk_Equality()
        {
            var a = new CachedFileChunk(0, 10);// this wuld produce a range of 0-9
            var b = new CachedFileChunk(new Range<int>(0, 9));// this is a range. 0-9
            var c = new CachedFileChunk();
            Assert.AreEqual(a, b);
            Assert.AreNotEqual(a, c);
            c.Offset = 0;
            c.Count = 10;
            Assert.AreEqual(b, c);
        }

        [TestMethod]
        public void CachedFileChunk_Ordering()
        {
            var a = new CachedFileChunk(0, 10);
            var b = new CachedFileChunk(11, 10);
            var c = new CachedFileChunk(22, 10);
         
            var list = new List<CachedFileChunk> { c, b, a };
            var sortedList = list.OrderBy(x => x).ToList();

            Assert.AreEqual(a, sortedList[0]);
            Assert.AreEqual(b, sortedList[1]);
            Assert.AreEqual(c, sortedList[2]);
        }

        [TestMethod]
        public void CacheMap_IsCached()
        {
            var cm = new CacheMap();
            cm.SetCached(0, 10);
            Assert.IsTrue(cm.IsCached(0,10));
            Assert.IsTrue(cm.IsCached(2, 5));
        }

        [TestMethod]
        public void CacheMap_GetMisingChunks()
        {
            var cm = new CacheMap();
            cm.SetCached(5, 4); // overlaps with the second one
            cm.SetCached(1, 9);// overlaps with previous one
            cm.SetCached(20, 10);// has a gap before
            cm.SetCached(15, 1); // is adjascent to the next one
            cm.SetCached(16, 1); // is adjascent to the next one
            var missing = cm.GetMissingChunks(0, 31);
            Assert.AreEqual(4, missing.Count);
            Assert.AreEqual(new CachedFileChunk(new Range<int>(0, 0)), missing[0]);
            Assert.AreEqual(new CachedFileChunk(new Range<int>(10, 14)), missing[1]);
            Assert.AreEqual(new CachedFileChunk(new Range<int>(17, 19)), missing[2]);
            Assert.AreEqual(new CachedFileChunk(new Range<int>(30, 30)), missing[3]);
        }

        [TestMethod]
        public void CacheMap_GetOneMisingChunk()
        {
            var cm = new CacheMap();
            cm.SetCached(0, 32);
            var missing = cm.GetMissingChunks(33, 10);
            Assert.AreEqual(1, missing.Count);
            Assert.AreEqual(new CachedFileChunk(33, 10), missing[0]);
        }
    }
}
