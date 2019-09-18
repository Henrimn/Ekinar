using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Ekinar.Utilities;

namespace Ekinar.Core
{
    public class PacketHandler
    {
        private byte[] _buffer;
        private byte[] _decryptedBuffer;
        private byte[] _newPacketBuffer;
        private bool _needSalt;
        private string _direction;
        public Transformer _transformer;
        private const int WM_COPYDATA = 0x004A;

        public PacketHandler(string direction)
        {
            _direction = direction;
            _buffer = null;
            _decryptedBuffer = null;
            _newPacketBuffer = null;
            _needSalt = true;
            _transformer = new Transformer();
        }

        public void AnalyzePacket(byte[] data)
        {
            this.DecryptPacketBuffer(data);
        }

        /// <summary>
        /// Decrypt packet buffer
        /// </summary>
        /// <param name="data">encrypted packet buffer</param>
        private void DecryptPacketBuffer(byte[] data)
        {
            _buffer = data;

            if (_needSalt)
            {
                // set salt
                _transformer.Decrypt(_buffer, IPAddress.NetworkToHostOrder(BitConverter.ToInt64(_buffer, 0)));
                _decryptedBuffer = _buffer;
                _needSalt = false;
            }
            else
            {
                _transformer.Decrypt(_buffer);

                // append current buffer to the leftover part of previous buffer
                _decryptedBuffer = new byte[_newPacketBuffer.Length + _buffer.Length];
                Buffer.BlockCopy(_newPacketBuffer, 0, _decryptedBuffer, 0, _newPacketBuffer.Length);
                Buffer.BlockCopy(_buffer, 0, _decryptedBuffer, _newPacketBuffer.Length, _buffer.Length);

            }

            this.ReassemblingRawPacket(_decryptedBuffer);
        }

        /// <summary>
        /// Reassembling decrypted packet buffer
        /// </summary>
        /// <param name="data">decrypted buffer</param>
        private void ReassemblingRawPacket(byte[] data)
        {
            if (data == null)
                return;

            var tempBuffer = new byte[data.Length];
            Buffer.BlockCopy(data, 0, tempBuffer, 0, data.Length);

            while (true)
            {
                // get packet length
                var packetFullLength = this.GetExpectedLength(tempBuffer);

                // if current buffer not = real packet length
                if (packetFullLength == -1 || tempBuffer.Length < packetFullLength)
                {
                    _newPacketBuffer = new byte[tempBuffer.Length];
                    Buffer.BlockCopy(tempBuffer, 0, _newPacketBuffer, 0, tempBuffer.Length);
                    break;
                }
                else
                {
                    // get all bytes of the completed packet
                    var packetBuffer = new byte[packetFullLength];
                    Buffer.BlockCopy(tempBuffer, 0, packetBuffer, 0, packetFullLength);
                    byte[] direction = System.Text.Encoding.ASCII.GetBytes(_direction);
                    var sendData = new byte[1 + packetBuffer.Length];
                    Buffer.BlockCopy(direction, 0, sendData, 0, 1);
                    Buffer.BlockCopy(packetBuffer, 0, sendData, 1, packetBuffer.Length);
                    IntPtr hWnd = FindWindow(null, "VinSeek");
                    if (hWnd == null)
                    {
                        Console.WriteLine("VinSeek not found - cannot send data.");
                    }
                    else
                    {
                        SendMessage(hWnd, sendData, 0, sendData.Length);
                    }

                    // copy all bytes belong to the new packet to tempBuffer, for the loop to continue
                    var tempBuffer2 = new byte[tempBuffer.Length - packetFullLength];
                    Buffer.BlockCopy(tempBuffer, packetFullLength, tempBuffer2, 0, tempBuffer2.Length);
                    tempBuffer = new byte[tempBuffer2.Length];
                    Buffer.BlockCopy(tempBuffer2, 0, tempBuffer, 0, tempBuffer2.Length);
                }
            }
        }
        
        /// <summary>
        /// Get packet body length from decrypted packet buffer
        /// </summary>
        /// <param name="buffer">decrypted packet buffer</param>
        /// <returns>packet body length</returns>
        private int GetExpectedLength(byte[] buffer)
        {
            // sizeof(long) + opcode byte
            if (buffer.Length < sizeof(long) + 1)
                return -1;

            try
            {
                var packetOpcode = Util.ReadVarInt(buffer, sizeof(long), out int opcodeBytesCount);
                if (buffer.Length < sizeof(long) + opcodeBytesCount)
                    return -1;

                var packetBodyLength = Util.ReadVarInt(buffer, sizeof(long) + opcodeBytesCount, out int lengthBytesCount);

                // return packet full length
                return sizeof(long) + opcodeBytesCount + lengthBytesCount + packetBodyLength;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region VinSeek interops
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(String lpClassName, String lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        static IntPtr SendMessage(IntPtr hWnd, byte[] array, int startIndex, int length)
        {
            IntPtr ptr = Marshal.AllocHGlobal(IntPtr.Size * 3 + length);
            Marshal.WriteIntPtr(ptr, 0, IntPtr.Zero);
            Marshal.WriteIntPtr(ptr, IntPtr.Size, (IntPtr)length);
            IntPtr dataPtr = new IntPtr(ptr.ToInt64() + IntPtr.Size * 3);
            Marshal.WriteIntPtr(ptr, IntPtr.Size * 2, dataPtr);
            Marshal.Copy(array, startIndex, dataPtr, length);
            IntPtr result = SendMessage(hWnd, WM_COPYDATA, IntPtr.Zero, ptr);
            Marshal.FreeHGlobal(ptr);
            return result;
        }
        #endregion
    }
}
