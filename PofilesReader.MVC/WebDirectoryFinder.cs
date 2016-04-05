using PofilesReader.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PofilesReader.MVC
{
    public class WebDirectoryFinder : IPathsBuilder
    {
        private string _basePath;

        public WebDirectoryFinder(string basePath)
        {
            _basePath = basePath;
        }
        public string GetDirPath()
        {
            var dirPath = HttpContext.Current.Server.MapPath(string.Concat(_basePath, CultureInfo.CurrentUICulture));

            var parent = CultureInfo.CurrentUICulture.Parent;
            var dirExist = Directory.Exists(dirPath);
            while (parent != null && parent.Parent != parent && !dirExist)
            {
                dirPath = HttpContext.Current.Server.MapPath(string.Concat(_basePath, parent));
                dirExist = Directory.Exists(dirPath);
                parent = parent.Parent;
            }
            return dirExist ? dirPath : string.Empty;
        }
    }
}
