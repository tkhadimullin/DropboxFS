using System.Diagnostics.CodeAnalysis;
using DropNet;
using DropNet.Models;
using DropboxFS.DropboxBackend;

namespace DropboxFS
{
    [ExcludeFromCodeCoverage]
    public class ApplicationContext
    {
        private MetaData _metaData;

        public ApplicationContext(DropNetClient dropNetClient, FileNameComponents fileNameComponents)
        {
            DropNetClient = dropNetClient;
            FileNameComponents = fileNameComponents;
        }

        public DropNetClient DropNetClient { get; set; }
        
        public MetaData MetaData
        {
            get
            {
                if(_metaData != null) return _metaData;
                _metaData = GetMetaData();
                return _metaData;
            }
            set { _metaData = value; }
        }

        public MetaData GetMetaData()
        {
            MetaData result = null;
            try
            {
                result = DropNetClient.GetMetaData(FileNameComponents.UnixFullFileName);
                if (result.Is_Deleted)
                    result = null;
            }
            catch
            {}
            return result;
        }

        public FileNameComponents FileNameComponents { get; set; }
        public ChunkedUpload ChunkedUpload { get; set; }
        public IDropboxCachedFile Data { get; set; }
        public string GlobalFileName { get; set; } // used as caching key
    }
}
