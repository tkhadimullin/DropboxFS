using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dokan;
using DropNet;
using DropNet.Models;
using DropboxFS.DropboxBackend;
using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace DropboxFS.Test
{
    [TestClass]
    public class DropboxBackendTests
    {
        [TestMethod]
        public void DropboxClientList_ResolveByPath()
        {
            using (ShimsContext.Create())
            {
                var dbClientList = new List<DropNetClient>
                    {
                        new DropNetClient("", "", "1", "client1"),
                        new DropNetClient("", "", "2", "client2"),
                        new DropNetClient("", "", "3", "client3"),
                        new DropNetClient("", "", "4", "client4"),
                    };

                DropNet.Fakes.ShimDropNetClient.AllInstances.AccountInfo = client => new AccountInfo { display_name = client.UserLogin.Secret, uid = long.Parse(client.UserLogin.Token) }; // using UserLogin Token and Secret just to differentiate between instances easily
                var d = new DropboxClientList(dbClientList);
                var cl1 = d.ResolveByPath(@"\\client1 (1)\root\path\filename.ext"); // first client
                var cl3 = d.ResolveByPath(@"\\client3 (3)\root\path\filename.ext"); // third client
                try
                {
                    var clNotFound = d.ResolveByPath(@"\\client5 (5)\root\path\filename.ext"); // not found client
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(InvalidOperationException)); // we should catch 'key not in list' exception
                }                
                Assert.AreEqual(dbClientList[0], cl1);
                Assert.AreEqual(dbClientList[2], cl3);
            }
        }

        [TestMethod]
        public void DropboxClientList_ToLocalPath()
        {
            using (ShimsContext.Create())
            {
                var dbClientList = new List<DropNetClient>
                    {
                        new DropNetClient("", "", "1", "client1")                        
                    };

                DropNet.Fakes.ShimDropNetClient.AllInstances.AccountInfo = client => new AccountInfo { display_name = client.UserLogin.Secret, uid = long.Parse(client.UserLogin.Token) }; // using UserLogin Token and Secret just to differentiate between instances easily
                var d = new DropboxClientList(dbClientList);
                var cl1 = d.ToLocalPath(@"\\client1 (1)\root\path\filename.ext"); // first client                
                Assert.AreEqual(@"\root\path\filename.ext", cl1);
            }
        }

        [TestMethod]
        public void DropboxCachedFile_Read()
        {

            var fileData = new MemoryStream(new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20});
            var client = new DropNetClient("", "", "", "");
            var filename = new FileNameComponents(@"\\test\test.ext");
            var metadata = new MetaData
                {
                    Bytes = fileData.Length,
                    Is_Dir = false,
                };
            var dokanFileInfo = new DokanFileInfo(1)
                {
                    Context = new ApplicationContext(client, filename)
                        {
                            MetaData = metadata                            
                        },
                    IsDirectory = false,

                };
            using (ShimsContext.Create())
            {
                Dokan.Fakes.ShimDokanNet.DokanResetTimeoutUInt32DokanFileInfo = (u, info) => true;
                DropNet.Fakes.ShimDropNetClient.AllInstances.GetFileStringInt64Int64String =
                    (netClient, path, startByte, endByte, rev) =>
                        {
                            fileData.Seek(startByte, SeekOrigin.Begin);
                            var buf = new byte[endByte - startByte + 1];
                            fileData.Read(buf, 0, (int) (endByte - startByte + 1));
                            return buf;
                        };
                var cf = new DropboxCachedFile(dokanFileInfo)
                    {
                        PreloadWindow = 5 // we will set a preload window to a small value so we could see how cache kicks in
                    };
                var res = new byte[fileData.Length];
                var read = cf.Read(res, 22, 1); // will return 0
                Assert.AreEqual(0, read);
                
                read = cf.Read(res, 15, 15); // will return only last 5 bytes
                Assert.AreEqual(5, read);
                for (var i = 0; i < read; i++)
                    Assert.AreEqual(fileData.ToArray()[15+i], res[i]);//we've requested buffer from 15th byte. so we're comparing with reference from this offset
                Array.Clear(res, 0, res.Length);

                read = cf.Read(res, 0, 10);
                Assert.AreEqual(10, read);
                for (var i = 0; i < read; i++)
                    Assert.AreEqual(fileData.ToArray()[i], res[i]);
                Array.Clear(res, 0, res.Length);

                read = cf.Read(res, 0, 2); // this one will be served from cache
                Assert.AreEqual(2, read);
                for (var i = 0; i < read; i++)
                    Assert.AreEqual(fileData.ToArray()[i], res[i]);
                Array.Clear(res, 0, res.Length);

                read = cf.Read(res, 2, 3); // so as this one
                Assert.AreEqual(3, read);
                for (var i = 0; i < read; i++)
                    Assert.AreEqual(fileData.ToArray()[2 + i], res[i]);
                Array.Clear(res, 0, res.Length);

                read = cf.Read(res, 4, 2);
                Assert.AreEqual(2, read);
                for (var i = 0; i < read; i++)
                    Assert.AreEqual(fileData.ToArray()[4 + i], res[i]);
                Array.Clear(res, 0, res.Length);

                read = cf.Read(res, 10, 2);
                Assert.AreEqual(2, read);
                for (var i = 0; i < read; i++)
                    Assert.AreEqual(fileData.ToArray()[10 + i], res[i]);
                Array.Clear(res, 0, res.Length);

                read = cf.Read(res, 0, 20);
                Assert.AreEqual(20, read);
                for (var i = 0; i < read; i++)
                    Assert.AreEqual(fileData.ToArray()[i], res[i]);
            }            
        }
    }
}
