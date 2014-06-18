using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dokan;
using DropNet;
using DropboxFS.HelperClasses;

namespace DropboxFS.DropboxBackend
{
    public class DropboxImplementation : DokanDefaultImplementation
    {
        private readonly DropboxClientList _clientList;

        public DropboxImplementation(IEnumerable<DropNetClient> clients)
        {
            _clientList = new DropboxClientList(clients);
            OnCreateFile = OnCreateFileDelegate;
            OnOpenDirectory = OpenDirectoryDelegate;
            OnCreateDirectory = CreateDirectoryDelegate;
            OnReadFile = ReadFileDelegate;
            OnWriteFile = WriteFileDelegate;
            OnFindFiles = FindFilesDelegate;
            OnDeleteFile = DeleteFileDelegate;
            OnDeleteDirectory = DeleteFileDelegate;
            OnMoveFile = MoveFileDelegate;
            OnGetDiskFreeSpace = GetDiskFreeSpaceDelegate;
            OnCloseFile = CloseFileDelegate;
            OnGetFileInformation = GetFileInformationDelegate;
        }

        public int OnCreateFileDelegate(string filename, FileAccess access, FileShare share, FileMode mode, FileOptions options, DokanFileInfo info)
        {
            if (filename == "\\")
            {
                info.IsDirectory = true;
                return 0;
            }
            DropNetClient client;
            try
            {
                client = _clientList.ResolveByPath(filename);
            }
            catch (Exception)
            {
                Log.Debug("CreateFile could not resolve name {0}", filename);
                return -DokanNet.ERROR_PATH_NOT_FOUND;
            }
            var globalFileName = filename;
            filename = _clientList.ToLocalPath(filename);
            var file = new FileNameComponents(filename);
            var appContext = info.TryGetApplicationContext(client, filename, globalFileName);
            var exists = false;
            if (appContext.MetaData != null)
            {
                info.IsDirectory = appContext.MetaData.Is_Dir;
                exists = true;
            }
            switch (mode)
            {
                case FileMode.Open:
                    {
                        if (exists) return 0;
                        return -DokanNet.ERROR_FILE_NOT_FOUND;
                    }
                case FileMode.CreateNew:
                    {
                        if (exists) return -DokanNet.ERROR_ALREADY_EXISTS;
                        appContext.MetaData = client.UploadFile(file.UnixPath, file.Name, new byte[] { });
                        return 0;
                    }
                case FileMode.Create:
                    {
                        appContext.MetaData = client.UploadFile(file.UnixPath, file.Name, new byte[] { });
                        return 0;
                    }
                case FileMode.OpenOrCreate:
                    {
                        if (!exists) appContext.MetaData = client.UploadFile(file.UnixPath, file.Name, new byte[] { });
                        return 0;
                    }
                case FileMode.Truncate:
                    {
                        if (!exists) return -DokanNet.ERROR_FILE_NOT_FOUND;
                        appContext.MetaData = client.UploadFile(file.UnixPath, file.Name, new byte[] { });
                        return 0;
                    }
                case FileMode.Append:
                    {

                        if (exists) return 0;
                        appContext.MetaData = client.UploadFile(file.UnixPath, file.Name, new byte[] { });
                        return 0;
                    }
                default:
                    return -1;
            }
        }

        public int OpenDirectoryDelegate(string filename, DokanFileInfo info)
        {
            if (filename == "\\")
            {
                info.IsDirectory = true;
                return 0;
            }
            var globalFileName = filename;
            var client = _clientList.ResolveByPath(filename);
            filename = _clientList.ToLocalPath(filename);
            var metaData = info.TryGetApplicationContext(client, filename, globalFileName).GetMetaData();
            if (metaData == null) return -DokanNet.ERROR_PATH_NOT_FOUND;
            info.IsDirectory = metaData.Is_Dir;

            if (info.IsDirectory)            
                return 0;            
            return -DokanNet.ERROR_PATH_NOT_FOUND; // TODO: return not directory?
        }

        public int CreateDirectoryDelegate(string filename, DokanFileInfo info)
        {
            if (filename == "\\")
            {
                info.IsDirectory = true;
                return -DokanNet.ERROR_ALREADY_EXISTS;
            }
            var client = _clientList.ResolveByPath(filename);
            var globalFileName = filename;
            filename = _clientList.ToLocalPath(filename);
            var file = new FileNameComponents(filename);
            var metaData = info.TryGetApplicationContext(client, filename, globalFileName).MetaData;
            if (metaData != null) return -DokanNet.ERROR_ALREADY_EXISTS;
            info.TryGetApplicationContext().MetaData = client.CreateFolder(file.UnixFullFileName);
            return 0;
        }

