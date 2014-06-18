using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DropboxFS.Test
{
    [TestClass]
    public class FileNameComponentsTests
    {
        private FileNameComponents _testFileExtension;
        private FileNameComponents _emptyFile;
        private FileNameComponents _multilevelFile;


        [TestInitialize]
        public void Init()
        {            
            _emptyFile = new FileNameComponents("\\");            
            _testFileExtension = new FileNameComponents("\\testFolder\\testFile.extension.ext");
            _multilevelFile = new FileNameComponents("\\testFolder\\level2\\level3\\testFile.extension.ext");
        }

        [TestMethod]
        public void FileNameComponents_Path()
        {
            Assert.AreEqual("\\", _emptyFile.Path);
            Assert.AreEqual("\\testFolder", _testFileExtension.Path);
            Assert.AreEqual("\\testFolder\\level2\\level3", _multilevelFile.Path);
        }

        [TestMethod]
        public void FileNameComponents_UnixPath()
        {
            Assert.AreEqual("/testFolder", _testFileExtension.UnixPath);
            Assert.AreEqual("/testFolder/level2/level3", _multilevelFile.UnixPath);
            Assert.AreEqual("", _emptyFile.UnixPath);// this one should probably be /
        }
    }
}
