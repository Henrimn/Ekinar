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
        private static PacketReassembler _reassemblerServer = new PacketReassembler();
        private static PacketReassembler _reassemblerClient = new PacketReassembler();
        private static string _ekinarIp;
        private static int _ekinarPort;
        private static string _serverIp;
        private static int _serverPort;
        private const int WM_COPYDATA = 0x004A;
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
            try
            {
                var bytesRead = state.SourceSocket.EndReceive(result);
                if (bytesRead > 0)
                {
                    if (state.SourceSocket.RemoteEndPoint.ToString().Contains(_serverPort.ToString())) // packet from server 
                    {
                        var receivedData = new byte[bytesRead];
                        Buffer.BlockCopy(state.Buffer, 0, receivedData, 0, bytesRead);

                        var completedBuffer = _reassemblerServer.AnalyzePacket(receivedData);
                        if (completedBuffer != null)
                        {
                            var packet = new Packet(completedBuffer);

                            byte[] direction = System.Text.Encoding.ASCII.GetBytes("S");
                            var sendData = new byte[1 + completedBuffer.Length];
                            Buffer.BlockCopy(direction, 0, sendData, 0, 1);
                            Buffer.BlockCopy(completedBuffer, 0, sendData, 1, completedBuffer.Length);

                            IntPtr hWnd = FindWindow(null, "VinSeek");
                            if (hWnd == null)
                            {
                                Console.WriteLine("VinSeek not found - cannot send data.");
                            }
                            else
                            {
                                SendMessage(hWnd, sendData, 0, sendData.Length);
                            }
                            Console.WriteLine("[Server] Opcode: {0} Length: {1}.", packet.Opcode, packet.Buffer.Length);
                        }
                    }
                    else
                    {
                        var receivedData = new byte[bytesRead];
                        Buffer.BlockCopy(state.Buffer, 0, receivedData, 0, bytesRead);

                        var completedBuffer = _reassemblerClient.AnalyzePacket(receivedData);
                        if (completedBuffer != null)
                        {
                            var packet = new Packet(completedBuffer);

                            byte[] direction = System.Text.Encoding.ASCII.GetBytes("C");
                            var sendData = new byte[1 + completedBuffer.Length];
                            Buffer.BlockCopy(direction, 0, sendData, 0, 1);
                            Buffer.BlockCopy(completedBuffer, 0, sendData, 1, completedBuffer.Length);

                            IntPtr hWnd = FindWindow(null, "VinSeek");
                            if (hWnd == null)
                            {
                                Console.WriteLine("VinSeek not found - cannot send data.");
                            }
                            else
                            {
                                SendMessage(hWnd, sendData, 0, sendData.Length);
                            }
                            Console.WriteLine("[Client] Opcode: {0} Length: {1}.", packet.Opcode, packet.Buffer.Length);
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
                Buffer = new byte[20000]; // read a lot because why not
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
