using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketServer.Models
{
    public class StateData
    {
        public const int BufferSize = 8192;
        public byte[] buffer = new byte[BufferSize];
        public byte[]? response = null;
        public StringBuilder sb = new StringBuilder();
        public string clientIp = "";
        public Socket? workSocket;
        public double responseTime;
    }
}
