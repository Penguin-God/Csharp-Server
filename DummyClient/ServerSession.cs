using ServerCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Text;

namespace DummyClient
{
    class ServerSession : Session
    {
        public abstract class Packet
        {
            public ushort Size;
            public ushort PacketId;

            public abstract ArraySegment<byte> Write();
            public abstract void Read(ArraySegment<byte> s);
        }

        class PlayerInfoRequest : Packet
        {
            public long PlayerId;
            public string Name;

            public PlayerInfoRequest()
            {
                PacketId = (ushort)PacketType.PlayerInfoRequest;
            }

            public override ArraySegment<byte> Write()
            {
                var s = SendBfferHelper.Open(4096);
                // 구조체 안에는 배열이 들어있었네 ㅋㅋ
                byte[] packetId = BitConverter.GetBytes(base.PacketId);
                byte[] playerId = BitConverter.GetBytes(PlayerId);
                int count = 0;
                count += sizeof(ushort);
                Array.Copy(packetId, 0, s.Array, s.Offset + count, 2);
                count += sizeof(ushort);
                Array.Copy(playerId, 0, s.Array, s.Offset + count, 8);
                count += sizeof(long);

                ushort nameLen = (ushort)Encoding.Unicode.GetByteCount(Name);
                Array.Copy(BitConverter.GetBytes(nameLen), 0, s.Array, s.Offset + count, 2);
                count += sizeof(ushort);

                Array.Copy(Encoding.Unicode.GetBytes(this.Name), 0, s.Array, s.Offset + count, nameLen);
                count += nameLen;

                // 마지막에는 패킷 크기 넣음
                byte[] size = BitConverter.GetBytes(count);
                Array.Copy(size, 0, s.Array, s.Offset, 2);

                return SendBfferHelper.Close(count);
            }

            public override void Read(ArraySegment<byte> s)
            {
                ushort readIndex = 0;
                readIndex += sizeof(ushort);
                readIndex += sizeof(ushort);
                PlayerId = BitConverter.ToInt64(s.Array, s.Offset + 4);
                readIndex += sizeof(long);

                ushort nameLen = BitConverter.ToUInt16(s.Array, readIndex);
                readIndex += sizeof(ushort);
                Name = Encoding.Unicode.GetString(new ArraySegment<byte>(s.Array, readIndex, nameLen).Array);
            }
        }

        enum PacketType
        {
            PlayerInfoRequest = 0,
            PlayerInfoResult = 1,
        }

        public override void OnConnect(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnect : {endPoint}");

            PlayerInfoRequest packet = new PlayerInfoRequest() { PlayerId = 1001, Name = "PenguinGod" };
            Send(packet.Write());
        }

        public override void OnDisconnect(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisConnect : {endPoint}");
        }

        public override int OnReceive(ArraySegment<byte> buffer)
        {
            string recvMessage = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
            Console.WriteLine($"from server : {recvMessage}");
            return buffer.Count;
        }

        public override void OnSend(int num)
        {
            Console.WriteLine($"바이트 크기는 {num}");
        }
    }

}
