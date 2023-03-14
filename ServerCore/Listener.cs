using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace ServerCore
{
    class Listener
    {
        Socket _listenSocket;
        Func<Session> _createSession;

        public void Init(IPEndPoint endPoint, Func<Session> createSession)
        {
            // 문지기가 들 휴대폰 생성
            _listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _createSession = createSession;
            // 문지기 교육
            _listenSocket.Bind(endPoint);

            // 영업 시작
            // backlog : 최대 대기 수
            _listenSocket.Listen(10);

            var acceptEvent = new SocketAsyncEventArgs();
            acceptEvent.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
            RegisterAccept(acceptEvent);
        }

        void RegisterAccept(SocketAsyncEventArgs acceptEvent)
        {
            acceptEvent.AcceptSocket = null; // 새로운 소켓을 접속시키기 위해 기존 정보 제거
            bool isPending = _listenSocket.AcceptAsync(acceptEvent); // 이게 접속 매서드

            if (isPending == false) // 비동기로 시도했지만 딜레이 없이 바로 됐을 경우(오직 성능을 위한 코드. 없어도 동작은 함)
                OnAcceptCompleted(null, acceptEvent);
        }

        void OnAcceptCompleted(object sender, SocketAsyncEventArgs acceptEvent)
        {
            if (acceptEvent.SocketError == SocketError.Success)
            {
                Session session = _createSession();
                session.Start(acceptEvent.AcceptSocket);
                session.OnConnect(acceptEvent.RemoteEndPoint);
            }
            else
                Console.WriteLine(acceptEvent.SocketError.ToString());

            RegisterAccept(acceptEvent); // 다음 소켓 등록
        }
    }
}
