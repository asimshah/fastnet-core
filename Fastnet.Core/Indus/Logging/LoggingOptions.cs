using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fastnet.Core.Indus
{
    public class LoggingOptions : Options
    {
        public string LogFolder { get; set; }
        public LoggingOptions()
        {
            this.LogFolder = "default";
        }
    }
}
