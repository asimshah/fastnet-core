using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Fastnet.Core
{
    public class MessageBase
    {
        public static int MaxMessageSize;
        public static int TransportBufferSize;
        //private static MessengerOptions options;
        private static JsonSerializerSettings jsonSettings;
        internal TcpClient receivedFrom;
        public DateTimeOffset dateTimeUtc { get; private set; }
        //private static ILogger<MessageBase> logger;
        static MessageBase()
        {
            //options = OptionsProvider.Get<MessengerOptions>();
            MaxMessageSize = 4096 * 64; //options.MaxMessageSize;
            TransportBufferSize = 4096 * 8;  //options.TransportBufferSize;
            jsonSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            };
        }
        public MessageBase()
        {
            
            this.dateTimeUtc = DateTime.UtcNow;
        }
        public virtual byte[] ToBytes()
        {
            var json = JsonConvert.SerializeObject(this, jsonSettings);
            return Encoding.UTF8.GetBytes(json);
        }
        public static MessageBase ToMessage(byte[] data, int length)
        {
            //Note: this works within a .net world because of TypeNameHandling.All
            // which adds some type information for JsonConvert
            // consequently I do not need generic version as in any case
            // it is not always the case that I know what type the message is at compile time
            var jsonString = Encoding.UTF8.GetString(data, 0, length);
            var m = JsonConvert.DeserializeObject<MessageBase>(jsonString, jsonSettings);
            return m;
        }
    }
}
