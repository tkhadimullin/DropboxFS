using System;
using System.IO;
using System.Threading;
using Dokan;
using DropNet;
using DropNet.Models;
using DropboxFS.FileCache;
using DropboxFS.HelperClasses;
using NLog;

namespace DropboxFS.DropboxBackend
{
    public class DropboxCachedFile : Stream, IDropboxCachedFile
    {
        public  int PreloadWindow = 500*1024; // we'll preload up to 500kB
        private readonly CacheMap _cacheMap;
        private readonly Stream _innerStream;
        private readonly DropNetClient _client;
        private readonly MetaData _metaData;
        private readonly Logger _log;
        private long _readPosition;
        private long _writePosition;
        private readonly FileNameComponents _file;
        private readonly DokanFileInfo _dokanInfo;

        public DropboxCachedFile(DokanFileInfo dokanInfo, Logger log = null)
        {
            var appContext = dokanInfo.TryGetApplicationContext();
            
            _dokanInfo = dokanInfo;
            _client = appContext.DropNetClient;
            _metaData = appContext.MetaData;
            if (log != null)
                _log = log;
            else
            {
                _log = LogManager.CreateNullLogger();
                LogManager.DisableLogging();
            }            
            _file = appContext.FileNameComponents;
            
            var path = string.Format(@"temp\{0}", _dokanInfo.DokanContext );
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            _innerStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 512, FileOptions.DeleteOnClose|FileOptions.RandomAccess);
            _innerStream.SetLength(_metaData.Bytes);
            _log.Trace("File length is {0}", _metaData.Bytes);
            _cacheMap = new CacheMap();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Flush()
        {
            lock (_innerStream)
            {
                _innerStream.Flush();
            }
        }

        public override long Length
        {
            get
            {
                lock (_innerStream)
                {
                    return _innerStream.Length;
                }
            }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Reads buffer from backing stream
        /// </summary>
        /// <param name="buffer">Buffer that will hold </param>
        /// <param name="offset">Offset in a backing stream to start reading from</param>
        /// <param name="count">Maximum bytes to be read from stream</param>
        /// <returns>Number of bytes actually read</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            // make sure we're not reading beyond the end of file
            if (offset > Length)
            {
                _log.Trace("Trying to read beyond the end of file.");
                return 0;
            }
            if (offset <= Length && (offset + count - 1) > Length)
            {
                count = (int)Length - offset;
                _log.Trace("This chunk is longer than our file stream. Trimed it down to {0} ({1}).", offset, count);
            }

            if (!_cacheMap.IsCached(offset, count))
            {
                _log.Trace(String.Format("Chunk {0} ({1}) is not cached", offset, count));
                var chunkList = _cacheMap.GetMissingChunks(offset, count);
                _log.Trace(String.Format("Will download {0} chunks", chunkList.Count));
                foreach (var chunk in chunkList)
                {
                    _log.Trace("Chunk #{0}", chunk);                    
                }
                var doneEvents = new ManualResetEvent[chunkList.Count];
                for (var i = 0; i < chunkList.Count; i++)
                {
                    var cachedFileChunk = chunkList[i];
                    doneEvents[i] = cachedFileChunk.DoneEvent;
                    ThreadPool.QueueUserWorkItem(DownloadChunk, cachedFileChunk);
                }
                do
                {
                    DokanNet.DokanResetTimeout(15000, _dokanInfo); // keep extending our time so OS will know we're still alive
                } while (!WaitHandle.WaitAll(doneEvents, 5000));
            }else
            {
                _log.Trace(String.Format("Chunk {0} ({1}) is cached, reading it", offset, count));
            }
            lock (_innerStream)
            {
                _innerStream.Position = _readPosition;
                _innerStream.Seek(offset, SeekOrigin.Begin);
                var read = _innerStream.Read(buffer, 0, count);
                _readPosition = _innerStream.Position;
                return read;                
            }
        }

        private void DownloadChunk(object state)
        {
            var chunk = state as CachedFileChunk;
            if (chunk == null) return;
            var readBegin = chunk.Range.Minimum;
            var readEnd = chunk.Range.Maximum;
            if (chunk.Count < PreloadWindow)
                readEnd = readBegin + PreloadWindow;
            if (readEnd > _metaData.Bytes)
                readEnd = (int)_metaData.Bytes;
            var buf = _client.GetFile(_file.UnixFullFileName, readBegin, readEnd, string.Empty);
            Write(buf, chunk.Offset, buf.Length);
            chunk.DoneEvent.Set();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            _log.Trace("Seek on CachedFileThread. Exception!");
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            _log.Trace("SetLength on CachedFileThread. Exception!");
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (_innerStream)
            {
                _cacheMap.SetCached(offset, count);
                _innerStream.Position = _writePosition;
                _innerStream.Seek(offset, SeekOrigin.Begin);
                _innerStream.Write(buffer, 0, count);
                _writePosition = _innerStream.Position;
            }
        }

        public void Cleanup()
        {
            _log.Trace("Cleanup on CachedFileThread. Exception!");
            _innerStream.Close();
        }
    }
}