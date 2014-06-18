using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Dokan;
using NLog;

namespace DropboxFS
{
    [ExcludeFromCodeCoverage]
    public abstract class DokanDefaultImplementation : DokanOperations
    {
        protected readonly Logger Log;

        protected DokanDefaultImplementation(Logger log)
        {
            Log = log;
        }

        protected DokanDefaultImplementation()
        {
            Log = LogManager.GetCurrentClassLogger();
        }

        protected DokanDefaultImplementation(CreateFileDelegate onCreateFile = null, 
                                            OpenDirectoryDelegate onOpenDirectory = null, 
                                            CreateDirectoryDelegate onCreateDirectory = null, 
                                            CleanupDelegate onCleanup = null, 
                                            CloseFileDelegate onCloseFile = null, 
                                            ReadFileDelegate onReadFile = null, 
                                            WriteFileDelegate onWriteFile = null,
                                            FlushFileBuffersDelegate onFlushFileBuffers = null,
                                            GetFileInformationDelegate onGetFileInformation = null,
                                            FindFilesDelegate onFindFiles = null,
                                            SetFileAttributesDelegate onSetFileAttributes = null,
                                            SetFileTimeDelegate onSetFileTime = null,
                                            DeleteFileDelegate onDeleteFile = null,
                                            DeleteDirectoryDelegate onDeleteDirectory = null,
                                            MoveFileDelegate onMoveFile = null,
                                            SetEndOfFileDelegate onSetEndOfFile = null,
                                            SetAllocationSizeDelegate onSetAllocationSize = null,
                                            LockFileDelegate onLockFile = null,
                                            UnlockFileDelegate onUnlockFile = null,
                                            GetDiskFreeSpaceDelegate onGetDiskFreeSpace = null,
                                            UnmountDelegate onUnmount = null)
        {
            OnCreateFile = onCreateFile;
            OnOpenDirectory = onOpenDirectory;
            OnCreateDirectory = onCreateDirectory;
            OnCleanup = onCleanup;
            OnCloseFile = onCloseFile;
            OnReadFile = onReadFile;
            OnWriteFile = onWriteFile;
            OnFlushFileBuffers = onFlushFileBuffers;
            OnGetFileInformation = onGetFileInformation;
            OnFindFiles = onFindFiles;
            OnSetFileAttributes = onSetFileAttributes;
            OnSetFileTime = onSetFileTime;
            OnDeleteFile = onDeleteFile;
            OnDeleteDirectory = onDeleteDirectory;
            OnMoveFile = onMoveFile;
            OnSetEndOfFile = onSetEndOfFile;
            OnSetAllocationSize = onSetAllocationSize;
            OnLockFile = onLockFile;
            OnUnlockFile = onUnlockFile;
            OnGetDiskFreeSpace = onGetDiskFreeSpace;
            OnUnmount = onUnmount;
        }

        public CreateFileDelegate OnCreateFile { get; set; }
        public OpenDirectoryDelegate OnOpenDirectory { get; set; }
        public CreateDirectoryDelegate OnCreateDirectory { get; set; }
        public CleanupDelegate OnCleanup { get; set; }
        public CloseFileDelegate OnCloseFile { get; set; }
        public ReadFileDelegate OnReadFile { get; set; }
        public WriteFileDelegate OnWriteFile { get; set; }
        public FlushFileBuffersDelegate OnFlushFileBuffers { get; set; }
        public GetFileInformationDelegate OnGetFileInformation { get; set; }
        public FindFilesDelegate OnFindFiles { get; set; }
        public SetFileAttributesDelegate OnSetFileAttributes { get; set; }
        public SetFileTimeDelegate OnSetFileTime { get; set; }
        public DeleteFileDelegate OnDeleteFile { get; set; }
        public DeleteDirectoryDelegate OnDeleteDirectory { get; set; }
        public MoveFileDelegate OnMoveFile { get; set; }
        public SetEndOfFileDelegate OnSetEndOfFile { get; set; }
        public SetAllocationSizeDelegate OnSetAllocationSize { get; set; }
        public LockFileDelegate OnLockFile { get; set; }
        public UnlockFileDelegate OnUnlockFile { get; set; }
        public GetDiskFreeSpaceDelegate OnGetDiskFreeSpace { get; set; }
        public UnmountDelegate OnUnmount { get; set; }


        public int CreateFile(string filename, FileAccess access, FileShare share, FileMode mode, FileOptions options,
                              DokanFileInfo info)
        {
            var result = -1;
            Log.Trace("CreateFile {0}, {1}, {2}, {3}, {4}, {5}", filename, access, share, mode, options, info);
            try
            {
                if (OnCreateFile != null)
                    result = OnCreateFile(filename, access, share, mode, options, info);
            }
            catch (Exception ex)
            {
                Log.DebugException("CreateFile exception", ex);
                result = -DokanNet.ERROR_FILE_NOT_FOUND;
            }
            finally
            {
                Log.Trace("CreateFile result {0}", result);
            }
            return result;
        }

