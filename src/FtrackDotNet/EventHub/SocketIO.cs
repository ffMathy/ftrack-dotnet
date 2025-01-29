using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Web;

namespace FtrackDotNet.EventHub;

/// <summary>
/// Ftrack uses an old, deprecated version of socket.io (on the server side). 
/// This class emulates its basic client behavior in .NET.
/// @see https://github.com/ftrackhq/ftrack-javascript/blob/main/source/simple_socketio.ts
/// </summary>
public class SocketIO(Uri url) : IAsyncDisposable, ISocketIO
{
    private const string PACKET_TYPE_DISCONNECT = "0";
    private const string PACKET_TYPE_CONNECT = "1";
    private const string PACKET_TYPE_HEARTBEAT = "2";
    private const string PACKET_TYPE_MESSAGE = "3";
    private const string PACKET_TYPE_JSON = "4";
    private const string PACKET_TYPE_EVENT = "5";
    private const string PACKET_TYPE_ACKNOWLEDGE = "6";
    private const string PACKET_TYPE_ERROR = "7";

    private ClientWebSocket _webSocket = new();
    private CancellationTokenSource _cancellationTokenSource = new();

    private bool _connected = false;
    private bool _reconnecting = false;  // guard against multiple concurrent reconnect attempts
    private bool _disposed = false;      // prevent usage after disposal

    // Heartbeat interval. Matches the ~25s from simple_socketio.ts
    private readonly TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(25);

    // Reconnection delay. Matches the ~2s from simple_socketio.ts
    private readonly TimeSpan _reconnectDelay = TimeSpan.FromSeconds(2);

    private Timer? _heartbeatTimer;

    // For reading from the socket
    private const int BufferSize = 4096;

    public event Action? OnConnect;
    public event Action? OnDisconnect;
    public event Action<Exception>? OnError;
    public event Action<JsonElement>? OnEvent;

    /// <summary>
    /// Initiates the connection, starts heartbeat, then enters receive loop.
    /// </summary>
    public async Task ConnectAsync()
    {
        if (_disposed) return;  // Don't connect if already disposed

        try
        {
            await _webSocket.ConnectAsync(url, _cancellationTokenSource.Token);
            _connected = true;
            
            StartHeartbeat();

            OnConnect?.Invoke();

            _ = ReceiveLoop();
        }
        catch (Exception ex)
        {
            HandleConnectionFailure(ex);
        }
    }

