using System.Collections;
using System.Collections.Generic;
using System.IO;
using Dokan;
using DropNet;
using DropNet.Models;
using DropboxFS.DropboxBackend;
using DropboxFS.HelperClasses;
using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DropboxFS.Test
{
    [TestClass]
    public class DropboxImplementationTests
    {
        private DropboxImplementation _impl;
        private DokanFileInfo _info;
        private string _clientPrefix;
        private readonly byte[] _buffer = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
        private DropNetClient _client;

        [TestInitialize]
        public void Init()
        {
            _client = new DropNetClient("", "", "", "");// all meaningful methods are shimmed, keys don't matter
            _clientPrefix = string.Format("{0} ({1})", "test", 123);
            using (ShimsContext.Create())
            {
                DropNet.Fakes.ShimDropNetClient.AllInstances.AccountInfo = netClient => new AccountInfo
                    {
                        display_name = "test",
                        uid = 123
                    };
                _impl = new DropboxImplementation(new List<DropNetClient> { _client });
                _info = new DokanFileInfo(1);
            }
        }

        [TestMethod]
        public void Impl_CreateWriteReadFile()
        {
            var backingFile = new MemoryStream();
            const string filename = "file.ext";
            var fileMetadata = new MetaData
            {
                Bytes = 0,
                Is_Dir = false,
                Path = "/" + filename
            };
            using (ShimsContext.Create())
            {
                Dokan.Fakes.ShimDokanNet.DokanResetTimeoutUInt32DokanFileInfo = (u, info) => true;
                DropNet.Fakes.ShimDropNetClient.AllInstances.GetMetaDataString = (client, s) => fileMetadata;
                DropNet.Fakes.ShimDropNetClient.AllInstances.StartChunkedUploadByteArray = (client, bytes) =>
                    {
                        backingFile.Seek(0, SeekOrigin.Begin);
                        backingFile.Write(bytes, 0, bytes.Length);
                        return new ChunkedUpload { UploadId = "1", Offset = bytes.Length };
                    };
                DropNet.Fakes.ShimDropNetClient.AllInstances.AppendChunkedUploadChunkedUploadByteArray = (client, upload, bytes) =>
                    {
                        backingFile.Seek(upload.Offset + 1, SeekOrigin.Begin);
                        backingFile.Write(bytes, 0, bytes.Length);
                        upload.Offset += bytes.Length;
                        return upload;
                    };
                DropNet.Fakes.ShimDropNetClient.AllInstances.CommitChunkedUploadChunkedUploadStringBoolean = (client, upload, path, overwrite) =>
                    {
                        fileMetadata.Bytes = backingFile.Length;
                        return fileMetadata;
                    };
                DropNet.Fakes.ShimDropNetClient.AllInstances.UploadFileStringStringByteArray = (client, s, arg3, arg4) => fileMetadata;
                DropNet.Fakes.ShimDropNetClient.AllInstances.GetFileStringInt64Int64String = (client, path, startByte, endByte, rev) =>
                    {
                        var buf = new byte[endByte - startByte];
                        backingFile.Seek(startByte, SeekOrigin.Begin);
                        backingFile.Read(buf, 0, (int)(endByte - startByte));
                        return buf;
                    };

                var name = _clientPrefix + "\\" + filename;
                uint writtenBytes = 0;
                // write
                var code = _impl.CreateFile(name, FileAccess.ReadWrite, FileShare.None, FileMode.OpenOrCreate, FileOptions.DeleteOnClose, _info);
                Assert.AreEqual(0, code);
                code = _impl.WriteFile(name, _buffer, ref writtenBytes, 0, _info);
                Assert.AreEqual(0, code);
                Assert.AreEqual(20, (int)writtenBytes);
                // close file
                code = _impl.CloseFile(name, _info);
                Assert.AreEqual(0, code);
                // read 
                uint readBytes = 0;
                var buffer = new byte[20];
                code = _impl.ReadFile(name, buffer, ref readBytes, 0, _info);
                Assert.AreEqual(0, code);
                Assert.AreEqual(20, (int)readBytes);
                Assert.AreEqual(_buffer.Length, buffer.Length);
                for (var i = 0; i < readBytes; i++)
                    Assert.AreEqual(_buffer[i], buffer[i]);
            }
        }

        [TestMethod]
        public void Impl_OpenDirectory()
        {
            var dirMetadata = new MetaData
            {
                Is_Dir = true,
                Path = "/TestDir",
            };
            using (ShimsContext.Create())
            {
                DropNet.Fakes.ShimDropNetClient.AllInstances.CreateFolderString = (client, s) => dirMetadata;
                DropNet.Fakes.ShimDropNetClient.AllInstances.DeleteString = (client, s) => new MetaData();
                DropNet.Fakes.ShimDropNetClient.AllInstances.GetMetaDataString = (client, s) => null; // as we're about to open a file we need metadata to be empty
                var dir = new DokanFileInfo(2);
                var name = _clientPrefix + "\\TestDir";
                var code = _impl.CreateDirectory(name, dir);
                Assert.AreEqual(0, code);
                DropNet.Fakes.ShimDropNetClient.AllInstances.GetMetaDataString = (client, s) => dirMetadata;// now we need proper metadata
                code = _impl.OpenDirectory(name, dir);
                var meta = dir.TryGetApplicationContext().MetaData;
                Assert.IsTrue(meta.Is_Dir);
                Assert.AreEqual("TestDir", meta.Name);
                Assert.AreEqual("/TestDir", meta.Path);
                Assert.AreEqual(0, code);
                code = _impl.DeleteDirectory(name, dir);
                Assert.AreEqual(0, code);
            }            
        }

        [TestMethod]
        public void Impl_GetDiskFreeSpace()
        {
            using (ShimsContext.Create())
            {
                DropNet.Fakes.ShimDropNetClient.AllInstances.AccountInfo = netClient => new AccountInfo
                {
                    display_name = "test",
                    uid = 123,
                    quota_info = new QuotaInfo
                        {
                            normal = 1, // space occupied by non-shared files
                            quota = 10, // total space available
                            shared = 0 // space occupied by shared files
                        }
                };
                ulong freeBytesAvailable = 0;
                ulong totalBytes = 0;
                ulong totalFreeBytes = 0;
                var code = _impl.GetDiskFreeSpace(ref freeBytesAvailable, ref totalBytes, ref totalFreeBytes, _info);
                Assert.AreEqual(0, code);
                Assert.AreEqual(10, (int)totalBytes);
                Assert.AreEqual(9, (int)totalFreeBytes); // we determine free space by formula: free = quota - normal - shared
                Assert.AreEqual(9, (int)freeBytesAvailable);
            }
        }

        [TestMethod]
        public void Impl_GetFileInformation()
        {
            using (ShimsContext.Create())
            {
                DropNet.Fakes.ShimDropNetClient.AllInstances.GetMetaDataString = (client, s) => new MetaData()
                    {
                        Is_Dir = false,
                        Path = "/file.ext",
                        Bytes = 100
                    };
                var f = new DokanFileInfo(3);
                var name = _clientPrefix + "\\file.ext";
                var fileinfo = new FileInformation();
                var code = _impl.GetFileInformation(name, fileinfo, f);
                Assert.AreEqual(0, code);
                Assert.AreEqual(FileAttributes.Normal, fileinfo.Attributes);
                Assert.AreEqual("file.ext", fileinfo.FileName);
                Assert.AreEqual(100, fileinfo.Length);                
            }
        }

        [TestMethod]
        public void Impl_FindFiles()
        {
            using (ShimsContext.Create())
            {
                // start with the root node. in our setup it holds account names
                var files = new ArrayList();
                var f = new DokanFileInfo(4);
                var code = _impl.FindFiles("\\", files, f);
                Assert.AreEqual(0, code);
                Assert.AreEqual(1, files.Count);
                Assert.AreEqual(_clientPrefix, (files[0] as FileInformation).FileName);
                // then try processing a non-root folder
                DropNet.Fakes.ShimDropNetClient.AllInstances.GetMetaDataString = (client, s) => new MetaData
                    {
                        Is_Dir = true,
                        Path = "/TestDir",
                        Contents = new List<MetaData>
                            {
                                new MetaData {Is_Dir = false, Path = "/TestDir/file1.dat", Bytes = 100},
                                new MetaData {Is_Dir = true, Path = "/TestDir/Subfolder"},
                                new MetaData {Is_Dir = false, Path = "/TestDir/file2.dat", Bytes = 200},
                            }
                    };
                // second call
                files.Clear();
                var name = _clientPrefix + "\\TestDir";
                f = new DokanFileInfo(5)
                    {
                        Context = new ApplicationContext(_client, new FileNameComponents("\\TestDir")) // for this operation we need a application context. Instead of relying on the underlying code to create it - we do it here
                    };
                code = _impl.FindFiles(name, files, f);
                Assert.AreEqual(0, code);
                Assert.AreEqual(3, files.Count);
                Assert.AreEqual("file1.dat", (files[0] as FileInformation).FileName);
                Assert.AreEqual(100, (files[0] as FileInformation).Length);
                Assert.AreEqual("Subfolder", (files[1] as FileInformation).FileName);
                Assert.AreEqual("file2.dat", (files[2] as FileInformation).FileName);
                Assert.AreEqual(200, (files[2] as FileInformation).Length);
            }
        }

        [TestCleanup]
        public void Clean()
        {
            _impl = null;
        }
    }
}
