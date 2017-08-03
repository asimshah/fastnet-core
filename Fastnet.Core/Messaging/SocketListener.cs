using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Core
{
    /// <summary>
    /// SocketListener is not intended for direct use - <see cref="Fastnet.Core.Messenger"/> 
    /// </summary>
    public class SocketListener
    {
        private CancellationTokenSource cts;
        private int port;
        private IPAddress listenTo;
        private TcpListener listener;
        //private ILogger log;
        private List<TcpClient> clientList;
        private bool logMessages;
        public SocketListener(IPAddress address, int port, bool logMessages)
        {
            //log = new Logger<SocketListener>();
            listenTo = address ?? IPAddress.Any;
            this.port = port;
            this.logMessages = logMessages;
        }
        public void Stop()
        {
            cts.Cancel();
            foreach (var client in clientList)
            {
                client.Close();
            }
        }
        public async Task StartAsync(Action<MessageBase> onMessageReceive)
        {
            cts = new CancellationTokenSource();
            clientList = new List<TcpClient>();
            listener = new TcpListener(listenTo, port);
            listener.ExclusiveAddressUse = true;
            
            listener.Start();
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await AcceptClientAsync(listener, cts.Token);
                    if (client != null)
                    {
                        client.NoDelay = true;
                        clientList.Add(client);
#pragma warning disable 4014
                        Task.Run(async () =>
                        {
                            var ct = cts.Token;
                            //log.Write($"New client ({GetClientDescr(client)}) connected");
                            await ServerReceiveAsync(client, ct, onMessageReceive);
                        });
#pragma warning restore 4014
                    }
                }
                catch { } 
            }
        }
        private async Task<TcpClient> AcceptClientAsync(TcpListener listener, CancellationToken token)
        {
            using (token.Register(listener.Stop))
            {
                try
                {
                    return await listener.AcceptTcpClientAsync();
                }
                catch (Exception)
                {
                    Debugger.Break();
                }
            }
            return null;
        }
        private string GetClientDescr(TcpClient client)
        {
            IPEndPoint remoteEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
            string remoteIpAddress = $"{remoteEndPoint.Address.ToString()}:{remoteEndPoint.Port}";
            IPEndPoint localEndPoint = (IPEndPoint)client.Client.LocalEndPoint;
            string localIpAddress = $"{localEndPoint.Address.ToString()}:{localEndPoint.Port}";
            return $"remote {remoteIpAddress}, local {localIpAddress}";
        }
        private async Task ServerReceiveAsync(TcpClient client, CancellationToken token, Action<MessageBase> onMessageReceive)
        {
            Action<TcpClient> disposeClient = (c) =>
            {
                var index = clientList.IndexOf(c);
                if (index >= 0)
                {
                    clientList.RemoveAt(index);
                }
                if(c.Connected)
                {
                    c.Close();
                }
                //log.Write($"Client {GetClientDescr(c)} dropped");
            };
            using (client)
            {
                var buffer = new byte[MessageBase.TransportBufferSize];
                var stream = client.GetStream();
                PacketProtocol pp = new PacketProtocol(MessageBase.MaxMessageSize);
                pp.MessageArrived = (data) =>
                {
                    //Debug.WriteLine($"recd: data of length {data.Length}");
                    if (data.Length > 0)
                    {
                        var message = MessageBase.ToMessage(data, data.Length);
                        if(logMessages)
                        {
                            //log.Write($"Received {message.GetType().Name}");
                        }
                        message.receivedFrom = client;
                        onMessageReceive?.Invoke(message);
                        //if (message is EchoMessage)
                        //{
                        //    EchoMessage echo = message as EchoMessage;
                        //    await SendAsync(client, echo);
                        //}
                        //else
                        //{
                        //    message.receivedFrom = client;
                        //    onMessageReceive?.Invoke(message);
                        //}
                    }
                };
                try
                {
                    await pp.StartDataRead(stream, token);
                    //while (!token.IsCancellationRequested)
                    //{
                    //    int count = 0;
                    //    count = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                    //    if (count > 0)
                    //    {
                    //        byte[] data = new byte[count];
                    //        Array.Copy(buffer, data, count);
                    //        pp.DataReceived(data);
                    //    }
                    //}
                }
                catch (System.IO.IOException e1)
                {
                    if (e1.InnerException is SocketException)
                    {
                        var se = (SocketException)e1.InnerException;
                        if (se.SocketErrorCode == SocketError.ConnectionReset)
                        {
                            disposeClient(client);
                            //var index = clientList.IndexOf(client);
                            //if (index >= 0)
                            //{
                            //    clientList.RemoveAt(index);
                            //}
                        }
                        else
                        {
                            //log.Write(LogLevel.Error, $"SocketErrorCode is {se.SocketErrorCode.ToString()}");
                        }
                    }
                    else
                    {
                        //log.Write(e1.InnerException);
                    }
                }
                catch (Exception)
                {
                    //log.Write(xe);
                    disposeClient(client);
                }
            }
        }

        //private async Task AddClientAsync(TcpClient client, CancellationToken token, Action<NetworkStream, byte[], int> onReceive)
        //{
        //    IPEndPoint endPoint = (IPEndPoint)client.Client.LocalEndPoint;
        //    string ipAddress = $"{endPoint.Address.ToString()}:{endPoint.Port}";
        //    log.Write($"New client {ipAddress} connected");
        //    using (client)
        //    {
        //        var buffer = new byte[8192 * 2];
        //        var stream = client.GetStream();
        //        try
        //        {
        //            while (!token.IsCancellationRequested)
        //            {
        //                int count = await stream.ReadAsync(buffer, 0, buffer.Length, token);
        //                onReceive(stream, buffer, count);
        //                //break;
        //            }
        //        }
        //        catch (Exception)
        //        {
        //            Debugger.Break();
        //            throw;
        //        }
        //    }
        //    log.Write($"New client {ipAddress} disconnected");
        //}
        //private static async void ReceiveAtListener(NetworkStream ns, byte[] buffer, int length, Action<MessageBase> onMessageReceive)
        //{
        //    Debug.WriteLine($"recd: data of length {length}");
        //    if (length > 0)
        //    {
        //        var message = MessageBase.ToMessage(buffer, length);
        //        if (message is EchoMessage)
        //        {
        //            EchoMessage echo = message as EchoMessage;
        //            await Messenger.SendAsync(ns, echo);
        //        }
        //        else
        //        {
        //            onMessageReceive?.Invoke(message);
        //        }
        //    }
        //}
    }
}


