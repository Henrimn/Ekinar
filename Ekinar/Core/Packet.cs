using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Ekinar.Utilities;

namespace Ekinar.Core
{
    public class Packet
    {
        public byte[] Buffer {get; private set; }
        
        public unsafe long InstanceId
        {
            get
            {
                fixed (byte* numRef = &(Buffer[0]))
                {
                    return IPAddress.NetworkToHostOrder(*((long*)numRef));
                }
            }
            set
            {
                fixed (byte* numRef = &(Buffer[0]))
                {
                    *((long*)numRef) = IPAddress.HostToNetworkOrder(value);
                }
            }
        }

        public int Opcode
        {
            get
            {
                var opCode = Util.GetInt32(Buffer, 8);
                return opCode;
            }
            private set
            {
                Util.GetBytes(value, Buffer, 8);
            }
        }

        private int LengthOffset
        {
            get
            {
                return 8 + Util.GetBytesCount(Buffer, 8);
            }
        }

        public int Length
        {
            get
            {
                return Util.GetInt32(Buffer, LengthOffset);
            }
            private set
            {
                Util.GetBytes(value, Buffer, LengthOffset);
            }
        }

        public int BodyOffset
        {
            get
            {
                int lengthOffset = LengthOffset;
                return lengthOffset + Util.GetBytesCount(Buffer, lengthOffset);
            }
        }

        public byte[] Body
        {
            get
            {
                var buffer = new byte[Buffer.Length];
                System.Buffer.BlockCopy(Buffer, BodyOffset - 1, buffer, 0, Buffer.Length);

                return buffer;
            }
        }

        public Packet(byte[] buffer)
        {
            Buffer = buffer;
        }
    }
}
