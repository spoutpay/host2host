using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketServer.Models
{
    internal class SslSocket
    {
        public SslStream stream { get; set; }
        public Socket client { get; set; }

        public SslSocket(SslStream stream, Socket client)
        {
            this.stream = stream;
            this.client = client;
        }
    }
}
