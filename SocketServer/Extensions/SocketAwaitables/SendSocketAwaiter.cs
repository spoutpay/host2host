using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SocketServer.Extensions.SocketAwaitables
{
    public class SendSocketAwaiter : INotifyCompletion
    {
        private readonly static Action SENTINEL = () => { };

        internal bool m_wasCompleted;
        internal Action m_continuation;
        internal SocketAsyncEventArgs m_eventArgs;

        public SendSocketAwaiter(SocketAsyncEventArgs eventArgs)
        {
            if (eventArgs == null) throw new ArgumentNullException("Invalid Arg");
            m_eventArgs = eventArgs;
            eventArgs.Completed += delegate
            {
                var prev = m_continuation ?? Interlocked.CompareExchange(
                    ref m_continuation, SENTINEL, null);
                if (prev != null) prev();
            };
        }

        internal void Reset()
        {
            m_wasCompleted = false;
            m_continuation = null;
        }

        public SendSocketAwaiter GetAwaiter() { return this; }

        public bool IsCompleted { get { return m_wasCompleted; } }

        public void OnCompleted(Action continuation)
        {
            if (m_continuation == SENTINEL ||
                Interlocked.CompareExchange(
                    ref m_continuation, continuation, null) == SENTINEL)
            {
                Task.Run(continuation);
            }
        }

        public int GetResult()
        {
            if (m_eventArgs.SocketError != SocketError.Success)
                throw new SocketException((int)m_eventArgs.SocketError);
            return m_eventArgs.BytesTransferred;
        }
    }
}