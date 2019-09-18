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
using System.Runtime.InteropServices;

namespace Ekinar
{
    public class EkinarServer
    {
        private readonly Socket _mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static PacketHandler _serverPacketHandler = new PacketHandler("S");
        private static PacketHandler _clientPacketHandler = new PacketHandler("C");
        private static string _ekinarIp;
        private static int _ekinarPort;
        private static string _serverIp;
        private static int _serverPort;

        public void Start(string ekinarIp, int ekinarPort, string serverIp, int serverPort)
        {
            IPEndPoint ekinarEndPoint = new IPEndPoint(IPAddress.Parse(ekinarIp), ekinarPort);
            IPEndPoint officialEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
            _ekinarIp = ekinarIp;
            _ekinarPort = ekinarPort;
            _serverIp = serverIp;
            _serverPort = serverPort;

            _mainSocket.Bind(ekinarEndPoint);
            _mainSocket.Listen(10);
            Console.WriteLine("[Ekinar] Proxy server is running on {0}:{1}. Connecting to {2}:{3}", ekinarIp, ekinarPort, serverIp, serverPort);
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
                try
                {
                    if (state.SourceSocket.RemoteEndPoint.ToString().Contains(_serverPort.ToString())) // packet from server 
                    {
                        var receivedData = new byte[bytesRead];
                        Buffer.BlockCopy(state.Buffer, 0, receivedData, 0, bytesRead);
                        _serverPacketHandler.AnalyzePacket(receivedData);
                    }
                    else
                    {
                        var receivedData = new byte[bytesRead];
                        Buffer.BlockCopy(state.Buffer, 0, receivedData, 0, bytesRead);
                        _clientPacketHandler.AnalyzePacket(receivedData);
                    }

                    state.DestinationSocket.Send(state.Buffer, bytesRead, SocketFlags.None);

                    state.SourceSocket.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, OnDataReceive, state);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[Ekinar] " + ex.Message);
                    state.DestinationSocket.Close();
                    state.SourceSocket.Close();
                }
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
                Buffer = new byte[30000];
            }
        }
    }
}
