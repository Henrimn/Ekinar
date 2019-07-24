using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace Ekinar
{
    public class EkinarServer
    {
        private readonly Socket _mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        public void Start(IPEndPoint ekinarEndPoint, IPEndPoint officialEndPoint)
        {
            _mainSocket.Bind(ekinarEndPoint);
            _mainSocket.Listen(10);

            while (true)
            {
                var source = _mainSocket.Accept(); // create socket from client to ekinar
                var destination = new EkinarServer();
                var state = new State(source, destination._mainSocket); // bridge from client-ekinar socket to ekinar-official socket
                destination.Connect(officialEndPoint, source); // create socket from ekinar to official server
                source.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, OnDataReceive, state);
            }
        }

        private void Connect(EndPoint remoteEndpoint, Socket destination)
        {
            var state = new State(_mainSocket, destination); // bridge from official-ekinar socket to ekinar-client socket
            _mainSocket.Connect(remoteEndpoint);
            _mainSocket.BeginReceive(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, OnDataReceive, state);
        }

        private static void OnDataReceive(IAsyncResult result)
        {
            var state = (State)result.AsyncState;

            var bytesRead = state.SourceSocket.EndReceive(result);
            if (bytesRead > 0)
            {
                state.DestinationSocket.Send(state.Buffer, bytesRead, SocketFlags.None);
                Console.WriteLine("Packet from: " + state.SourceSocket.RemoteEndPoint.ToString() + ". Length: " + bytesRead);

                byte[] data = new byte[bytesRead];
                Buffer.BlockCopy(state.Buffer, 0, data, 0, bytesRead);

                // Decrypt before accepting new data
                //PacketHandler.Decrypt(PacketHandler._Transformer(), data, data.Length);

                Console.WriteLine("Data: \n" + BitConverter.ToString(data));
                state.SourceSocket.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, OnDataReceive, state);
            }
        }

        private class State
        {
            public Socket SourceSocket { get; private set; }
            public Socket DestinationSocket { get; private set; }
            public byte[] Buffer { get; private set; }

            public State(Socket source, Socket destination)
            {
                SourceSocket = source;
                DestinationSocket = destination;
                Buffer = new byte[1460];
            }
        }
    }
}
