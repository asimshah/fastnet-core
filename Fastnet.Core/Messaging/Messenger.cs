using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Fastnet.Core
{
    public class MessengerOptions //: Options
    {
        [Obsolete]
        public bool TraceMessages { get; set; }
        public bool TraceSerialization { get; set; }
        public int MaxMessageSize { get; set; }
        public int TransportBufferSize { get; set; }
        public MessengerOptions()
        {
            MaxMessageSize = 4096 * 64;
            TransportBufferSize = 4096 * 8;
        }
    }
    /// <summary>
    /// Use this class to Start and Stop a message listener in a server and
    /// to Send (and receive replies) to such a listening server.
    /// Note that this class uses a packet protocol to allow .net objects (derived from MessageBase) to be
    /// sent and received over tcp/ip.
    /// </summary>
    public class Messenger
    {
        private static MulticastSender mcastSender;
        private static MulticastListener mcastListener;
        private static SocketListener listener;
        private MessengerOptions options;
        private ILogger log;
        private ILoggerFactory loggerFactory;
        //static Messenger()
        //{
        //    options = null;

        //}
        public Messenger(ILogger<Messenger> logger, IOptions<MessengerOptions> options, ILoggerFactory loggerFactory)
        {
            this.log = logger;
            this.options = options.Value;
            this.loggerFactory = loggerFactory;
        }
        /// <summary>
        /// Start a (tcp point-to-point) listener on the given address and port. Only one instance of a listener is supported
        /// </summary>
        /// <param name="address">an address such as 127.0.0.1</param>
        /// <param name="port">some suitable port no, such as 5858</param>
        /// <param name="onReceive">The method to call for each MessageBase derived message that arrives</param>
        /// <returns></returns>
        public async Task StartListener(string address, int port, Action<MessageBase> onReceive)
        {
            //Messenger.options = options;
            IPAddress ip;
            if(IPAddress.TryParse(address, out ip))
            {
                await StartListener(ip, port, onReceive);
            }
        }
        /// <summary>
        /// Stop the current listener
        /// </summary>
        public void StopListener()            
        {
            listener?.Stop();
            listener = null;
        }
        /// <summary>
        /// Connect to a server listening on the given address and port. Use this at the client end
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <returns>The TcpClient that can be used for the SendAsync and Receive Async methods</returns>
        public TcpClient Connect(string address, int port)
        {
            //Messenger.options = options;
            var client =  new TcpClient(address, port);
            client.NoDelay = true;
            return client;
        }
        /// <summary>
        /// Register a method to receive MessageBase derived messages on the give <see cref="System.Net.Sockets.TcpClient"/>
        /// </summary>
        /// <remarks>
        /// This is a method intended for use in a client to receive responses from the server.
        /// </remarks>
        /// <param name="client">A client obtained by calling <see cref="Connect"/></param>
        /// <param name="token">A cancellation token (required)</param>
        /// <param name="onMessageReceive">The method to call for each message received</param>
        /// <returns></returns>
        public async Task ReceiveAsync(TcpClient client, CancellationToken token, Action<MessageBase> onMessageReceive)
        {
            using (client)
            {
                var buffer = new byte[MessageBase.MaxMessageSize];
                var stream = client.GetStream();
                PacketProtocol pp = new PacketProtocol(MessageBase.MaxMessageSize);
                pp.MessageArrived = (data) =>
                {
                    var message = MessageBase.ToMessage(data, data.Length);
                    onMessageReceive?.Invoke(message);
                };
                try
                {
                    await pp.StartDataRead(stream, token);
                }
                catch(Exception xe)
                {
                    log.LogError(xe.Message);
                    //Debug.WriteLine(xe.Message);
                    throw;
                }

            }
        }
        /// <summary>
        /// Send a message to a server
        /// </summary>
        /// <param name="client">A client obtained by calling <see cref="Connect"/></param>
        /// <param name="message">The message (derived from MessageBase) to send</param>
        /// <returns></returns>
        public async Task SendAsync(TcpClient client, MessageBase message)
        {
            var stm = client.GetStream();
            await SendAsync(stm, message);
        }
        public async Task ReplyAsync(MessageBase originalMessage, MessageBase reply = null)
        {
            TcpClient replyTo = originalMessage.receivedFrom;
            if(replyTo == null)
            {
                throw new Exception("Cannot reply. Probable reason is that this message was not received from an Fastnet message listener");
            }
            if(reply == null)
            {
                reply = originalMessage;
            }
            await SendAsync(replyTo, reply);
        }
        //public async Task StartMulticastListener(string multicastAddress, int multicastPort, string localIpAddress,  Action<MessageBase> onReceive)
        //{
        //    //Messenger.options = options;
        //    await StartMulticastListener(IPAddress.Parse(multicastAddress), multicastPort, IPAddress.Parse(localIpAddress), onReceive);
        //}
        /// <summary>
        /// Start a multi-cast listener
        /// </summary>
        /// <param name="multicastAddress"></param>
        /// <param name="multicastPort"></param>
        /// <param name="localIpAddress"></param>
        /// <param name="onReceive"></param>
        /// <returns></returns>
        public async Task StartMulticastListener(string multicastAddress, int multicastPort, string localIpAddress, Action<MessageBase> onReceive)
        {
            //Debug.Assert(options != null);
            if (mcastListener == null)
            {
                mcastListener = new MulticastListener(multicastAddress, multicastPort, localIpAddress, loggerFactory);
                await mcastListener.StartAsync(onReceive);
            }
            else
            {
                log.LogInformation("Multicast Listener already started - only one instance is allowed, request ignored");
            }
        }
        public void StopMulticastListener()
        {
            mcastListener?.Stop();
            mcastListener = null;
        }
        //public void EnableMulticastSend(string multicastAddress, int multicastPort, string localIpAddress)
        //{
        //    EnableMulticastSend(IPAddress.Parse(multicastAddress), multicastPort, IPAddress.Parse(localIpAddress));
        //}
        public void EnableMulticastSend(string multicastAddress, int multicastPort, string localIpAddress)
        {
            if (mcastSender == null)
            {
                mcastSender = new MulticastSender(multicastAddress, multicastPort, localIpAddress, loggerFactory);
            }
            else
            {
                log.LogInformation("Multicast send already enabled, request ignored");
            }
        }
        public void DisableMulticastSend(/*IPAddress address, int port*/)
        {
            mcastSender?.Dispose();
            mcastSender = null;
        }
        public async Task SendMulticastAsync(MessageBase message)
        {
            await mcastSender?.SendAsync(message);
        }
        /// <summary>
        /// NB: this method is not thread-safe!!!
        /// a future enhancement 
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task SendAsync(NetworkStream ns, MessageBase message)
        {
            try
            {
                var data = message.ToBytes();
                var wrappedData = PacketProtocol.WrapMessage(data);
                int index = 0;
                while (index < wrappedData.Length)
                {
                    int packetSize = (wrappedData.Length - index) > options.TransportBufferSize ? options.TransportBufferSize : (wrappedData.Length - index);
                    await ns.WriteAsync(wrappedData, index, packetSize);
                    index += packetSize;
                }
                log.LogTrace($"Sent: {message.GetType().Name} ({data.Length} bytes)");
            }
            catch (Exception xe)
            {
                log.LogError(xe.Message);
                //Debugger.Break();
                throw;
            }
        }
        /// <summary>
        /// Start a listener on the given ip address and port. Only one instance of a listener is supported
        /// </summary>
        /// <param name="address">an address such one provided by IpAddress.TryParse</param>
        /// <param name="port">some suitable port no, such as 5858</param>
        /// <param name="onReceive">The method to call for each MessageBase derived message that arrives</param>
        /// <returns></returns>
        private async Task StartListener(IPAddress address, int port, Action<MessageBase> onReceive)
        {
            if (listener == null)
            {
                listener = new SocketListener(address, port, options.TraceMessages);
                await listener.StartAsync(onReceive);
            }
            else
            {
                log.LogInformation("Listener already started - only one instance is allowed, request ignored");

            }
        }


    }
}
