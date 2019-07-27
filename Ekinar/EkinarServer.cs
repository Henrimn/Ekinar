using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Ekinar.Core;

namespace Ekinar
{
    public class EkinarServer
    {
        private readonly Socket _mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public static ObservableCollection<Packet> completedPacketList = new ObservableCollection<Packet>();
        private static PacketReassembler _reassemblerServer = new PacketReassembler();
        private static PacketReassembler _reassemblerClient = new PacketReassembler();

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
            
            try
            {
                var bytesRead = state.SourceSocket.EndReceive(result);

                if (bytesRead > 0)
                {
                    var receivedData = new byte[bytesRead];
                    Buffer.BlockCopy(state.Buffer, 0, receivedData, 0, bytesRead);

                    if (state.SourceSocket.RemoteEndPoint.ToString().Contains("27015")) // packet from server 
                    {
                        var completedBuffer = _reassemblerServer.AnalyzePacket(receivedData);
                        if (completedBuffer != null)
                        {
                            var packet = new Packet(completedBuffer);
                            completedPacketList.Add(packet);
                            Console.WriteLine("[Server] Packet from: {0} Opcode: {1} Length: {2}.", state.SourceSocket.RemoteEndPoint, packet.Opcode, packet.Buffer.Length);
                        }
                    }
                    else // packet from client
                    {
                        var completedBuffer = _reassemblerClient.AnalyzePacket(receivedData);
                        if (completedBuffer != null)
                        {
                            var packet = new Packet(completedBuffer);
                            completedPacketList.Add(packet);
                            Console.WriteLine("[Client] Packet from: {0} Opcode: {1} Length: {2}.", state.SourceSocket.RemoteEndPoint, packet.Opcode, packet.Buffer.Length);
                        }
                    }

                    state.DestinationSocket.Send(state.Buffer, bytesRead, SocketFlags.None);

                    state.SourceSocket.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, OnDataReceive, state);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _reassemblerClient.Dispose();
                _reassemblerServer.Dispose();
                state.DestinationSocket.Close();
                state.SourceSocket.Close();
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
