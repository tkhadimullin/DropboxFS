using System;
using System.Text;

namespace DropboxFS
{
    public class FileNameComponents
    {
        private string _path;
        public string Path
        {
            get { return string.IsNullOrWhiteSpace(_path)? "\\" : _path; }
            set { _path = value; }
        }

        public string Name { get; set; }
        public string FileName { get; set; }

        public string UnixFullFileName
        {
            get
            {                
                return UnixPath + "/" + Name; 
            }
        }

        public string UnixPath
        {
            get 
            {
                var components = Path.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                var sb = new StringBuilder();
                foreach (var component in components)
                {
                    sb.Append("/");
                    sb.Append(component);
                }
                return sb.ToString(); 
            }
        }

        public FileNameComponents(string dokanFilename)
        {
            FileName = dokanFilename;
            Name = System.IO.Path.GetFileName(FileName) ?? string.Empty;
            Path = System.IO.Path.GetDirectoryName(FileName) ?? string.Empty;
        }

    }
}
