using System.Collections.Generic;
using System.Linq;

namespace DropboxFS.FileCache
{
    public class CacheMap
    {
        private List<CachedFileChunk> _dictionary = new List<CachedFileChunk>();

        public bool IsCached(int offset, int count)
        {
            var testChunk = new Range<int>(offset, offset + count - 1);            
            return _dictionary.Any(chunk => chunk.Range.ContainsRange(testChunk));
        }

        public List<CachedFileChunk> GetMissingChunks(int offset, int count)
        {
            var testChunk = new Range<int>(offset, offset + count - 1);
            var result = new List<CachedFileChunk> {new CachedFileChunk(testChunk)};//c = max - min + 1
            var sortedChunks = _dictionary.Where(x => x.Range.OverlapsWith(testChunk)).OrderBy(x => x).ToList();
            if (sortedChunks.Count < 1)
                return result.ToList();
            result.Clear();
            var min = sortedChunks.Min(x => x.Range.Minimum);
            if(testChunk.Minimum < min)
                result.Add(new CachedFileChunk(new Range<int>
                    {
                        Minimum = testChunk.Minimum,
                        Maximum = min - 1
                    }));
            for (var i = 0; i < sortedChunks.Count - 1; i++)
            {
                // check if current chunk has gap with the next one
                if (sortedChunks[i + 1].Range.Minimum - sortedChunks[i].Range.Maximum > 1)
                {
                    result.Add(new CachedFileChunk(new Range<int> //define gap size
                        {
                            Minimum = sortedChunks[i].Range.Maximum + 1,
                            Maximum = sortedChunks[i+1].Range.Minimum - 1
                        }));
                }                
            }
            var max = sortedChunks.Max(x => x.Range.Maximum);
            if (testChunk.Maximum > max)
                result.Add(new CachedFileChunk(new Range<int>
                {
                    Minimum = max + 1,
                    Maximum = testChunk.Maximum
                }));
            return result.Where(x => x.Range.IsInsideRange(testChunk) || x.Range.OverlapsWith(testChunk)).ToList();
        }

        private bool AreAdjascent(Range<int> range1, Range<int> range2)
        {
            return range1.IsValid() && range2.IsValid() && (range1.Maximum == (range2.Minimum - 1) || range2.Maximum == (range1.Minimum - 1));
        }

        private void ConnectChunks()
        {
            var sortedChunks = _dictionary.OrderBy(x => x).ToList();
            for (var i = 0; i < sortedChunks.Count - 1;)
            {
                // check if current chunk has gap with the next one
                if (AreAdjascent(sortedChunks[i].Range, sortedChunks[i + 1].Range))
                {
                    //slap them together
                    sortedChunks[i + 1].Offset = sortedChunks[i].Offset;
                    sortedChunks[i + 1].Count += sortedChunks[i].Count;
                    sortedChunks.RemoveAt(i); //remove the old one
                    i = 0;
                    continue;
                } 
                if (sortedChunks[i].Range.OverlapsWith(sortedChunks[i + 1].Range))
                {
                    var maxMax = new List<int> {sortedChunks[i].Range.Maximum, sortedChunks[i+1].Range.Maximum}.Max();
                    var minMin = new List<int> {sortedChunks[i].Range.Minimum, sortedChunks[i+1].Range.Minimum}.Min();

                    sortedChunks[i + 1].Offset = minMin;
                    sortedChunks[i + 1].Count = maxMax - minMin + 1;
                    sortedChunks.RemoveAt(i);
                    i = 0;
                    continue;
                }
                i++;
            }
            lock (_dictionary)
            {
                _dictionary = sortedChunks;
            }
            return;
        }

        public void SetCached(int offset, int count)
        {
            lock (_dictionary)
            {
                _dictionary.Add(new CachedFileChunk(offset, count));                
            }
            ConnectChunks();
        }
    }
}