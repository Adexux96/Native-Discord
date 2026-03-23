using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NativeDiscord.Models;

namespace NativeDiscord.Services
{
    public class DiscordGatewayService
    {
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly Uri GatewayUrl = new Uri("wss://gateway.discord.gg/?v=10&encoding=json");
        private string _token;
        private int? _sequenceNumber;
        private int _heartbeatInterval;
        
        // Events
        public event EventHandler<PresenceUpdate> OnPresenceUpdate;
        public event EventHandler<Message> OnMessageCreate;
        public event EventHandler<Message> OnMessageUpdate;
        public event EventHandler<MessageDeletedPayload> OnMessageDelete;
        public event EventHandler<VoiceState> OnVoiceStateUpdate;
        public event EventHandler<TypingStartPayload> OnTypingStart;
        public event EventHandler<MessageReactionUpdatePayload> OnMessageReactionAdd;
        public event EventHandler<MessageReactionUpdatePayload> OnMessageReactionRemove;
        public event EventHandler<ReadyPayload> OnReady;

        public bool IsConnected => _webSocket != null && _webSocket.State == WebSocketState.Open;

        public DiscordGatewayService()
        {
        }

        public async Task ConnectAsync(string token)
        {
            _token = token;
            _cancellationTokenSource = new CancellationTokenSource();
            _webSocket = new ClientWebSocket();

            try
            {
                await _webSocket.ConnectAsync(GatewayUrl, _cancellationTokenSource.Token);
                _ = ReceiveLoop();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Gateway Connection Error: {ex.Message}");
            }
        }

        public void Disconnect()
        {
             // Synchronous fallback
             _ = DisconnectAsync();
        }

        public async Task DisconnectAsync()
        {
            _cancellationTokenSource?.Cancel();
            var webSocket = _webSocket;
            var cancellationTokenSource = _cancellationTokenSource;

            try
            {
                if (webSocket != null &&
                    (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived))
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Logout", CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch { }
            finally
            {
                webSocket?.Dispose();
                cancellationTokenSource?.Dispose();

                if (ReferenceEquals(_webSocket, webSocket)) _webSocket = null;
                if (ReferenceEquals(_cancellationTokenSource, cancellationTokenSource)) _cancellationTokenSource = null;

                _sequenceNumber = null;
            }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[8192];

            try
            {
                while (_webSocket.State == WebSocketState.Open && !_cancellationTokenSource.IsCancellationRequested)
                {
                    using (var ms = new System.IO.MemoryStream())
                    {
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);
                            
                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                                return;
                            }
                            
                            ms.Write(buffer, 0, result.Count);
                            
                        } while (!result.EndOfMessage);

                        ms.Seek(0, System.IO.SeekOrigin.Begin);
                        
                        using (var reader = new System.IO.StreamReader(ms, Encoding.UTF8))
                        {
                            var json = await reader.ReadToEndAsync();
                            ProcessPayload(json);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Gateway Receive Error: {ex}");
            }
        }

        private static JsonSerializerOptions DefaultOptions => new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            TypeInfoResolver = Helpers.DiscordJsonContext.Default
        };

        private void ProcessPayload(string json)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    JsonElement root = doc.RootElement;
                    int op = root.GetProperty("op").GetInt32();
                    
                    // Update sequence if present
                    if (root.TryGetProperty("s", out JsonElement seqElement) && seqElement.ValueKind == JsonValueKind.Number)
                    {
                        _sequenceNumber = seqElement.GetInt32();
                    }

                    switch (op)
                    {
                        case 10: // Hello
                            var hello = JsonSerializer.Deserialize<GatewayHello>(root.GetProperty("d").GetRawText(), DefaultOptions);
                            _heartbeatInterval = hello.HeartbeatInterval;
                            StartHeartbeat();
                            SendIdentify();
                            break;
                            
                        case 11: // Heartbeat ACK
                            // Received ACK
                            break;
                            
                        case 0: // Dispatch
                            string eventName = root.GetProperty("t").GetString();
                            HandleDispatch(eventName, root.GetProperty("d"));
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Payload Processing Error: {ex}");
            }
        }

