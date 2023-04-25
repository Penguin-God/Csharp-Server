using System;
using System.Collections.Generic;

namespace ServerCore
{
    internal class ReceiveBuffer
    {
        // [r w] [] [] [] [] [] [] [] [] []
        ArraySegment<byte> _buffer;
        int _readIndex;
        int _writeIndex;

        public ReceiveBuffer(int bufferSize) => _buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);

        public int DataSize => _writeIndex - _readIndex; // 아직 처리되지 않은 데이터 크기
        public int FreeSize => _buffer.Count - _writeIndex; // 버퍼 여유분

        // 처리되지 않은 데이터
        public ArraySegment<byte> ReadSegment => new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _readIndex, DataSize);
        // 여유분
        public ArraySegment<byte> WriteSegment => new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _writeIndex, FreeSize);

        public void Clear()
        {
            if (DataSize == 0)
                _readIndex = _writeIndex = 0;
            else
            {
                Array.Copy(_buffer.Array, _buffer.Offset + _readIndex, _buffer.Array, _buffer.Offset, DataSize);
                _readIndex = 0;
                _writeIndex = DataSize;
            }
        }

        public bool OnRead(int size)
        {
            if(size > DataSize) return false;

            _readIndex += size;
            return true;
        }

        public bool OnWrite(int size)
        {
            if (size > FreeSize) return false;

            _writeIndex += size;
            return true;
        }
    }
}
