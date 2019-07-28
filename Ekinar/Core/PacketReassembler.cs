using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Ekinar.Utilities;

namespace Ekinar.Core
{
    public class PacketReassembler : IDisposable
    {
        private byte[] _buffer;
        private byte[] _packetBuffer;
        private byte[] _decryptedBuffer;
        private byte[] _newPacketBuffer = null;
        private int _packetLength;

        private Transformer _transformer = new Transformer();

        public byte[] AnalyzePacket(byte[] data)
        {
            if (PacketReassembled(data))
                return _packetBuffer;
            else
                return null;
        }

        private unsafe bool PacketReassembled(byte[] data)
        {
            _buffer = data;

            if (_buffer.Length < 9)
                return false;
            
            if (_newPacketBuffer == null)
            {
                _transformer = new Transformer();

                var salt = new byte[8];
                Buffer.BlockCopy(_buffer, 0, salt, 0, 8);
                if (_transformer.Decrypt(salt, IPAddress.NetworkToHostOrder(BitConverter.ToInt64(salt, 0))))
                {
                    // not necessary but ok
                    fixed (byte* numPtr = &salt[0])
                        *(long*)numPtr = 0L;
                }

                var body = new byte[_buffer.Length - 8];
                Buffer.BlockCopy(_buffer, 8, body, 0, body.Length);

                _transformer.Decrypt(body);

                _decryptedBuffer = new byte[_buffer.Length];
                Buffer.BlockCopy(salt, 0, _decryptedBuffer, 0, 8);
                Buffer.BlockCopy(body, 0, _decryptedBuffer, 8, body.Length);
            }
            else
            {
                _transformer.Decrypt(_buffer);
                _decryptedBuffer = new byte[_newPacketBuffer.Length + _buffer.Length];
                Buffer.BlockCopy(_newPacketBuffer, 0, _decryptedBuffer, 0, _newPacketBuffer.Length);
                Buffer.BlockCopy(_buffer, 0, _decryptedBuffer, _newPacketBuffer.Length, _buffer.Length);

            }

            // get packet length
            _packetLength = this.GetExpectedLength(_decryptedBuffer);
            
            // if current buffer not = real packet length
            if (_decryptedBuffer.Length < _packetLength)
            {
                _newPacketBuffer = _decryptedBuffer;
                return false;
            }

            // get all bytes of the completed packet
            _packetBuffer = new byte[_packetLength];
            Buffer.BlockCopy(_decryptedBuffer, 0, _packetBuffer, 0, _packetLength);

            // copy all bytes belong to the new packet to another buffer
            var leftLength = _decryptedBuffer.Length - _packetLength;

            _newPacketBuffer = new byte[_decryptedBuffer.Length - _packetLength];
            Buffer.BlockCopy(_decryptedBuffer, _packetLength, _newPacketBuffer, 0, _newPacketBuffer.Length);

            return true;
        }

        public int GetExpectedLength(byte[] buffer)
        {
            var decryptedBuffer = buffer;
            var opcodeBytesCount = Util.GetBytesCount(decryptedBuffer, 8);
            var lengthBytesCount = Util.GetBytesCount(decryptedBuffer, 8 + opcodeBytesCount);
            var packetBodyLength = Util.GetInt32(decryptedBuffer, 8 + opcodeBytesCount);
            var packetLength = sizeof(long) + opcodeBytesCount + lengthBytesCount + packetBodyLength;

            return packetLength;
        }

        public void Dispose()
        {
            _buffer = null;
            _newPacketBuffer = null;
            _packetBuffer = null;
        }
    }
}