        public int OpenDirectory(string filename, DokanFileInfo info)
        {
            var result = -1;
            Log.Trace("OpenDirectory {0}, {1}", filename, info);
            try
            {
                if (OnOpenDirectory != null)
                    result = OnOpenDirectory(filename, info);
            }
            catch (Exception ex)
            {
                Log.DebugException("OpenDirectory exception", ex);
                result = -DokanNet.ERROR_PATH_NOT_FOUND;
            }
            finally
            {
                Log.Trace("OpenDirectory result {0}", result);
            }
            return result;
        }

        public int CreateDirectory(string filename, DokanFileInfo info)
        {
            var result = -1;
            Log.Trace("CreateDirectory {0}, {1}", filename, info);
            try
            {
                if (OnCreateDirectory != null)
                    result = OnCreateDirectory(filename, info);
            }
            catch (Exception ex)
            {
                Log.DebugException("CreateDirectory exception", ex);
                result = -DokanNet.ERROR_PATH_NOT_FOUND;
            }
            finally
            {
                Log.Trace("CreateDirectory result {0}", result);
            }
            return result;
        }

        public int Cleanup(string filename, DokanFileInfo info)
        {
            var result = -1;
            Log.Trace("Cleanup {0}, {1}", filename, info);
            try
            {
                if (OnCleanup != null)
                    result = OnCleanup(filename, info);
            }
            catch (Exception ex)
            {
                Log.DebugException("Cleanup exception", ex);
            }
            finally
            {
                Log.Trace("Cleanup result {0}", result);
            }
            return result;
        }

        public int CloseFile(string filename, DokanFileInfo info)
        {
            var result = -1;
            Log.Trace("CloseFile {0}, {1}", filename, info);
            try
            {
                if (OnCloseFile != null)
                    result = OnCloseFile(filename, info);
            }
            catch (Exception ex)
            {
                Log.DebugException("CloseFile exception", ex);
            }
            finally
            {
                Log.Trace("CloseFile result {0}", result);
            }
            return result;
        }

        public int ReadFile(string filename, byte[] buffer, ref uint readBytes, long offset, DokanFileInfo info)
        {
            var result = -1;
            Log.Trace("ReadFile {0}, {1}", filename, info);
            try
            {
                if (OnReadFile != null)
                    result = OnReadFile(filename, buffer, ref readBytes, offset, info);
            }
            catch (Exception ex)
            {
                Log.DebugException("ReadFile exception", ex);
            }
            finally
            {
                Log.Trace("ReadFile result {0}", result);
            }
            return result;
        }

        public int WriteFile(string filename, byte[] buffer, ref uint writtenBytes, long offset, DokanFileInfo info)
        {
            var result = -1;
            Log.Trace("WriteFile {0}, {1}", filename, info);
            try
            {
                if (OnWriteFile != null)
                    result = OnWriteFile(filename, buffer, ref writtenBytes, offset, info);
            }
            catch (Exception ex)
            {
                Log.DebugException("WriteFile exception", ex);
            }
            finally
            {
                Log.Trace("WriteFile result {0}", result);
            }
            return result;
        }

        public int FlushFileBuffers(string filename, DokanFileInfo info)
        {
            var result = -1;
            Log.Trace("FlushFileBuffers {0}, {1}", filename, info);
            try
            {
                if (OnFlushFileBuffers != null)
                    result = OnFlushFileBuffers(filename, info);
            }
            catch (Exception ex)
            {
                Log.DebugException("FlushFileBuffers exception", ex);
            }
            finally
            {
                Log.Trace("FlushFileBuffers result {0}", result);
            }
            return result;
        }

        public int GetFileInformation(string filename, FileInformation fileinfo, DokanFileInfo info)
        {
            var result = -1;
            Log.Trace("GetFileInformation {0}, {1}", filename, info);
            try
            {
                if (OnGetFileInformation != null)
                    result = OnGetFileInformation(filename, fileinfo, info);
            }
            catch (Exception ex)
            {
                Log.DebugException("GetFileInformation exception", ex);
            }
            finally
            {
                Log.Trace("GetFileInformation result {0}", result);
            }
            return result;
        }

        public int FindFiles(string filename, ArrayList files, DokanFileInfo info)
        {
            var result = -1;
            Log.Trace("FindFiles {0}, {1}", filename, info);
            try
            {
                if (OnFindFiles != null)
                    result = OnFindFiles(filename, files, info);
            }
            catch (Exception ex)
            {
                Log.DebugException("FindFiles exception", ex);
            }
            finally
            {
                Log.Trace("FindFiles result {0}", result);
            }
            return result;
        }

        public int SetFileAttributes(string filename, FileAttributes attr, DokanFileInfo info)
        {
            var result = -1;
            Log.Trace("SetFileAttributes {0}, {1}", filename, info);
            try
            {
                if (OnSetFileAttributes != null)
                    result = OnSetFileAttributes(filename, attr, info);
            }
            catch (Exception ex)
            {
                Log.DebugException("SetFileAttributes exception", ex);
            }
            finally
            {
                Log.Trace("SetFileAttributes result {0}", result);
            }
            return result;
        }

