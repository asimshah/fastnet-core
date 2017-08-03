using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Fastnet.Core.Extensions;


namespace Fastnet.Core
{
    internal static class udpExtensions
    {
        public static void EnableMulticast(this UdpClient client, string multicastAddress, int multicastPort, string localAddress)
        {
            IPAddress multicastIpAddress = IPAddress.Parse(multicastAddress);
            IPAddress localIpAddress = null;
            IPNetwork localNetwork = null;
            if (IPNetwork.TryParse(localAddress, out localNetwork))
            {
                localIpAddress = GetLocalIpAddress(localNetwork);

            }
            else
            {
                localIpAddress = IPAddress.Parse(localAddress);
            }
            var localEndPoint = new IPEndPoint(localIpAddress, multicastPort);
            client.ExclusiveAddressUse = false;
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            // Bind, Join
            client.Client.Bind(localEndPoint);
            client.JoinMulticastGroup(multicastIpAddress, localIpAddress);
            //return client;
        }

        private static IPAddress GetLocalIpAddress(IPNetwork localNetwork)
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if(IPNetwork.Contains(localNetwork, ip))
                //if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;// IPAddress.Parse(ip.ToString());
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }
    }
    internal class MulticastListener
    {
        private CancellationTokenSource cts;
        private UdpClient client;
        //private bool logMessages;
        private ILogger log;
        //private IPEndPoint listenTo;
        internal MulticastListener(string multicastAddress, int multicastPort, string localIpAddress, ILoggerFactory lf)
        {
            try
            {
                this.log = lf.CreateLogger<MulticastListener>();
                client = new UdpClient();
                client.EnableMulticast(multicastAddress, multicastPort, localIpAddress);
                //var localEndPoint = new IPEndPoint(localIpAddress, multicastPort);
                //client = new UdpClient();
                //// The following two lines allow multiple clients on the same PC
                //client.ExclusiveAddressUse = false;
                //client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                //// Bind, Join
                //client.Client.Bind(localEndPoint);
                //client.JoinMulticastGroup(multicastAddress, localIpAddress);
                //log.LogInformation($"listening to {address.ToString()}, port {port}");
            }
            catch (Exception xe)
            {
                log.LogError(xe);
                throw;
            }
        }
        internal async Task StartAsync(Action<MessageBase> onMessageReceive)
        {
            cts = new CancellationTokenSource();
            //IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            while (!cts.IsCancellationRequested)
            {
                using (cts.Token.Register(client.Dispose))
                {
                    try
                    {
                        UdpReceiveResult result = await client.ReceiveAsync();
                        //Debug.WriteLine($"mc: recd {result.Buffer.Length} bytes");
                        var message = MessageBase.ToMessage(result.Buffer, result.Buffer.Length);
                        log.LogTrace($"Received {result.Buffer.Length} bytes: message {message.GetType().Name} from {result.RemoteEndPoint.ToString()}");
                        onMessageReceive(message);
                    }
                    catch { }
                }
            }
        }
        internal void Stop()
        {
            cts.Cancel();
        }
    }
}
