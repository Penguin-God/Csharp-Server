using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    public static class SendBfferHelper
    {
        public static ThreadLocal<SendBuffer> CurrentBuffer = new ThreadLocal<SendBuffer>(() => null);
        public static int ChunkSize { get; set; } = 4096 * 100;
        public static ArraySegment<byte> Open(int reserveSize)
        {
            if (CurrentBuffer.Value == null)
                CurrentBuffer.Value = new SendBuffer(ChunkSize);

            if(reserveSize > CurrentBuffer.Value.FreeSize)
                CurrentBuffer.Value = new SendBuffer(ChunkSize);

            return CurrentBuffer.Value.Open(reserveSize);
        }

        public static ArraySegment<byte> Close(int usedSize) => CurrentBuffer.Value.Close(usedSize);
    }
    public class SendBuffer
    {
        // [u] [] [] [] [] [] [] [] [] []
        byte[] _buffer;
        int _usedIndex;
        
        public SendBuffer(int chunkSize) => _buffer = new byte[chunkSize];

        public int FreeSize => _buffer.Length - _usedIndex;

        public ArraySegment<byte> Open(int reserveSize) => new ArraySegment<byte>(_buffer, _usedIndex, reserveSize);

        public ArraySegment<byte> Close(int usedSize)
        {
            var result = new ArraySegment<byte>(_buffer, _usedIndex, usedSize);
            _usedIndex += usedSize;
            return result;
        }
    }
}
