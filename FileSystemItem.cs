using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualGuhCode
{
    public class FileSystemItem
    {
        public string Name { get; set; }
        public bool isFolder { get; set; }
        public bool IsLoaded { get; set; }
        public string Extension { get; set; }

        public string FullPath { get; set; }

        public List<FileSystemItem> SubItems { get; set; } = new List<FileSystemItem>();

        public List<FileSystemItem> result { get; set; } = new List<FileSystemItem>();
    }
}
