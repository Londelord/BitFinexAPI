using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using TestTask.API.ConnectorTest;
using TestTask.API.TestHQ;

namespace TestTask.API;

public class Connector : ITestConnector
{
    private readonly HttpClient _httpClient = new ()
    {
        BaseAddress = new Uri("https://api-pub.bitfinex.com/v2/")
    };
    
    private readonly ClientWebSocket _clientWebSocket = new();
    private readonly Dictionary<string, Action<string, int>> _tradeHandlers = new();
    private readonly Dictionary<string, Action<string>> _candleHandlers = new();

    public async Task ConnectAsync()
    {
        await _clientWebSocket.ConnectAsync(new Uri("wss://api-pub.bitfinex.com/ws/2"), CancellationToken.None);
    }
    
    public async Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount)
    {
        var response = await _httpClient.GetFromJsonAsync<object[][]>($"trades/t{pair.ToUpper()}/hist?limit={maxCount}");
        
        var trades = new List<Trade>();
        
        if (response == null)
        {
            Console.WriteLine("No trades found");
            return trades;
        }

        trades.AddRange(response.Select(trade =>
        {
            
            return new Trade
            {
                Pair = pair.ToUpper(),
                Id = Convert.ToString(trade[0]),
                Time = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(trade[1].ToString())),
                Amount = Math.Abs(Convert.ToDecimal(float.Parse(trade[2].ToString()))),
                Price = Convert.ToDecimal(trade[3].ToString()),
                Side = Convert.ToDecimal(float.Parse(trade[2].ToString())) > 0 ? "buy" : "sell"
            };
        }));

        return trades;
    }

    public async Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0)
    {
        if (from is null && to is null && count is null)
            throw new ArgumentException("Count or from and to must be set");

        string periodInString = TimeFrame.GetTimeFrameInString(periodInSec);
        
        long start = from.Value.ToUnixTimeMilliseconds();
        long end = to!.Value.ToUnixTimeMilliseconds();

        object[][]? candlesApi;
        
        if (count is not null)
        {
            candlesApi = await _httpClient.GetFromJsonAsync<object[][]>
                ($"candles/trade:{periodInString}:t{pair.ToUpper()}/hist?limit={count}");
        }
        else
        {
            if (from is null && to is null)
                throw new ArgumentException("From and To must be set");
            
            candlesApi = await _httpClient.GetFromJsonAsync<object[][]>
                ($"candles/trade:{periodInString}:t{pair.ToUpper()}/hist?start={start}&end={end}");
        }
        
        var candles = new List<Candle>();
        
        candles.AddRange(candlesApi.Select(candle =>
        {
            string Pair = pair.ToUpper();
            DateTimeOffset StartTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(candle[0].ToString()));
            decimal OpenPrice = Convert.ToDecimal(candle[1].ToString());
            decimal ClosePrice = Convert.ToDecimal(candle[2].ToString());
            decimal LowPrice = Convert.ToDecimal(candle[3].ToString());
            decimal HighPrice = Convert.ToDecimal(candle[4].ToString());
            decimal Volume = Convert.ToDecimal(float.Parse(candle[5].ToString()));
            decimal TotalPrice = Volume * ClosePrice;

            return new Candle
            {
                Pair = Pair,
                OpenPrice = OpenPrice,
                ClosePrice = ClosePrice,
                LowPrice = LowPrice,
                HighPrice = HighPrice,
                TotalPrice = TotalPrice,
                TotalVolume = Volume,
                OpenTime = StartTime
            };
        }));
        
        return candles;
    }

    public event Action<Trade>? NewBuyTrade;
    public event Action<Trade>? NewSellTrade;

    public void HandleTrade(string pair, int maxCount = 100)
    {
        string message = JsonSerializer.Serialize(new
        {
            @event = "subscribe",
            channel = "trades",
            symbol = $"t{pair.ToUpper()}"
        });

        byte[] buffer = Encoding.UTF8.GetBytes(message);
        _clientWebSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);

        var resultBuffer = new ArraySegment<byte>(new byte[4048]);
        int countOfInfoMessages = 3;
        int numberOfCurrentMessage = 0;
            
        while (_clientWebSocket.State == WebSocketState.Open)
        {
            var result = _clientWebSocket.ReceiveAsync(resultBuffer, CancellationToken.None);
            if (result.Result.MessageType == WebSocketMessageType.Close) break;

            if (numberOfCurrentMessage < countOfInfoMessages)
            {
                numberOfCurrentMessage++;
                continue;
            }
            
            string response = Encoding.UTF8.GetString(resultBuffer.Array, 0, result.Result.Count);
            
            var data = JsonSerializer.Deserialize<object[]>(response);
            
            if (data.Length < 3)
                continue;

            var trade = new Trade
            {
                Pair = pair.ToUpper(),
                Id = Convert.ToString(((JsonElement)data[2])[0]),
                Time = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(((JsonElement)data[2])[1].ToString())),
                Amount = Math.Abs(Convert.ToDecimal(float.Parse(((JsonElement)data[2])[2].ToString()))),
                Price = Math.Abs(Convert.ToDecimal(((JsonElement)data[2])[3].ToString())),
                Side = Convert.ToDecimal(float.Parse(((JsonElement)data[2])[2].ToString())) > 0 ? "buy" : "sell"
            };

            if (trade.Side == "buy")
                OnNewBuyTrade(trade);
            else
                OnNewSellTrade(trade);
        }
    }
    
    public void SubscribeTrades(string pair, int maxCount = 100)
    {
        HandleTrade(pair, maxCount);
        if (!_tradeHandlers.ContainsKey(pair))
        {
            _tradeHandlers.Add(pair, HandleTrade);   
        }
    }

    public void UnsubscribeTrades(string pair)
    {
        throw new NotImplementedException();
    }

    public event Action<Candle>? CandleSeriesProcessing;
    public void SubscribeCandles(string pair, int periodInSec, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0)
    {
        throw new NotImplementedException();
    }

    public void UnsubscribeCandles(string pair)
    {
        throw new NotImplementedException();
    }
    
    private void OnNewBuyTrade(Trade trade)
    {
        NewBuyTrade?.Invoke(trade);
    }

    private void OnNewSellTrade(Trade trade)
    {
        NewSellTrade?.Invoke(trade);
    }
}