namespace Microsoft.AspNetCore.WebSockets.Client
{
    public static class Constants
    {
        public static class Headers
        {
            public const string Upgrade = "Upgrade";
            public const string UpgradeWebSocket = "websocket";
            public const string Connection = "Connection";
            public const string ConnectionUpgrade = "Upgrade";
            public const string SecWebSocketKey = "Sec-WebSocket-Key";
            public const string SecWebSocketVersion = "Sec-WebSocket-Version";
            public const string SecWebSocketProtocol = "Sec-WebSocket-Protocol";
            public const string SecWebSocketAccept = "Sec-WebSocket-Accept";
            public const string SupportedVersion = "13";
        }
    }
}