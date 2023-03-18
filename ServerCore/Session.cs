using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    public abstract class Session
    {
        Socket _clientSocket;
        const int BUFFER_SIZE = 1024;
        public void Start(Socket clientSocket)
        {
            _clientSocket = clientSocket;
            InitRecive();
            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSend);
        }

        public abstract void OnConnect(EndPoint endPoint);
        public abstract void OnDisconnect(EndPoint endPoint);
        public abstract void OnSend(int num);
        public abstract void OnReceive(ArraySegment<byte> buffer);

        void InitRecive()
        {
            var reciveArgs = new SocketAsyncEventArgs();
            reciveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecive);
            reciveArgs.SetBuffer(new byte[BUFFER_SIZE], 0, BUFFER_SIZE);
            RegisterRecive(reciveArgs);
        }

        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
        List<byte[]> _sendMessages = new List<byte[]>(); // 0이 아니면 누군가가 send 대기중
        IList<ArraySegment<byte>> _butfferList = new List<ArraySegment<byte>>(); // 0이 아니라면 버퍼를 보내는 중
        object _sendLock = new object();
        protected void Send(byte[] buffer)
        {
            lock (_sendLock) 
            {
                _sendMessages.Add(buffer);
                if (_butfferList.Count == 0)
                    RegisterSend();
            }
        }

        void RegisterSend()
        {
            _butfferList = 
                _sendMessages
                .Select(x => new ArraySegment<byte>(x, 0, x.Length))
                .ToList();
            _sendMessages.Clear();
            _sendArgs.BufferList = _butfferList;

            bool isPending = _clientSocket.SendAsync(_sendArgs);

            if (isPending == false)
                OnSend(null, _sendArgs);
        }

        void OnSend(object obj, SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                args.BufferList = null;
                _butfferList.Clear();

                OnSend(args.BytesTransferred);

                if (_sendMessages.Count > 0)
                    RegisterSend();
            }
            else
                DisConnect();
        }


        void RegisterRecive(SocketAsyncEventArgs reciveArgs)
        {
            reciveArgs.AcceptSocket = null;
            if (_clientSocket == null) return;
            bool isPending = _clientSocket.ReceiveAsync(reciveArgs);

            if (isPending == false)
                OnRecive(null, reciveArgs);
        }

        void OnRecive(object sender, SocketAsyncEventArgs reciveArgs)
        {
            lock (_sendLock)
            {
                if (reciveArgs.BytesTransferred > 0 && reciveArgs.SocketError == SocketError.Success)
                {
                    OnReceive(new ArraySegment<byte>(reciveArgs.Buffer, reciveArgs.Offset, reciveArgs.BytesTransferred));
                    RegisterRecive(reciveArgs);
                }
                else
                    DisConnect();
            }
        }

        int _disConneted;
        protected void DisConnect()
        {
            if (Interlocked.Exchange(ref _disConneted, 1) == 1)
                return;

            OnDisconnect(_clientSocket.RemoteEndPoint);
            _clientSocket.Shutdown(SocketShutdown.Both); // 안넣어도 상관은 없음
            _clientSocket.Close();
        }
    }
}
