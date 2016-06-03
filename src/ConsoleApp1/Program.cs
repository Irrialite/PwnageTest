using System;
using System.Threading;
using System.Text;
using System.Net.Sockets;
using NetworkBase;
using NetworkBase.Events;
using NetworkBase.Events.Args;
using NetworkBase.Collections;
using System.Net;
using System.IO;
using Serilog;
using Serilog.Sinks.RollingFile;

namespace ConsoleApp1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var hostName = "ec2-52-87-205-193.compute-1.amazonaws.com";
            var exePath = Directory.GetCurrentDirectory();
            var port = 6789;

            Directory.CreateDirectory(exePath + "/log");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(Serilog.Events.LogEventLevel.Verbose)
                .Enrich.FromLogContext()
                .WriteTo.RollingFile($"{exePath}/log/{{Date}}-log.txt", fileSizeLimitBytes: 10 * 1024 * 1024)
                .WriteTo.LiterateConsole()
                .CreateLogger();

            var sock = new Socket(SocketType.Stream, ProtocolType.IP);
            try
            {
                var ipAddresses = Dns.GetHostAddressesAsync(hostName).GetAwaiter().GetResult();
                Log.Logger.Debug($"Got {ipAddresses.Length} IPs for {hostName}.");
                if (ipAddresses.Length > 0)
                {
                    Log.Logger.Debug($"Connecting to {ipAddresses[0]} at {port}.");
                    sock.ConnectAsync(new IPEndPoint(ipAddresses[0], port)).GetAwaiter().GetResult();
                }
                else
                {
                    var localhost = new IPAddress(new byte[] { 127, 0, 0, 1 });
                    Log.Logger.Debug($"Connecting to {localhost} at {port}.");
                    sock.ConnectAsync(new IPEndPoint(localhost, port)).GetAwaiter().GetResult();
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
                        Log.Logger.Information($"Received {ges.Length} events.");
                        foreach (var ge in ges)
                        {
                            Log.Logger.Information($"Event {ge.ID}.");
                            sock.SendEventAsync(new GameEvent(EGameEventID.BetSetHack, null, null)).GetAwaiter().GetResult();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "Error occurred.");
            }

            //var ws = new WebSocketClient();
            //var ws2 = ws.ConnectAsync(new Uri("ws://127.0.0.1:5000/hai"), CancellationToken.None).GetAwaiter().GetResult() as CommonWebSocket;
            //ws2.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("PLZ TOUCH ME")), WebSocketMessageType.Text, true, CancellationToken.None);
            Thread.Sleep(10000);
        }
    }
}
