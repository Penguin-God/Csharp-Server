using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace ServerCore
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

            // 문지기가 들 휴대폰 생성
            Socket listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                // 문지기 교육
                listenSocket.Bind(endPoint);

                // 영업 시작
                // backlog : 최대 대기 수
                listenSocket.Listen(10);

                while (true)
                {
                    Console.WriteLine("Listening....");

                    // 손님 입장시키기
                    Socket clientSocket = listenSocket.Accept();

                    // 받는다. 클라이언트한테
                    byte[] recvBuffer = new byte[1024];
                    int recvByte = clientSocket.Receive(recvBuffer);
                    string recvMessage = Encoding.UTF8.GetString(recvBuffer, 0, recvByte);
                    Console.WriteLine($"from client message : {recvMessage}");

                    // 보낸다. 클라이언트한테
                    byte[] sendBuffer = Encoding.UTF8.GetBytes("Hello Client");
                    clientSocket.Send(sendBuffer);

                    // 볼 일 다 봤으니 쫒아낸다
                    clientSocket.Shutdown(SocketShutdown.Both); // 안넣어도 상관은 없음
                    clientSocket.Close();
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }    
}
