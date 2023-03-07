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
            InitListener();
            Console.WriteLine("Listening....");

<<<<<<< HEAD
            Console.ReadKey();
        }

        static void OnAccept(Socket clientSocket)
        {
            try
            {
                var session = new Session();
                session.Start(clientSocket);

                // 보낸다. 클라이언트한테
                byte[] sendBuffer = Encoding.UTF8.GetBytes("Hello Client");
                session.Send(sendBuffer);

                Thread.Sleep(1000);
                session.DisConnect();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

=======
            while (true)
            {

            }

            Console.ReadKey();
        }

>>>>>>> b6f9585fd76c5cdecbd56c9aa15e461c911ed620
        static Listener InitListener()
        {
            // DNS( Domain Name System) 사용
            // www.penguingod.com -> 123.1.2.3 처럼 이름을 통해서 주소에 접근하는 방식
            string host = Dns.GetHostName(); // 내 로컬 컴퓨터의 호스트 이름을 가져옴
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddress = ipHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 1234);

            Listener listener = new Listener();
            listener.Init(endPoint, OnAccept);
            return listener;
<<<<<<< HEAD
=======
        }

        static void OnAccept(Socket clientSocket)
        {
            try
            {
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
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
>>>>>>> b6f9585fd76c5cdecbd56c9aa15e461c911ed620
        }
    }
}
