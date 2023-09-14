using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.WebSockets;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using System.Text;
using serverapiorg.Models;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Net.Sockets;

namespace serverapiorg.Models
{
    public class WSServer
    {
        private HttpListener _listener;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private int _bufferSize = 1024;
        private static ConcurrentDictionary<string, WebSocket> Users = new ConcurrentDictionary<string, WebSocket>();

        public async Task Start(string url) 
        {
            IsItServerRuning();
            
            _listener = new HttpListener();
            _listener.Prefixes.Add(url);
            _listener.Start();

            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var context = await _listener.GetContextAsync();
                if (context.Request.IsWebSocketRequest)
                {
                    string user = context.Request.QueryString["user"];
                    if (user == null) 
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                        return;
                    }
                    await HandleWebSocketSession(user, await context.AcceptWebSocketAsync(null));
                }
                else {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
        }

        public async void Stop() 
        {
            _cancellationTokenSource.Cancel();
            await CloseAllWebsocketConnectionAsync();
            _listener.Stop();
            _listener = null;
        }

        private async Task HandleWebSocketSession(string user, HttpListenerWebSocketContext context) 
        { 
            var socket = context.WebSocket;
            ArraySegment<byte> buffer = CreateBuffer();

            if (!Users.TryAdd(user, socket)) 
            {
                await SendMessageAsync(socket, "You are already connecting...");
                return;
            }

            try {
                while (socket.State == WebSocketState.Open)
                {
                    var receiveResult = await socket.ReceiveAsync(buffer, CancellationToken.None);

                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        await CloseWebsocketConnection(socket, user);
                        return;
                    }

                    WebSocket connectingUser = Users[user];
                    await SendMessageAsync(socket, "You have successfully connected!");
                }
            } catch(Exception error) {
                throw new InvalidOperationException(error.ToString());
            }
        }

        private void IsItServerRuning() {
            if (_listener != null && _listener.IsListening) 
            {
                throw new InvalidOperationException("Server is runing...");
            }
        }

        public void SetBufferSize(int size = 1024) 
        {
            _bufferSize = size;
        }

        private ArraySegment<byte> CreateBuffer() 
        {
            return new ArraySegment<byte>(new byte[_bufferSize]);
        }

        private async Task SendMessageAsync(WebSocket socket, string message) 
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            var segment = new ArraySegment<byte>(buffer);
            await socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task CloseWebsocketConnection(WebSocket socket, string user) 
        {
            await SendMessageAsync(socket, "connection is already disconnecting...");
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server acknowledged the close request.", CancellationToken.None);
            Users.TryRemove(user, out _);
        }

        private async Task CloseAllWebsocketConnectionAsync() {
            foreach (var user in Users) {
                if (user.Value.State == WebSocketState.Open) 
                {
                    await user.Value.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server is shutting down.", CancellationToken.None);
                }
                Users.Clear();
            }
        }
    }
}