using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace Server
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

        public struct SkillInfo
        {
            public int id;
            public short level;

            public void Write(ArraySegment<byte> s, ref ushort count)
            {
                Array.Copy(BitConverter.GetBytes(id), 0, s.Array, s.Offset + count, sizeof(int));
                count += sizeof(int);
                Array.Copy(BitConverter.GetBytes(level), 0, s.Array, s.Offset + count, sizeof(short));
                count += sizeof(short);
            }

            public void Read(ArraySegment<byte> s, ref ushort count)
            {
                id = BitConverter.ToInt32(s.Array, s.Offset + count);
                count += sizeof(int);
                level = BitConverter.ToInt16(s.Array, s.Offset + count);
                count += sizeof(ushort);
            }
        }
        public List<SkillInfo> SkillInfos = new List<SkillInfo>()
            {
                new SkillInfo(){id = 0, level = 0},
                new SkillInfo(){id = 10, level = 2},
                new SkillInfo(){id = 20, level = 3},
                new SkillInfo(){id = 30, level = 4},
            };

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
            ushort count = 0;
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

            // list
            Array.Copy(BitConverter.GetBytes((ushort)SkillInfos.Count), 0, s.Array, s.Offset + count, sizeof(ushort));
            count += sizeof(ushort);

            foreach (SkillInfo skillInfo in SkillInfos)
                skillInfo.Write(s, ref count);

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
            Name = Encoding.Unicode.GetString(s.Array, s.Offset + readIndex, nameLen);
            readIndex += nameLen;

            ushort skillLen = BitConverter.ToUInt16(s.Array, readIndex);
            readIndex += sizeof(ushort);
            SkillInfos.Clear();
            for (int i = 0; i < skillLen; i++)
            {
                SkillInfo skillInfo = new SkillInfo();
                skillInfo.Read(s, ref readIndex);
                SkillInfos.Add(skillInfo);
            }
        }
    }
    enum PacketType
    {
        PlayerInfoRequest = 0,
        PlayerInfoResult = 1,
    }
    class ClientSession : PacketSession
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
            int count = 0;
            ushort packetSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            count += 2;
            ushort packetId = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
            count += 2;

            switch ((PacketType)packetId)
            {
                case PacketType.PlayerInfoRequest:
                    var playerInfo = new PlayerInfoRequest();
                    playerInfo.Read(buffer);
                    Console.WriteLine($"아이디 : {playerInfo.PlayerId}, 닉네임 : {playerInfo.Name}");
                    foreach (var info in playerInfo.SkillInfos)
                    {
                        Console.WriteLine($"{info.id}, {info.level}");
                    }
                    break;
                case PacketType.PlayerInfoResult:
                    break;
            }
            Console.WriteLine($"크기 : {packetSize}, 패킷 아이디 : {packetId}");
        }

        public override void OnSend(int num)
        {
            Console.WriteLine($"바이트 크기는 {num}");
        }
    }
}
