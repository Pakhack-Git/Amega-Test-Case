using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("WebSocketApp");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseWebSockets();

var clients = new ConcurrentDictionary<Guid, WebSocket>();

// Start the Tiingo WebSocket listener in the background
_ = Task.Run(() => TiingoListener(clients, logger));

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var id = Guid.NewGuid();
        clients.TryAdd(id, webSocket);
        logger.LogInformation("Client {ClientId} connected.", id);

        // Wait for the client to disconnect
        var buffer = new byte[1024];
        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
                break;
        }

        clients.TryRemove(id, out _);
        logger.LogInformation("Client {ClientId} disconnected.", id);
        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
    }
    else
    {
        context.Response.StatusCode = 400;
        logger.LogWarning("Non-WebSocket request received at /ws endpoint.");
    }
});

async Task TiingoListener(ConcurrentDictionary<Guid, WebSocket> clients, ILogger logger)
{
    var tiingoUri = new Uri("wss://api.tiingo.com/fx");
    using var tiingoSocket = new ClientWebSocket();
    logger.LogInformation("Connecting to Tiingo WebSocket at {Uri}", tiingoUri);

    await tiingoSocket.ConnectAsync(tiingoUri, CancellationToken.None);
    logger.LogInformation("Connected to Tiingo WebSocket.");

    var subscribeMsg = JsonSerializer.Serialize(new
    {
        eventName = "subscribe",
        authorization = "39042154ebdf8fe0e48ba4cb280837b6c11830dd",
        eventData = new { tickers = new[] { "EURUSD" } }
    });

    var subscribeBuffer = Encoding.UTF8.GetBytes(subscribeMsg);
    await tiingoSocket.SendAsync(new ArraySegment<byte>(subscribeBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
    logger.LogInformation("Sent subscription message to Tiingo for EURUSD.");

    var buffer = new byte[4096];
    while (tiingoSocket.State == WebSocketState.Open)
    {
        var result = await tiingoSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        if (result.MessageType == WebSocketMessageType.Close)
        {
            logger.LogWarning("Tiingo WebSocket closed the connection.");
            break;
        }

        logger.LogInformation("Received price update from Tiingo. Broadcasting to {Count} clients.", clients.Count);

        // Broadcast to all connected clients
        var message = new ArraySegment<byte>(buffer, 0, result.Count);
        foreach (var client in clients)
        {
            if (client.Value.State == WebSocketState.Open)
            {
                try
                {
                    await client.Value.SendAsync(message, WebSocketMessageType.Text, true, CancellationToken.None);
                    logger.LogDebug("Sent update to client {ClientId}.", client.Key);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send update to client {ClientId}.", client.Key);
                }
            }
        }
    }
    logger.LogInformation("Exiting TiingoListener loop.");
}

app.Run();