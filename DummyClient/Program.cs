using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DummyClient
{
    class GameSession : Session
    {
        public override void OnConnect(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnect : {endPoint}");
            // 보낸다. 서버한테
            for (int i = 0; i < 5; i++)
            {
                byte[] sendBuffer = Encoding.UTF8.GetBytes($"Hello Server : {i}");
                Send(sendBuffer);
            }
        }

        public override void OnDisconnect(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisConnect : {endPoint}");
        }

        public override void OnReceive(ArraySegment<byte> buffer)
        {
            string recvMessage = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
            Console.WriteLine($"from server : {recvMessage}");
        }

        public override void OnSend(int num)
        {
            Console.WriteLine($"바이트 크기는 {num}");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // DNS( Domain Name System) 사용
            // www.penguingod.com -> 123.1.2.3 처럼 이름을 통해서 주소에 접근하는 방식
            string host = Dns.GetHostName(); // 내 로컬 컴퓨터의 호스트 이름을 가져옴
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddress = ipHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 1234);

            Connector connector = new Connector();
            connector.Connect(endPoint, () => new GameSession());

            while (true)
            {
                Thread.Sleep(100);
            }
        }
    }
}
