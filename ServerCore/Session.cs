using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ServerCore
{
    public abstract class Session
    {
        Socket _clientSocket;
        const int BUFFER_SIZE = 1024;
        ReceiveBuffer _recvBuffer = new ReceiveBuffer(BUFFER_SIZE);

        public void Start(Socket clientSocket)
        {
            _clientSocket = clientSocket;
            InitRecive();
            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSend);
        }

        public abstract void OnConnect(EndPoint endPoint);
        public abstract void OnDisconnect(EndPoint endPoint);
        public abstract void OnSend(int num);
        public abstract int OnReceive(ArraySegment<byte> buffer);

        void InitRecive()
        {
            var reciveArgs = new SocketAsyncEventArgs();
            reciveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecive);
            RegisterRecive(reciveArgs);
        }

        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
        List<ArraySegment<byte>> _sendMessages = new List<ArraySegment<byte>>(); // 0이 아니면 누군가가 send 대기중
        IList<ArraySegment<byte>> _butfferList = new List<ArraySegment<byte>>(); // 0이 아니라면 버퍼를 보내는 중
        object _sendLock = new object();
        protected void Send(ArraySegment<byte> buffer)
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
            _butfferList = _sendMessages.ToList();
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
            _recvBuffer.Clear();
            var segment = _recvBuffer.WriteSegment;
            reciveArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);
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
                    if(_recvBuffer.OnWrite(_sendArgs.BytesTransferred) == false)
                    {
                        DisConnect();
                        return;
                    }

                    int processLen = OnReceive(new ArraySegment<byte>(reciveArgs.Buffer, reciveArgs.Offset, reciveArgs.BytesTransferred));
                    
                    if(_recvBuffer.OnRead(processLen) == false)
                    {
                        DisConnect();
                        return;
                    }

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
