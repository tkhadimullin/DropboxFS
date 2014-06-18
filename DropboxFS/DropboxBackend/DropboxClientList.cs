using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DropNet;

namespace DropboxFS.DropboxBackend
{
    public class DropboxClientList
    {
        private readonly ConcurrentDictionary<string, DropNetClient> _clients = new ConcurrentDictionary<string, DropNetClient>();

        public DropboxClientList(IEnumerable<DropNetClient> clients)
        {
            foreach (var dropNetClient in clients)
            {
                var info = dropNetClient.AccountInfo();
                _clients[string.Format("{0} ({1})", info.display_name, info.uid)] = dropNetClient;
            }
        }

        public IEnumerable<DropNetClient> AllClients
        {
            get { return _clients.Values; }
        }

        public IEnumerable<string> AllKeys
        {
            get { return _clients.Keys; }
        }

        public DropNetClient ResolveByPath(string filename)
        {
            filename = filename.TrimStart('\\');
            DropNetClient res = null;
            try
            {
                res = _clients.First(x => filename.StartsWith(x.Key)).Value;
            }
            catch // we should handle the 'notfound' exceprions upstream
            {                
                throw;
            }
            return res;
        }

        public string ToLocalPath(string filename)
        {
            filename = filename.TrimStart('\\');
            var path = filename.Remove(0, _clients.First(x => filename.StartsWith(x.Key)).Key.Count());
            if (string.IsNullOrWhiteSpace(path))
                path = "\\";
            return path;
        }
    }
}
