using System;
using System.Threading;

namespace DropboxFS.FileCache
{
    public class CachedFileChunk : IComparable
    {
        public ManualResetEvent DoneEvent { get; set; }
        public int Offset { get; set; }
        public int Count { get; set; }
        public Range<int> Range
        {
            get { return new Range<int>(Offset, Offset + Count - 1); }
        }

        public CachedFileChunk()
        {
            DoneEvent = new ManualResetEvent(false);
        }

        public CachedFileChunk(int offset, int count) : this()
        {
            Offset = offset;
            Count = count;
        }

        public CachedFileChunk(Range<int> range)
            : this(range.Minimum, range.Maximum - range.Minimum + 1)
        {
        }

        public int CompareTo(object obj)
        {
            var otherChunk = obj as CachedFileChunk;
            if (otherChunk == null) throw new NotSupportedException();
            return Offset.CompareTo(otherChunk.Offset);
        }

        protected bool Equals(CachedFileChunk other)
        {
            return Offset == other.Offset && Count == other.Count;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CachedFileChunk) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Offset*397) ^ Count;
            }
        }

        public static bool operator ==(CachedFileChunk left, CachedFileChunk right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(CachedFileChunk left, CachedFileChunk right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            return string.Format("[{0}-{1}] ({2})", Offset, Offset + Count - 1, Count);
        }
    }
}
