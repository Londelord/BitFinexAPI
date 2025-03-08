using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using TestTask.API.ConnectorTest;
using TestTask.API.TestHQ;

namespace TestTask.API;

public class Connector : ITestConnector
{
    private readonly HttpClient _httpClient = new HttpClient
    {
        BaseAddress = new Uri("https://api-pub.bitfinex.com/v2/")
    };

    private readonly string _wsPath = "wss://api-pub.bitfinex.com/ws/2";
    
    private readonly Dictionary<string, WebSocketSubscription> _tradeHandlers =
        new Dictionary<string, WebSocketSubscription>();
    private readonly Dictionary<string, WebSocketSubscription> _candleHandlers =
        new Dictionary<string, WebSocketSubscription>();
    
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

    public async Task<float[]> GetLastPricesOfPairsAsync(string[] pairs)
    {
        var queryParameter = "symbols=";

        for (int i = 0; i < pairs.Length; i++)
        {
            queryParameter += "t" + pairs[i].ToUpper() + ",";
        }
        
        var response = await _httpClient.GetFromJsonAsync<object[][]>($"tickers?{queryParameter}");
        
        if (response == null) return [];
        
        var lastPrices = new float[pairs.Length];

        for (int i = 0; i < response.Length; i++)
        {
            lastPrices[i] = float.Parse(response[i][7].ToString());
        }
        
        return lastPrices;
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
    
    public void SubscribeTrades(string pair, int maxCount = 100)
    {
        pair = pair.ToUpper();
        if (_tradeHandlers.ContainsKey(pair))
            return;

        var subscription = new WebSocketSubscription(response => HandleTrade(pair, response));
        
        _tradeHandlers[pair] = subscription;
        subscription.ConnectAsync(_wsPath).Wait();
        subscription.SubscribeAsync(JsonSerializer.Serialize(new
        {
            @event = "subscribe",
            channel = "trades",
            symbol = $"t{pair}"
        })).Wait();
    }
    
    public void UnsubscribeTrades(string pair)
    {
        pair = pair.ToUpper();
        if (!_tradeHandlers.TryGetValue(pair, out var handler))
            return;

        handler.Dispose();
        _tradeHandlers.Remove(pair);
    }
    
    private void HandleTrade(string pair, string response)
    {
        object[]? data;
        try
        {
            data = JsonSerializer.Deserialize<object[]>(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return;
        }
            
        if (data.Length < 3)
            return;

        if (data == null)
            return;

        var trade = new Trade
        {
            Pair = pair.ToUpper(),
            Id = Convert.ToString(((JsonElement)data[2])[0]) ?? string.Empty,
            Time = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(((JsonElement)data[2])[1].ToString())),
            Amount = Math.Abs(Convert.ToDecimal(float.Parse(((JsonElement)data[2])[2].ToString()))),
            Price = Math.Abs(Convert.ToDecimal(((JsonElement)data[2])[3].ToString())),
            Side = Convert.ToDecimal(float.Parse(((JsonElement)data[2])[2].ToString())) > 0 ? "buy" : "sell"
        };

        if (trade.Side == "buy")
            NewBuyTrade?.Invoke(trade);
        else
            NewSellTrade?.Invoke(trade);
    }

    public event Action<Candle>? CandleSeriesProcessing;
    public void SubscribeCandles(string pair, int periodInSec, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0)
    {
        pair = pair.ToUpper();
        if (_candleHandlers.ContainsKey(pair))
            return;

        var subscription = new WebSocketSubscription(response => HandleCandle(pair, response));
        
        _candleHandlers[pair] = subscription;
        
        subscription.ConnectAsync(_wsPath).Wait(); 
        subscription.SubscribeAsync(JsonSerializer.Serialize(new
        {
            @event = "subscribe",
            channel = "candles",
            key = $"trade:{TimeFrame.GetTimeFrameInString(periodInSec)}:t{pair}"
        })).Wait();
    }
    
    public void UnsubscribeCandles(string pair)
    {
        pair = pair.ToUpper();
        if (!_candleHandlers.TryGetValue(pair, out var handler))
            return;

        handler.Dispose();
        _candleHandlers.Remove(pair);
    }

    private void HandleCandle(string pair, string response)
    {
        object[]? data;
        try
        {
            data = JsonSerializer.Deserialize<object[]>(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return;
        }
        
        try
        {
            var candle = new Candle
            {
                Pair = pair,
                OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(((JsonElement)data[1])[0].ToString())),
                OpenPrice = Convert.ToDecimal(((JsonElement)data[1])[1].ToString()),
                ClosePrice = Convert.ToDecimal(((JsonElement)data[1])[2].ToString()),
                HighPrice = Convert.ToDecimal(((JsonElement)data[1])[3].ToString()),
                LowPrice = Convert.ToDecimal(((JsonElement)data[1])[4].ToString()),
                TotalVolume = Convert.ToDecimal(float.Parse(((JsonElement)data[1])[5].ToString())),
                TotalPrice = 0
            };

            candle.TotalPrice = candle.TotalVolume * candle.ClosePrice;
            Console.WriteLine(candle.OpenTime);

            CandleSeriesProcessing?.Invoke(candle);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}