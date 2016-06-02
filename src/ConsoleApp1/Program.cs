using System;
using System.Threading;
using System.Text;
using System.Net.Sockets;
using NetworkBase;
using NetworkBase.Events;
using NetworkBase.Events.Args;
using NetworkBase.Collections;
using System.Net;

namespace ConsoleApp1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var sock = new Socket(SocketType.Stream, ProtocolType.IP);
            try
            {
                var ipAddresses = Dns.GetHostAddressesAsync("ec2-54-209-126-92.compute-1.amazonaws.com").GetAwaiter().GetResult();
                if (ipAddresses.Length > 0)
                {
                    sock.ConnectAsync(new IPEndPoint(ipAddresses[0], 6789)).GetAwaiter().GetResult();
                }
                else
                {
                    sock.ConnectAsync(new IPEndPoint(new IPAddress(new byte[] { 127, 0, 0, 1 }), 6789)).GetAwaiter().GetResult();
                }
                var ev = new GameEvent(EGameEventID.Handshake, new ClientHandshake()
                {
                    game = 1,
                    instance = 1,
                }, null);
                var bytesSent = sock.SendEventAsync(ev).GetAwaiter().GetResult();

                var recvBuf = new byte[4096];
                var recvBufList = new FastList<byte>(recvBuf.Length * 2);
                bool recv = true;
                while (recv)
                {
                    var msgSize = sock.ReceiveAsync(new ArraySegment<byte>(recvBuf), SocketFlags.None).GetAwaiter().GetResult();
                    recvBufList.AddRange(recvBuf, msgSize);
                    GameEvent[] ges;
                    var bytesProcessed = recvBufList.Buffer.ParseGameEvents(recvBufList.Count, out ges);
                    recvBufList.RemoveRange(0, bytesProcessed);

                    if (ges.Length > 0)
                    {
                        Console.WriteLine($"Received {ges.Length} events.");
                        foreach (var ge in ges)
                        {
                            Console.WriteLine($"Event {ge.ID}.");
                            sock.SendEventAsync(new GameEvent(EGameEventID.BetSetHack, null, null)).GetAwaiter().GetResult();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }

            //var ws = new WebSocketClient();
            //var ws2 = ws.ConnectAsync(new Uri("ws://127.0.0.1:5000/hai"), CancellationToken.None).GetAwaiter().GetResult() as CommonWebSocket;
            //ws2.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("PLZ TOUCH ME")), WebSocketMessageType.Text, true, CancellationToken.None);
            Thread.Sleep(10000);
        }
    }
}
