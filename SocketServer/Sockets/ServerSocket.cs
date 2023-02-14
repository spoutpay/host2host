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

namespace SocketServer.Sockets
{
    internal class ServerSocket
    {
        // Thread signal.  
        public ManualResetEvent allDone = new ManualResetEvent(false);
        private readonly ILogger<ServerSocket> logger;
        private readonly SocketClient socketClient;
        private readonly int port;
        private readonly int maxConn;

        public ServerSocket(ILogger<ServerSocket> logger, IConfiguration configuration, SocketClient socketClient)
        {
            this.logger = logger;
            this.socketClient = socketClient;
            this.port = int.Parse(configuration["port"] ?? "2000");
            this.maxConn = int.Parse(configuration["maxConn"] ?? "100");
        }

        public void StartListening()
        {

            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

            Socket listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(maxConn);

                logger.LogInformation("Socket server start and listening on PORT {0} at {1}", port, DateTime.Now);

                while (true)
                {
                    allDone.Reset();

                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);

                    // Wait until a connection is made before continuing.  
                    allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                logger.LogError(e.Message, e);
            }

        }

        private void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();


            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            logger.LogInformation("new connection from {0} at {1}", handler.ClientIp(), DateTime.Now);

            StateData state = new StateData();
            state.workSocket = handler;
            state.clientIp = handler.ClientIp();

            handler.BeginReceive(state.buffer, 0, StateData.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }

        private void ReadCallback(IAsyncResult ar)
        {
            var start = DateTime.Now;
            String content = String.Empty;

            StateData state = (StateData)ar.AsyncState;
            Socket handler = state.workSocket;

            try
            {

                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {

                    state.sb.Append(state.buffer.BytesToHexString(bytesRead));

                    content = state.sb.ToString();
                    var len = int.Parse(content.Substring(0, 4), NumberStyles.HexNumber);
                    if (len == content.Substring(4).Length / 2)
                    {
                        logger.LogInformation("Read {0} bytes from socket: {1}. Data:{2}",
                            content.Length / 2, state.clientIp, content.ToUtf8().Substring(0, Math.Min(67,content.ToUtf8().Length)));

                        Task.Run(async () =>
                        {
                            
                            var response = await socketClient.SendMessage(content.HexToByteArray());
                            state.responseTime = DateTime.Now.Subtract(start).TotalSeconds;
                            if (response != null)
                            {
                                Send(state, response);
                            }
                            else
                            {
                                handler.Shutdown(SocketShutdown.Both);
                                handler.Close();
                            }
                            
                        });
                    }
                    else
                    {
                        handler.BeginReceive(state.buffer, 0, StateData.BufferSize, 0,
                        new AsyncCallback(ReadCallback), state);
                    }
                }
            }
            catch (SocketException e)
            {
                logger.LogError("Error Reading data from {0} at {1}", state.clientIp, DateTime.Now);
                logger.LogError(e.Message);

                if (handler != null)
                {
                    if (handler.Connected)
                    {
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                        handler = null;
                    }
                }
            }
        }

        private void Send(StateData stateData, byte[] data)
        {
            Socket handler = stateData.workSocket;
            stateData.response = data;

            handler.BeginSend(data, 0, data.Length, 0,
                new AsyncCallback(SendCallback), stateData);
        }

        private void SendCallback(IAsyncResult ar)
        {
            StateData state = (StateData)ar.AsyncState;
            Socket handler = state.workSocket;

            var res = "";
            if (state.response != null)
            {
                var resString = state.response.BytesToUtf8();
                res = resString.Substring(0, Math.Min(67, resString.Length));
            }

            try
            {
                int bytesSent = handler.EndSend(ar);
                logger.LogInformation("Response written to {0} after {1} seconds. =>>{2}", state.clientIp,state.responseTime , res);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
                logger.LogInformation("Closed connection from {0} at {1}", state.clientIp, DateTime.Now);
            }
            catch (Exception e)
            {
                logger.LogError("Error writing to {0} at {1}", state.clientIp, DateTime.Now);
                logger.LogError(e.Message);
            }
        }
    }
}
