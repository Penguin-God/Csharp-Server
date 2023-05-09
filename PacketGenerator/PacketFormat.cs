using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacketGenerator
{
    internal class PacketFormat
    {
        /// <summary>
        /// {0} : 패킷 이름
        /// {1} : 멤버 이름
        /// {2} : 멤버 write
        /// {3} : 멤버 read
        /// </summary>
        public static string packetFormat  =
@"
class {0}
{{
    {1}

    public ArraySegment<byte> Write()
    {{
        var s = SendBfferHelper.Open(4096);
        ushort count = 0;
        count += sizeof(ushort);
        Array.Copy(BitConverter.GetBytes(PacketType.{0}), 0, s.Array, s.Offset + count, sizeof(ushort));
        count += sizeof(ushort);

        {2}

        // 마지막에는 패킷 크기 넣음
        byte[] size = BitConverter.GetBytes(count);
        Array.Copy(size, 0, s.Array, s.Offset, 2);

        return SendBfferHelper.Close(count);
    }}

    public void Read(ArraySegment<byte> s)
    {{
        ushort readIndex = 0;
        readIndex += sizeof(ushort);
        readIndex += sizeof(ushort);
        {3}
    }}
}}

";

        /// <summary>
        /// {0} : 변수 형식
        /// {1} : 변수 이름
        /// </summary>
        public static string memberFormat = "public {0} {1};";

        // 구조체 List로 변환함
        /// <summary>
        /// 
        /// </summary>
        public static string memberListFormat =
@"

";

        /// <summary>
        /// {0} : 변수 이름
        /// {1} : 변수 형식
        /// </summary>
        public static string writeFormat = "Array.Copy({0}, 0, s.Array, s.Offset + count, sizeof({1})); count += sizeof({1});";

        /// <summary>
        /// {0} : 변수 이름
        /// </summary>
        public static string writeStringFormat =
@"
ushort {0}Len = (ushort)Encoding.Unicode.GetByteCount({0});
Array.Copy(BitConverter.GetBytes({0}Len), 0, s.Array, s.Offset + count, 2);
count += sizeof(ushort);

Array.Copy(Encoding.Unicode.GetBytes(this.{0}), 0, s.Array, s.Offset + count, {0}Len);
count += {0}Len;
";

        /// <summary>
        /// {0} : 변수 이름
        /// {1} : 변수 크기에 따른 변환 함수 이름
        /// {2} : 변수 형식
        /// </summary>
        public static string readFormat = "this.{0} = BitConverter.{1}(s.Array, s.Offset + readIndex); readIndex += sizeof({2})";

        /// <summary>
        /// {0} : 변수 이름
        /// </summary>
        public static string readStringFormat =
@"
ushort {0}Len = BitConverter.ToUInt16(s.Array, readIndex);
readIndex += sizeof(ushort);
{0} = Encoding.Unicode.GetString(new ArraySegment<byte>(s.Array, readIndex, {0}Len).Array);
";


    }
}
