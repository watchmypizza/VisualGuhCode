using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Media3D;

namespace VisualGuhCode
{
    public class FileTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            var item = value as FileSystemItem;
            var resourcesPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
            var charstoremove = new string[] { "file.", ".png" };
            var fileNames = new List<string>();

            if (item == null)
            {
                return $"{resourcesPath}/unknownfile.png";
            }

            if (item.isFolder)
            {
                return $"{resourcesPath}/folder_256.png";
            }

            if (!Directory.Exists(resourcesPath))
            {
                System.Diagnostics.Debug.WriteLine($"Resources directory missing: {resourcesPath}");
                throw new Exception("Well your Resources path is gone.");
            }

            foreach (var filePath in Directory.GetFiles(resourcesPath))
            {
                string fileName = Path.GetFileName(filePath);
                string replacedFileName = fileName;

                foreach (var c in charstoremove)
                {
                    replacedFileName = replacedFileName.Replace(c, string.Empty);
                }

                var ext = (item.Extension ?? "").TrimStart('.').ToLowerInvariant();
                //System.Diagnostics.Debug.WriteLine(ext);
                var cleanName = replacedFileName.ToLowerInvariant();
                //System.Diagnostics.Debug.WriteLine(cleanName);

                if (ext == cleanName)
                {
                    System.Diagnostics.Debug.WriteLine(filePath);
                    return Path.GetFullPath(filePath);
                }
            }

            return $"{resourcesPath}/unknownfile.png";
        }

        public object ConvertBack(object value, Type targetInfo, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
