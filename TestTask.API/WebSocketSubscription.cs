using System.Net.WebSockets;
using System.Text;

namespace TestTask.API;

public class WebSocketSubscription : IDisposable
{
    private readonly ClientWebSocket _clientWebSocket = new ();
    private readonly CancellationTokenSource _cancellationToken = new CancellationTokenSource();
    private readonly Action<string> _handler;

    public WebSocketSubscription(Action<string> handler)
    {
        _handler = handler;
    }
    public async Task ConnectAsync(string url)
    {
        try
        {
            await _clientWebSocket.ConnectAsync(new Uri(url), CancellationToken.None);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
    
    private async Task ReceiveMessagesAsync()
    {
        var buffer = new byte[1024];
        while (_clientWebSocket.State == WebSocketState.Open && !_cancellationToken.IsCancellationRequested)
        {
            var result = await _clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationToken.Token);
            
            if (result.MessageType == WebSocketMessageType.Close)
                break;

            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Console.WriteLine(message);
            _handler(message);
        }
    }

    public async Task SubscribeAsync(string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        await _clientWebSocket.SendAsync(new ArraySegment<byte>(buffer), 
            WebSocketMessageType.Text, 
            true, 
            CancellationToken.None);
        
        _ = Task.Run(ReceiveMessagesAsync);
    }
    
    public void Dispose()
    {
        _cancellationToken.Cancel();
        _clientWebSocket.Dispose();
    }
}