        public int SetFileTime(string filename, DateTime ctime, DateTime atime, DateTime mtime, DokanFileInfo info)
        {
            var result = -1;
            Log.Trace("SetFileTime {0}, {1}", filename, info);
            try
            {
                if (OnSetFileTime != null)
                    result = OnSetFileTime(filename, ctime, atime, mtime, info);
            }
            catch (Exception ex)
            {
                Log.DebugException("SetFileTime exception", ex);
            }
            finally
            {
                Log.Trace("SetFileTime result {0}", result);
            }
            return result;
        }

        public int DeleteFile(string filename, DokanFileInfo info)
        {
            var result = -1;
            Log.Trace("DeleteFile {0}, {1}", filename, info);
            try
            {
                if (OnDeleteFile != null)
                    result = OnDeleteFile(filename, info);
            }
            catch (Exception ex)
            {
                Log.DebugException("DeleteFile exception", ex);
            }
            finally
            {
                Log.Trace("DeleteFile result {0}", result);
            }
            return result;
        }

        public int DeleteDirectory(string filename, DokanFileInfo info)
        {
            var result = -1;
            Log.Trace("DeleteDirectory {0}, {1}", filename, info);
            try
            {
                if (OnDeleteDirectory != null)
                    result = OnDeleteDirectory(filename, info);
            }
            catch (Exception ex)
            {
                Log.DebugException("DeleteDirectory exception", ex);
            }
            finally
            {
                Log.Trace("DeleteDirectory result {0}", result);
            }
            return result;
        }

        public int MoveFile(string filename, string newname, bool replace, DokanFileInfo info)
        {
            var result = -1;
            Log.Trace("MoveFile {0}, {1}", filename, info);
            try
            {
                if (OnMoveFile != null)
                    result = OnMoveFile(filename, newname, replace, info);
            }
            catch (Exception ex)
            {
                Log.DebugException("MoveFile exception", ex);
            }
            finally
            {
                Log.Trace("MoveFile result {0}", result);
            }
            return result;
        }

        public int SetEndOfFile(string filename, long length, DokanFileInfo info)
        {
            var result = -1;
            Log.Trace("SetEndOfFile {0}, {1}", filename, info);
            try
            {
                if (OnSetEndOfFile != null)
                    result = OnSetEndOfFile(filename, length, info);
            }
            catch (Exception ex)
            {
                Log.DebugException("SetEndOfFile exception", ex);
            }
            finally
            {
                Log.Trace("SetEndOfFile result {0}", result);
            }
            return result;
        }

        public int SetAllocationSize(string filename, long length, DokanFileInfo info)
        {
            var result = -1;
            Log.Trace("SetAllocationSize {0}, {1}", filename, info);
            try
            {
                if (OnSetAllocationSize != null)
                    result = OnSetAllocationSize(filename, length, info);
            }
            catch (Exception ex)
            {
                Log.DebugException("SetAllocationSize exception", ex);
            }
            finally
            {
                Log.Trace("SetAllocationSize result {0}", result);
            }
            return result;
        }

        public int LockFile(string filename, long offset, long length, DokanFileInfo info)
        {
            var result = 0;
            Log.Trace("LockFile {0}, {1}", filename, info);
            try
            {
                if (OnLockFile != null)
                    result = OnLockFile(filename, offset, length, info);
            }
            catch (Exception ex)
            {
                Log.DebugException("LockFile exception", ex);
            }
            finally
            {
                Log.Trace("LockFile result {0}", result);
            }
            return result;
        }

        public int UnlockFile(string filename, long offset, long length, DokanFileInfo info)
        {
            var result = 0;
            Log.Trace("UnlockFile {0}, {1}", filename, info);
            try
            {
                if (OnUnlockFile != null)
                    result = OnUnlockFile(filename, offset, length, info);
            }
            catch (Exception ex)
            {
                Log.DebugException("UnlockFile exception", ex);
            }
            finally
            {
                Log.Trace("UnlockFile result {0}", result);
            }
            return result;
        }

        public int GetDiskFreeSpace(ref ulong freeBytesAvailable, ref ulong totalBytes, ref ulong totalFreeBytes, DokanFileInfo info)
        {
            var result = -1;
            Log.Trace("GetDiskFreeSpace {0}", info);
            try
            {
                if (OnGetDiskFreeSpace != null)
                    result = OnGetDiskFreeSpace(ref freeBytesAvailable, ref totalBytes, ref totalFreeBytes, info);
            }
            catch (Exception ex)
            {
                Log.DebugException("GetDiskFreeSpace exception", ex);
            }
            finally
            {
                Log.Trace("GetDiskFreeSpace result {0}", result);
            }
            return result;
        }

        public int Unmount(DokanFileInfo info)
        {
            var result = 0;
            Log.Trace("Unmount {0}", info);
            try
            {
                if (OnUnmount != null)
                    result = OnUnmount(info);
            }
            catch (Exception ex)
            {
                Log.DebugException("Unmount exception", ex);
            }
            finally
            {
                Log.Trace("Unmount result {0}", result);
            }
            return result;
        }
    }
}
