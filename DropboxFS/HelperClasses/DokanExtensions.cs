using System;
using Dokan;
using DropNet;

namespace DropboxFS.HelperClasses
{
    public static class DokanExtensions
    {
        public static ApplicationContext TryGetApplicationContext(this DokanFileInfo info, DropNetClient client = null, string filename = null, string cachingKey = null)
        {
            var file = new FileNameComponents(filename);
            if (info.Context == null)
            {
                if (client == null) throw new ArgumentNullException("client");
                if (cachingKey == null) throw new ArgumentNullException("cachingKey");
                info.Context = new ApplicationContext(client, file) { GlobalFileName = cachingKey };                
            }            
            return info.Context as ApplicationContext;
        }        
    }
}
