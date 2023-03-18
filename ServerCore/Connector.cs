using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public class Connector
    {
        Func<Session> _createSession;
        public void Connect(IPEndPoint endPoint, Func<Session> createSession)
        {
            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _createSession = createSession;

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += OnConnected;
            args.RemoteEndPoint = endPoint;
            args.UserToken = socket;

            RegisterConnect(args);
        }

        void RegisterConnect(SocketAsyncEventArgs args)
        {
            Socket socket = (Socket)args.UserToken;
            bool isPending = socket.ConnectAsync(args);
            if (isPending == false)
                OnConnected(null, args);
        }

        void OnConnected(object sender, SocketAsyncEventArgs args)
        {
            if(args.SocketError == SocketError.Success)
            {
                Session session = _createSession();
                session.Start(args.ConnectSocket);
                session.OnConnect(args.RemoteEndPoint);
            }
        }
    }
}