        public int ReadFileDelegate(string filename, byte[] buffer, ref uint readBytes, long offset, DokanFileInfo info)
        {
            var globalFileName = filename;
            var client = _clientList.ResolveByPath(filename);
            filename = _clientList.ToLocalPath(filename);
            var appContext = info.TryGetApplicationContext(client, filename, globalFileName);
            if (appContext.MetaData == null) return -DokanNet.ERROR_FILE_NOT_FOUND;
            info.IsDirectory = appContext.MetaData.Is_Dir;

            if (info.IsDirectory) return -1;
            if (offset > appContext.MetaData.Bytes) return -1;

            lock (appContext)
            {
                if (appContext.Data == null)
                {
                    if (string.IsNullOrWhiteSpace(appContext.GlobalFileName))
                        appContext.GlobalFileName = globalFileName;
                    appContext.Data = new DropboxCachedFile(info, Log);   
                }                        
            }                
            var buf = new byte[buffer.Length];
            readBytes = (uint)appContext.Data.Read(buf, (int)offset, buffer.Length);                
            Array.Copy(buf, buffer, readBytes);
            return 0;
        }

        public int WriteFileDelegate(string filename, byte[] buffer, ref uint writtenBytes, long offset, DokanFileInfo info)
        {
            var client = _clientList.ResolveByPath(filename);
            var globalFileName = filename;
            filename = _clientList.ToLocalPath(filename);
            var appContext = info.TryGetApplicationContext(client, filename, globalFileName);
            if (appContext == null || appContext.MetaData == null) return -DokanNet.ERROR_FILE_NOT_FOUND;
            info.IsDirectory = appContext.MetaData.Is_Dir;

            if (info.IsDirectory) return -1;
            DokanNet.DokanResetTimeout(60000, info);
            if (appContext.ChunkedUpload == null) // start chunked upload
            {
                appContext.ChunkedUpload = client.StartChunkedUpload(buffer);
                writtenBytes = (uint)buffer.Length;
            }
            else // continue chunked upload
            {
                appContext.ChunkedUpload = client.AppendChunkedUpload(appContext.ChunkedUpload, buffer);
                writtenBytes = (uint)buffer.Length;
            }
            return 0;
        }

        public int GetFileInformationDelegate(string filename, FileInformation fileinfo, DokanFileInfo info)
        {
            DropNetClient client;
            try
            {
                    
                client = _clientList.ResolveByPath(filename);
            }
            catch (Exception)
            {
                Log.Debug("GetFileInformation could not resolve name {0}", filename);
                return -DokanNet.ERROR_PATH_NOT_FOUND;
            }
            var globalFileName = filename;
            filename = _clientList.ToLocalPath(filename);
            Log.Trace("GetFileInformation {0}", filename);
            var metaData = info.TryGetApplicationContext(client, filename, globalFileName).MetaData;
            if (metaData == null) return -DokanNet.ERROR_PATH_NOT_FOUND;
            info.IsDirectory = metaData.Is_Dir;

            fileinfo.Attributes = info.IsDirectory ? FileAttributes.Directory : FileAttributes.Normal;
            fileinfo.LastAccessTime = DateTime.Now;
            fileinfo.LastWriteTime = DateTime.Now; //TODO: check if metaData.ModifiedDate is not 0 and use it
            fileinfo.CreationTime = DateTime.Now;//TODO: check if metaData.ModifiedDate is not 0 and use it
            fileinfo.Length = metaData.Bytes;
            fileinfo.FileName = metaData.Name;
            return 0;
        }

