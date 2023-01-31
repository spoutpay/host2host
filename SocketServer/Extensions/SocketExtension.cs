using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SocketServer.Extensions.SocketAwaitables;

namespace SocketServer.Extensions
{
    internal static class SocketExtension
    {
        public static ReceiveSocketAwaiter ReceivedAsync(this Socket socket, ReceiveSocketAwaiter awaitable)
        {
            awaitable.Reset();
            if (!socket.ReceiveAsync(awaitable.m_eventArgs))
                awaitable.m_wasCompleted = true;
            return awaitable;
        }

        public static SendSocketAwaiter SentAsync(this Socket socket, SendSocketAwaiter awaitable)
        {
            awaitable.Reset();
            if (!socket.SendAsync(awaitable.m_eventArgs))
                awaitable.m_wasCompleted = true;
            return awaitable;
        }

        public static string ClientIp(this Socket socket)
        {
            return ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
        }
    }
}