        private void HandleDispatch(string eventName, JsonElement data)
        {
            System.Diagnostics.Debug.WriteLine($"Gateway Dispatch: {eventName}");
            switch (eventName)
            {
                case "READY":
                    var ready = JsonSerializer.Deserialize<ReadyPayload>(data.GetRawText(), DefaultOptions);
                    OnReady?.Invoke(this, ready);
                    System.Diagnostics.Debug.WriteLine($"Gateway READY. Presences: {ready?.Presences?.Count ?? 0}");
                    break;

                case "PRESENCE_UPDATE":
                    var presence = JsonSerializer.Deserialize<PresenceUpdate>(data.GetRawText(), DefaultOptions);
                    OnPresenceUpdate?.Invoke(this, presence);
                    break;

                case "MESSAGE_CREATE":
                    var message = JsonSerializer.Deserialize<Message>(data.GetRawText(), DefaultOptions);
                    OnMessageCreate?.Invoke(this, message);
                    break;

                case "MESSAGE_UPDATE":
                    var updatedMessage = JsonSerializer.Deserialize<Message>(data.GetRawText(), DefaultOptions);
                    OnMessageUpdate?.Invoke(this, updatedMessage);
                    break;

                case "MESSAGE_DELETE":
                    var deletedMessage = JsonSerializer.Deserialize<MessageDeletedPayload>(data.GetRawText(), DefaultOptions);
                    OnMessageDelete?.Invoke(this, deletedMessage);
                    break;
                    
                case "VOICE_STATE_UPDATE":
                    var voiceState = JsonSerializer.Deserialize<VoiceState>(data.GetRawText(), DefaultOptions);
                    OnVoiceStateUpdate?.Invoke(this, voiceState);
                    break;

                case "TYPING_START":
                    var typing = JsonSerializer.Deserialize<TypingStartPayload>(data.GetRawText(), DefaultOptions);
                    OnTypingStart?.Invoke(this, typing);
                    break;
                    
                case "MESSAGE_REACTION_ADD":
                    var reactionAdd = JsonSerializer.Deserialize<MessageReactionUpdatePayload>(data.GetRawText(), DefaultOptions);
                    OnMessageReactionAdd?.Invoke(this, reactionAdd);
                    break;
                    
                case "MESSAGE_REACTION_REMOVE":
                    var reactionRemove = JsonSerializer.Deserialize<MessageReactionUpdatePayload>(data.GetRawText(), DefaultOptions);
                    OnMessageReactionRemove?.Invoke(this, reactionRemove);
                    break;
            }
        }

        private async void StartHeartbeat()
        {
             _cancellationTokenSource = new CancellationTokenSource(); // Ensure we have a token
             
             while (_webSocket.State == WebSocketState.Open)
             {
                 try
                 {
                     await Task.Delay(_heartbeatInterval, _cancellationTokenSource.Token);
                     
                     var payload = new GatewayPayload
                     {
                         OpCode = 1,
                         Data = _sequenceNumber
                     };
                     
                     string json = JsonSerializer.Serialize(payload, DefaultOptions);
                     await SendJsonAsync(json);
                 }
                 catch (TaskCanceledException) { break; }
                 catch (Exception ex)
                 {
                     System.Diagnostics.Debug.WriteLine($"Heartbeat Error: {ex}");
                     break;
                 }
             }
        }

        private async void SendIdentify()
        {
            var identify = new IdentifyPayload
            {
                Token = _token,
                Properties = new IdentifyProperties
                {
                    Os = "Windows",
                    Browser = "Chrome",
                    Device = "NativeDiscord"
                },
                Intents = 32767
            };

            var payload = new GatewayPayload
            {
                OpCode = 2,
                Data = identify
            };
            
            await SendJsonAsync(JsonSerializer.Serialize(payload, DefaultOptions));
        }

        private async Task SendJsonAsync(string json)
        {
             if (_webSocket.State != WebSocketState.Open) return;
             
             var bytes = Encoding.UTF8.GetBytes(json);
             await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