        public int FindFilesDelegate(string filename, ArrayList files, DokanFileInfo info)
        {
            if (filename == "\\")
            {
                info.IsDirectory = true;
                foreach (var fi in _clientList.AllKeys.Select(account => new FileInformation
                    {
                        Attributes = FileAttributes.Directory,
                        CreationTime = DateTime.Now,
                        FileName = account,
                        LastAccessTime = DateTime.Now,
                        LastWriteTime = DateTime.Now,
                        Length = 0
                    }))
                {
                    files.Add(fi);
                }
                return 0;
            }
            // after we've processed root we act as usual
            var client = _clientList.ResolveByPath(filename);
            filename = _clientList.ToLocalPath(filename);
            var metaData = info.TryGetApplicationContext(client, filename).MetaData;
            if (metaData == null) return -DokanNet.ERROR_PATH_NOT_FOUND;
            info.IsDirectory = metaData.Is_Dir;

            if (!info.IsDirectory) return -DokanNet.ERROR_PATH_NOT_FOUND;
            foreach (var fi in metaData.Contents.Select(item => new FileInformation
                {
                    Attributes = item.Is_Dir ? FileAttributes.Directory : FileAttributes.Normal,
                    CreationTime = item.ModifiedDate,
                    FileName = item.Name,
                    LastAccessTime = DateTime.Now,
                    LastWriteTime = item.ModifiedDate,
                    Length = item.Bytes
                }))
            {
                files.Add(fi);
            }
            return 0;
        }

        public int DeleteFileDelegate(string filename, DokanFileInfo info)
        {
            var client = _clientList.ResolveByPath(filename);
            filename = _clientList.ToLocalPath(filename);
            var file = new FileNameComponents(filename);
            if (info.TryGetApplicationContext(client, filename).MetaData == null) return -DokanNet.ERROR_FILE_NOT_FOUND;

            info.TryGetApplicationContext().MetaData = client.Delete(file.UnixFullFileName);
            return 0;
        }

        public int MoveFileDelegate(string filename, string newname, bool replace, DokanFileInfo info)
        {
            var client = _clientList.ResolveByPath(filename);
            var newclient = _clientList.ResolveByPath(newname);
            if (client != newclient)
            {
                Log.Error("Moving files between different accounts is not supported at the moment");
                return -1;
            }
            filename = _clientList.ToLocalPath(filename);
            newname = _clientList.ToLocalPath(newname);
            var oldFile = new FileNameComponents(filename);
            var newFile = new FileNameComponents(newname);
            var oldMetaData = info.TryGetApplicationContext(client, filename).MetaData;
            var newMetaData = client.GetMetaData(newFile.UnixFullFileName);
            if (oldMetaData == null) return -DokanNet.ERROR_PATH_NOT_FOUND;
            if (newMetaData != null && !replace) return -DokanNet.ERROR_ALREADY_EXISTS;

            info.TryGetApplicationContext().MetaData = client.Move(oldFile.UnixFullFileName, newFile.UnixFullFileName);
            return 0;
        }

        public int GetDiskFreeSpaceDelegate(ref ulong freeBytesAvailable, ref ulong totalBytes, ref ulong totalFreeBytes, DokanFileInfo info)
        {
            ulong totalBytesAllAccounts = 0;
            ulong totalFreeBytesAllAccounts = 0;
            foreach (var accountInfo in _clientList.AllClients.Select(client => client.AccountInfo()))
            {
                if (accountInfo == null) return -DokanNet.ERROR_ACCESS_DENIED;
                totalBytesAllAccounts += (ulong)accountInfo.quota_info.quota;
                totalFreeBytesAllAccounts += (ulong)(accountInfo.quota_info.quota - accountInfo.quota_info.normal - accountInfo.quota_info.shared);
            }
            totalBytes = totalBytesAllAccounts;
            totalFreeBytes = totalFreeBytesAllAccounts;
            freeBytesAvailable = totalFreeBytes;
            return 0;
        }

        public int CloseFileDelegate(string filename, DokanFileInfo info)
        {
            if (filename == "\\")
                return 0;
            var client = _clientList.ResolveByPath(filename);
            var globalFileName = filename;
            filename = _clientList.ToLocalPath(filename);
            var file = new FileNameComponents(filename);
            var appContext = info.TryGetApplicationContext(client, filename, globalFileName);
            if (appContext.MetaData == null) return -DokanNet.ERROR_FILE_NOT_FOUND;
            info.IsDirectory = appContext.MetaData.Is_Dir;
            if (info.IsDirectory) return -1;
            if (appContext.ChunkedUpload != null) // we were uploading chunks
            {
                appContext.MetaData = client.CommitChunkedUpload(appContext.ChunkedUpload, file.UnixFullFileName);
                appContext.ChunkedUpload = null;
            }
            return 0;
        }
    }
}