    /// <summary>
    /// Continuously reads messages until the socket is closed or disposed.
    /// This version handles multi-frame messages by buffering until EndOfMessage.
    /// </summary>
    private async Task ReceiveLoop()
    {
        // Use a small buffer for each frame, and a MemoryStream to accumulate
        var buffer = new byte[BufferSize];
        using var memoryStream = new MemoryStream();

        while (!_disposed && _webSocket.State == WebSocketState.Open && !_cancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                var result = await _webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    _cancellationTokenSource.Token
                );

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    // The server closed the connection
                    Debug.WriteLine("Server closed the Event Hub connection.");
                    break;
                }
                else if (result.MessageType == WebSocketMessageType.Text)
                {
                    // Accumulate the frame data
                    memoryStream.Write(buffer, 0, result.Count);

                    if (result.EndOfMessage)
                    {
                        // We've got a full text message
                        var messageBytes = memoryStream.ToArray();
                        memoryStream.SetLength(0);  // reset for the next message

                        var message = Encoding.UTF8.GetString(messageBytes);
                        await HandleMessage(message);
                    }
                }
                else
                {
                    // If it's binary or something else, ignore or handle as needed
                }
            }
            catch (Exception ex)
            {
                // Possibly an I/O error or cancellation
                OnError?.Invoke(ex);
                break;
            }
        }

        // Exiting the loop means the socket is no longer open/valid
        _connected = false;
        OnDisconnect?.Invoke();

        await ScheduleReconnectAsync();
    }

    /// <summary>
    /// Handles an exception that occurs during connection or in the receive loop.
    /// </summary>
    private async void HandleConnectionFailure(Exception ex)
    {
        OnError?.Invoke(ex);
        
        if (_connected)
        {
            _connected = false;
            OnDisconnect?.Invoke();
        }

        await ScheduleReconnectAsync();
    }

    private async Task HandleMessage(string message)
    {
        if (string.IsNullOrEmpty(message) || _disposed) return;

        var split = message.Split(":", 2, StringSplitOptions.RemoveEmptyEntries);
        var packetType = split[0];

        Debug.WriteLine("Received: " + message);

        if (packetType == PACKET_TYPE_EVENT)
        {
            var data = split[1];
            TryHandleEvent(data);
        } 
        else if (packetType == PACKET_TYPE_CONNECT)
        {
            Debug.WriteLine("Connected to Event Hub");
        }
        else if (packetType == PACKET_TYPE_MESSAGE)
        {
            var data = split[1];
            Debug.WriteLine("Event Hub message: " + data);
        }
        else if (packetType == PACKET_TYPE_HEARTBEAT)
        {
            Debug.WriteLine("Event Hub heartbeat received");
            StartHeartbeat();
            await SendRawAsync($"{PACKET_TYPE_HEARTBEAT}::");
        }
        else if (packetType == PACKET_TYPE_DISCONNECT)
        {
            // Server closed the connection
            _connected = false;
            OnDisconnect?.Invoke();
            _ = ScheduleReconnectAsync();
        }
        else if (packetType == PACKET_TYPE_ERROR)
        {
            var data = split[1];
            OnError?.Invoke(new InvalidOperationException($"Received error from server: {data}"));
            _ = ScheduleReconnectAsync();
        }
    }

    private void TryHandleEvent(string json)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(json);
            OnEvent?.Invoke(payload);
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
        }
    }

    /// <summary>
    /// Start a heartbeat timer which sends "40" every ~25s (ping).
    /// </summary>
    private void StartHeartbeat()
    {
        StopHeartbeat(); // stop any existing timer first

        _heartbeatTimer = new Timer(_ =>
        {
            _ = CloseAsync();
        }, null, _heartbeatInterval, _heartbeatInterval);
    }

    private void StopHeartbeat()
    {
        _heartbeatTimer?.Dispose();
        _heartbeatTimer = null;
    }

    /// <summary>
    /// Send a raw string over the WebSocket.
    /// Returns a Task so callers may handle (or ignore) exceptions.
    /// </summary>
    public async Task EmitEventAsync(string message)
    {
        await SendRawAsync($"{PACKET_TYPE_EVENT}:::{message}");
    }

    private async Task SendRawAsync(string fullMessage)
    {
        if (_disposed || !_connected || _webSocket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("Event hub not connected.");
        }
        
        Debug.WriteLine("Sending: " + fullMessage);

        try
        {
            var bytes = Encoding.UTF8.GetBytes(fullMessage);
            await _webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: _cancellationTokenSource.Token
            );
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
        }
    }

    /// <summary>
    /// Closes the socket cleanly.
    /// </summary>
    public async Task CloseAsync()
    {
        if (_disposed) return;

        await _cancellationTokenSource.CancelAsync();
        StopHeartbeat();

        if (_webSocket.State == WebSocketState.Open)
        {
            try
            {
                await _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "",
                    CancellationToken.None
                );
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
            }
        }

        _webSocket.Dispose();
    }

    /// <summary>
    /// Schedules a reconnect after a delay, unless already disposed or reconnecting.
    /// </summary>
    private async Task ScheduleReconnectAsync()
    {
        if (_disposed || _reconnecting) 
            return;

        _reconnecting = true;

        StopHeartbeat();
        await Task.Delay(_reconnectDelay);

        if (!_disposed)
        {
            await ReconnectAsync();
        }

        _reconnecting = false;
    }

    /// <summary>
    /// Dispose of the old socket, create a fresh one, and connect again.
    /// </summary>
    private async Task ReconnectAsync()
    {
        if (_disposed) return;

        await _cancellationTokenSource.CancelAsync();
        _webSocket.Dispose();

        _cancellationTokenSource = new CancellationTokenSource();
        _webSocket = new ClientWebSocket();

        await ConnectAsync();
    }

    /// <summary>
    /// Permanently dispose the socket (and related resources). 
    /// No further reconnects or reads will happen after this.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) 
            return;
        
        _disposed = true;

        // Stop everything gracefully
        await CloseAsync();

        // Clean up final references
        _heartbeatTimer?.Dispose();
        _heartbeatTimer = null;

        await CastAndDisposeAsync(_webSocket);
        await CastAndDisposeAsync(_cancellationTokenSource);
    }

    private static async ValueTask CastAndDisposeAsync(IDisposable resource)
    {
        // In .NET 8, CancellationTokenSource has CancelAsync(), 
        // but it still only implements IDisposable, not IAsyncDisposable.
        // If you have other resources that do implement IAsyncDisposable, 
        // this pattern ensures a proper async dispose.
        if (resource is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else
            resource.Dispose();
    }
}
