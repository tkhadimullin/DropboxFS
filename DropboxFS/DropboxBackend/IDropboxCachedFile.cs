using System.IO;

namespace DropboxFS.DropboxBackend
{
    public interface IDropboxCachedFile
    {
        /// <summary>
        /// Reads buffer from backing stream
        /// </summary>
        /// <param name="buffer">Buffer that will hold the read data</param>
        /// <param name="offset">Offset in a backing stream to start reading from</param>
        /// <param name="count">Maximum bytes to be read from stream</param>
        /// <returns>Number of bytes actually read</returns>
        int Read(byte[] buffer, int offset, int count);

        long Seek(long offset, SeekOrigin origin);
        void Write(byte[] buffer, int offset, int count);
    }
}