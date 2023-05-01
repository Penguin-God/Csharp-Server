using ServerCore;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace DummyClient
{
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
            connector.Connect(endPoint, () => new ServerSession());

            while (true)
            {
                Thread.Sleep(100);
            }
        }
    }
}
