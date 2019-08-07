using System;
using System.Collections.Generic;
using System.Text;
using NLog;

namespace DatabaseVersioningTool.Application
{
    public class LogUpdatedEventArgs
    {
        public string Message { get; set; }
        public LogLevel Level { get; set; }
    }
}
