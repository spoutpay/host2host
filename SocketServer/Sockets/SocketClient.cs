using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SocketServer.Models;
using SocketServer.Extensions;
using System.Reflection;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.ConstrainedExecution;
using System.Reflection.Metadata.Ecma335;

namespace SocketServer.Sockets
{
    internal class SocketClient
    {
        private readonly ILogger<SocketClient> logger;
        private readonly IConfiguration configuration;

        private readonly int? port;
        private readonly string? ip;
        private readonly string? certPath;
        private readonly string? certPwd;
        private readonly bool isSsl;

        private Socket? client;
        private SslSocket? sslClient;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public SocketClient(ILogger<SocketClient> logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;

            this.port = configuration["serverPort"] != null ? int.Parse(configuration["serverPort"] ?? "0") : null; 
            this.ip = configuration["serverIp"];
            this.certPath = configuration["certPath"];
            this.certPwd = configuration["certPwd"];
            this.isSsl = Convert.ToBoolean(configuration["isSslClient"] ?? "false");



            var connection = this.Connect();
            this.client = connection.plain;
            this.sslClient = connection.ssl;
        }


        public (Socket? plain,SslSocket? ssl) Connect()
        {
            try
            {
                if (ip == null || port == null)
                {
                    this.logger.LogError("Server IP or PORT cannot be empty");
                    return (null,null);
                }

                IPAddress ipAddress = IPAddress.Parse(ip);
                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, (int)port);

                if (isSsl)
                {
                    Socket tcpClient = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);
                    tcpClient.SendTimeout = 55000;
                    tcpClient.Connect(localEndPoint);

                    X509Certificate2Collection collection = new X509Certificate2Collection();

                    collection.Import(certPath, certPwd, X509KeyStorageFlags.PersistKeySet);
                    Stream networkStream = new NetworkStream(tcpClient);
                    SslStream stream = new SslStream(networkStream, false, new RemoteCertificateValidationCallback(RemoteCertificateValidationCallback),null);
                    stream.AuthenticateAsClientAsync(collection[0].Subject);
                   
                    
                    logger.LogInformation("Autheticate {0} at {1}", tcpClient.ClientIp(), DateTime.Now);

                    return (null, new SslSocket(stream,tcpClient));
                }

                Socket clientSocket = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);
                clientSocket.SendTimeout = 55000;
                clientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                clientSocket.Connect(localEndPoint);
                return (clientSocket,null);
            }
            catch (Exception e)
            {
                logger.LogError(e,"Error connecting to the server");
                return (null,null);
            }
        }

        private async Task<byte[]> SendMessageWithSSL(byte[] request)
        {
            if (sslClient == null)
            {
                throw new NullReferenceException("Client socket is null");
            }

            if (!sslClient.client.Connected)
            {

                var result = Connect();
                sslClient = result.ssl;
            }

            // Data buffer
            byte[] messageReceived = new byte[8192];
            int byteRecv = 0;
            int bytes = -1;

            await _semaphore.WaitAsync();
            try
            {
                await sslClient.stream.WriteAsync(request);
                await sslClient.stream.FlushAsync();

                do
                {
                    bytes = await sslClient.stream.ReadAsync(messageReceived, byteRecv, messageReceived.Length- byteRecv);
                    byteRecv += bytes;
                } while (bytes != 0);
            }
            finally
            {
                _semaphore.Release();
            }

            Console.WriteLine("Message from Server -> {0}",
                  Encoding.ASCII.GetString(messageReceived,
                                             0, byteRecv));
            if (byteRecv <= 0)
            {
                return null;
            }

            return messageReceived.Take(byteRecv).ToArray();
        }

        private async Task<byte[]?> SendMessagePlain(byte[] request)
        {
            if(client == null)
            {
                throw new NullReferenceException("Client socket is null");
            }

            if (!client.Connected)
            {
                var result = Connect();
                client = result.plain;
            }

            // Data buffer
            byte[] messageReceived = new byte[8192];
            int byteRecv = 0;

            await _semaphore.WaitAsync();
            try
            {
                var requestUtf8 = request.BytesToUtf8();
                logger.LogInformation($"Sending message {requestUtf8.Substring(0,Math.Min(requestUtf8.Length,67))} to server.");
                var sent = await client.SendAsync(request, SocketFlags.None);
                logger.LogInformation($"Sending {sent}, request length {request.Length}");
                

                var receiveTask = client.ReceiveAsync(messageReceived, SocketFlags.None);
                var timeOutTask = Task.Delay(55000);

                if(receiveTask != await Task.WhenAny(receiveTask, timeOutTask))
                {
                    logger.LogError("Request timeout");
                    _semaphore.Release();
                    return null;
                }
                byteRecv = receiveTask.Result;

            }
            finally
            {
                _semaphore.Release();
            }

            Console.WriteLine("Message from Server -> {0}",
                  Encoding.ASCII.GetString(messageReceived,
                                             0, byteRecv));
            if (byteRecv <= 0)
            {
                return null;
            }

            return messageReceived.Take(byteRecv).ToArray();
        }

        public async Task<byte[]> SendMessage(byte[] request)
        {
            if (isSsl)
            {
                return await SendMessageWithSSL(request);
            }

            return await SendMessagePlain(request);
        }


        private bool RemoteCertificateValidationCallback(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
