using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Fastnet.Core
{
    public class AMAliveMulticast : MessageBase
    {
        public string Text { get; set; }
    }
    internal class MulticastSender : IDisposable
    {
        private UdpClient sendClient;
        private IPEndPoint sendTo;
        //private bool logMessages;
        private ILogger log;
        internal MulticastSender(string multicastAddress, int multicastPort, string localIpAddress, ILoggerFactory lf)
        {
            this.log = lf.CreateLogger<MulticastSender>();
            //this.logMessages = logMessages;
            sendClient = new UdpClient();
            sendClient.EnableMulticast(multicastAddress, multicastPort, localIpAddress);
            sendTo = new IPEndPoint(IPAddress.Parse(multicastAddress), multicastPort);
            //sendClient.JoinMulticastGroup(sendTo.Address, 50);
            log.LogInformation($"sending enabled, {multicastAddress.ToString()}, port {multicastPort}");
        }
        public async Task SendAsync(MessageBase message)
        {
            var data = message.ToBytes();
            await sendClient.SendAsync(data, data.Length, sendTo);
            log.LogTrace($"Sent {data.Length} bytes to {sendTo.ToString()}");
            //if (logMessages)
            //{
            //    //log.Write(message.ToString());
            //}
        }
        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (sendClient != null)
                    {
                        sendClient.Close();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }
        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MulticastSender() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
