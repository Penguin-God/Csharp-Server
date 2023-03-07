using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    class Session
    {
        Socket _clientSocket;
        public void Start(Socket clientSocket)
        {
            _clientSocket = clientSocket;

            var reciveArgs = new SocketAsyncEventArgs();
            reciveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecive);
            const int BUFFER_SIZE = 1024;
            reciveArgs.SetBuffer(new byte[BUFFER_SIZE], 0, BUFFER_SIZE);
            RegisterRecive(reciveArgs);
        }

        public void Send(byte[] buffer)
        {
            _clientSocket.Send(buffer);
        }

        void RegisterRecive(SocketAsyncEventArgs reciveArgs)
        {
            reciveArgs.AcceptSocket = null;
            bool isPending = _clientSocket.ReceiveAsync(reciveArgs);

            if (isPending == false)
                OnRecive(null, reciveArgs);
        }

        void OnRecive(object sender, SocketAsyncEventArgs reciveArgs)
        {
            if(reciveArgs.BytesTransferred > 0 && reciveArgs.SocketError == SocketError.Success)
            {
                string recvMessage = Encoding.UTF8.GetString(reciveArgs.Buffer, reciveArgs.Offset, reciveArgs.BytesTransferred);
                Console.WriteLine($"from client message : {recvMessage}");
                RegisterRecive(reciveArgs);
            }
        }

        int _disConneted;
        public void DisConnect()
        {
            if (Interlocked.Exchange(ref _disConneted, 1) == 1)
                return;

            _clientSocket.Shutdown(SocketShutdown.Both); // 안넣어도 상관은 없음
            _clientSocket.Close();
        }
    }
}
