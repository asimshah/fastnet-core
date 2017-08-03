using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Core
{
    public class MulticastOptions
    {
        public string MulticastIpAddress { get; set; }
        public int MulticastPort { get; set; }
        /// <summary>
        /// This is either a particular IP v4 address, e.g. 192.168.0.44, or, more likely,
        /// a range using a cidr format, e.g. 192.168.0.0/24. (first 24 bits are the subnet mask)
        /// </summary>
        public string LocalIpAddress { get; set; }
    }
}
