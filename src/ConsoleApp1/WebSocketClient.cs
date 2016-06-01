using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebSockets.Protocol;

namespace Microsoft.AspNetCore.WebSockets.Client
{
    internal class WebSocketHttpRequestCreator : IWebRequestCreate
    {
        private string m_httpScheme;
        private static readonly Random s_KeyGenerator = new Random();

        // This ctor is used to create a WebSocketHttpRequestCreator.
        // We will do a URI change to update the scheme with Http or Https scheme. The usingHttps boolean is 
        // used to indicate whether the created HttpWebRequest should take the https scheme or not.
        public WebSocketHttpRequestCreator(bool usingHttps)
        {
            m_httpScheme = usingHttps ? "https" : "http";
        }

        /*++

         Create - Create an HttpWebRequest.

            This is our method to create an HttpWebRequest for WebSocket connection. We register
            We will register it for custom Uri prefixes. And this method is called when a request
            needs to be created for one of those. The created HttpWebRequest will still be with Http or Https
            scheme, depending on the m_httpScheme field of this object.


            Input:
                    Uri             - Uri for request being created.

            Returns:
                    The newly created HttpWebRequest for WebSocket connection.

         --*/

        public WebRequest Create(Uri Uri)
        {
            UriBuilder uriBuilder = new UriBuilder(Uri);
            uriBuilder.Scheme = m_httpScheme;
            var request = WebRequest.CreateHttp(uriBuilder.Uri);

            request.Headers[Constants.Headers.Connection] = Constants.Headers.Upgrade;
            request.Headers[Constants.Headers.ConnectionUpgrade] = Constants.Headers.UpgradeWebSocket;
            byte[] keyBlob = new byte[16];
            lock (s_KeyGenerator)
            {
                s_KeyGenerator.NextBytes(keyBlob);
            }

            request.Headers[Constants.Headers.SecWebSocketKey] = Convert.ToBase64String(keyBlob);

            //HttpWebRequest request = new HttpWebRequest(uriBuilder.Uri, null, true, "WebSocket" + Guid.NewGuid());
            //WebSocketHelpers.PrepareWebRequest(ref request);
            return request;
        }

    } // class WebSocketHttpRequestCreator

    public class WebSocketClient
    {
        static WebSocketClient()
        {
            try
            {
                // Only call once
                //WebSocket.RegisterPrefixes();
                WebRequest.RegisterPrefix("ws" + ":", new WebSocketHttpRequestCreator(false));
                WebRequest.RegisterPrefix("wss" + ":", new WebSocketHttpRequestCreator(true));
            }
            catch (Exception)
            {
                // Already registered
            }
        }

        public WebSocketClient()
        {
            ReceiveBufferSize = 1024 * 16;
            KeepAliveInterval = TimeSpan.FromMinutes(2);
            SubProtocols = new List<string>();
        }

        public IList<string> SubProtocols
        {
            get;
            private set;
        }

        public TimeSpan KeepAliveInterval
        {
            get;
            set;
        }

        public int ReceiveBufferSize
        {
            get;
            set;
        }

        public bool UseZeroMask
        {
            get;
            set;
        }

        public Action<HttpWebRequest> ConfigureRequest
        {
            get;
            set;
        }

        public Action<HttpWebResponse> InspectResponse
        {
            get;
            set;
        }

        public async Task<WebSocket> ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);

            request.Proxy = null;
            CancellationTokenRegistration cancellation = cancellationToken.Register(() => request.Abort());

            request.Headers[Constants.Headers.SecWebSocketVersion] = Constants.Headers.SupportedVersion;
            if (SubProtocols.Count > 0)
            {
                request.Headers[Constants.Headers.SecWebSocketProtocol] = string.Join(", ", SubProtocols);
            }

            if (ConfigureRequest != null)
            {
                ConfigureRequest(request);
            }

            HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();

            cancellation.Dispose();

            InspectResponse?.Invoke(response);

            // TODO: Validate handshake
            HttpStatusCode statusCode = response.StatusCode;
            if (statusCode != HttpStatusCode.SwitchingProtocols)
            {
                response.Dispose();
                throw new InvalidOperationException("Incomplete handshake, invalid status code: " + statusCode);
            }
            // TODO: Validate Sec-WebSocket-Key/Sec-WebSocket-Accept

            string subProtocol = response.Headers[Constants.Headers.SecWebSocketProtocol];
            if (!string.IsNullOrEmpty(subProtocol) && !SubProtocols.Contains(subProtocol, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Incomplete handshake, the server specified an unknown sub-protocol: " + subProtocol);
            }

            Stream stream = response.GetResponseStream();

            return CommonWebSocket.CreateClientWebSocket(stream, subProtocol, KeepAliveInterval, ReceiveBufferSize, useZeroMask: UseZeroMask);
        }
    }
}