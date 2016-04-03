using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PofilesReader.Localization
{
    public enum LogLevel
    {
        Debug,
        Information,
        Warning,
        Error,
        Fatal
    }
     
    public interface ILogger
    {
        bool IsEnabled(LogLevel level);
        void Log(LogLevel level, Exception exception, string format, params object[] args);
        void Debug(string v, string corePath);

    }

}
