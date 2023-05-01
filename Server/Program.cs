using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using ServerCore;
using System.Linq;

namespace Server
{
    class Packet
    {
        public ushort Size;
        public ushort Id;
    }

    class GameSession : PacketSession
    {
        public override void OnConnect(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnect : {endPoint}");
            
            // 보낸다. 클라이언트한테
            //SendBfferHelper.Open(4096);
            //var packet = new Packet() { Size = 4, Id = 3 };
            //byte[] buffer1 = BitConverter.GetBytes(packet.Size);
            //byte[] buffer2 = BitConverter.GetBytes(packet .Id);
            //var sendBuffer = SendBfferHelper.Close(buffer1.Concat(buffer2).Count());
            //Send(sendBuffer);

            Thread.Sleep(5000);
            DisConnect();
        }

        public override void OnDisconnect(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisConnect : {endPoint}");
        }

        public override void OnReceivePacket(ArraySegment<byte> buffer)
        {
            ushort packetSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            ushort packetId = BitConverter.ToUInt16(buffer.Array, buffer.Offset + 2);
            Console.WriteLine($"size : {packetSize}, Id : {packetId}");
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
            InitListener();
            Console.WriteLine("Listening....");

            Console.ReadKey();
        }

        static Listener InitListener()
        {
            // DNS( Domain Name System) 사용
            // www.penguingod.com -> 123.1.2.3 처럼 이름을 통해서 주소에 접근하는 방식
            string host = Dns.GetHostName(); // 내 로컬 컴퓨터의 호스트 이름을 가져옴
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddress = ipHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 1234);

            Listener listener = new Listener();
            listener.Init(endPoint, () => new GameSession());
            return listener;
        }
    }
}
