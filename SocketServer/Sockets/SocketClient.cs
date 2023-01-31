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

namespace SocketServer.Sockets
{
    internal class SocketClient
    {
        private readonly ILogger<SocketClient> logger;
        private readonly IConfiguration configuration;

        private readonly int? port;
        private readonly string? ip;

        public readonly Socket? client;

        public SocketClient(ILogger<SocketClient> logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;

            this.port = configuration["serverPort"] != null ? int.Parse(configuration["serverPort"] ?? "0") : null; 
            this.ip = configuration["serverIp"];


            this.client = this.Connect();
        }


        public Socket? Connect()
        {
            try
            {
                if (ip == null || port == null)
                {
                    this.logger.LogError("Server IP or PORT cannot be empty");
                    return null;
                }

                IPAddress ipAddress = IPAddress.Parse(ip);
                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, (int)port);

                Socket clientSocket = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);
                clientSocket.SendTimeout = 55000;
                clientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                clientSocket.Connect(localEndPoint);

                return clientSocket;
            }
            catch (Exception e)
            {
                logger.LogError(e,"Error connecting to the server");
                return null;
            }
        }

        public async Task<byte[]> SendMessage(byte[] request)
        {
            if(client == null)
            {
                throw new NullReferenceException("Client socket is null");
            }

            if (!client.Connected)
            {
                throw new InvalidOperationException("Client socket is not connected");
            }

            var sent = await client?.SendAsync(request, SocketFlags.None);

            // Data buffer
            byte[] messageReceived = new byte[8192];

            int byteRecv = await client.ReceiveAsync(messageReceived,SocketFlags.None);
            Console.WriteLine("Message from Server -> {0}",
                  Encoding.ASCII.GetString(messageReceived,
                                             0, byteRecv));
            if (byteRecv <= 0)
            {
                return null;
            }

            return messageReceived.Take(byteRecv).ToArray();
        }
    }
}
