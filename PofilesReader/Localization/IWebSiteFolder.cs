using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PofilesReader.Localization
{
    public interface IWebSiteFolder
    {
        string ReadFile(string filepath);
    }
}
