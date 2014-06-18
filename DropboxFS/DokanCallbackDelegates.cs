using System;
using System.Collections;
using System.IO;
using Dokan;

namespace DropboxFS
{
    public delegate int CreateFileDelegate(string filename, FileAccess access, FileShare share, FileMode mode, FileOptions options, DokanFileInfo info);
    public delegate int OpenDirectoryDelegate(string filename, DokanFileInfo info);
    public delegate int CreateDirectoryDelegate(string filename, DokanFileInfo info);
    public delegate int CleanupDelegate(string filename, DokanFileInfo info);
    public delegate int CloseFileDelegate(string filename, DokanFileInfo info);
    public delegate int ReadFileDelegate(string filename, byte[] buffer, ref uint readBytes, long offset, DokanFileInfo info);
    public delegate int WriteFileDelegate(string filename, byte[] buffer, ref uint writtenBytes, long offset, DokanFileInfo info);
    public delegate int FlushFileBuffersDelegate(string filename, DokanFileInfo info);
    public delegate int GetFileInformationDelegate(string filename, FileInformation fileinfo, DokanFileInfo info);
    public delegate int FindFilesDelegate(string filename, ArrayList files, DokanFileInfo info);
    public delegate int SetFileAttributesDelegate(string filename, FileAttributes attr, DokanFileInfo info);
    public delegate int SetFileTimeDelegate(string filename, DateTime ctime, DateTime atime, DateTime mtime, DokanFileInfo info);
    public delegate int DeleteFileDelegate(string filename, DokanFileInfo info);
    public delegate int DeleteDirectoryDelegate(string filename, DokanFileInfo info);
    public delegate int MoveFileDelegate(string filename, string newname, bool replace, DokanFileInfo info);
    public delegate int SetEndOfFileDelegate(string filename, long length, DokanFileInfo info);
    public delegate int SetAllocationSizeDelegate(string filename, long length, DokanFileInfo info);
    public delegate int LockFileDelegate(string filename, long offset, long length, DokanFileInfo info);
    public delegate int UnlockFileDelegate(string filename, long offset, long length, DokanFileInfo info);
    public delegate int GetDiskFreeSpaceDelegate(ref ulong freeBytesAvailable, ref ulong totalBytes, ref ulong totalFreeBytes, DokanFileInfo info);
    public delegate int UnmountDelegate(DokanFileInfo info);
}