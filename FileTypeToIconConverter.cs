using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace VisualGuhCode
{
    public class FileTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            var item = value as FileSystemItem;

            if (item == null)
            {
                return "/Resources/unknownfile.png";
            }

            if (item.isFolder)
            {
                return "/Resources/folder_256.png";
            }

            if (item.Extension == ".txt")
            {
                return "/Resources/textfile.png";
            }

            return "/Resources/unknownfile.png";
        }

        public object ConvertBack(object value, Type targetInfo, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
