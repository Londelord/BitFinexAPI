using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using TestTask.API;
using TestTask.API.TestHQ;

namespace TestTask.GUI.ViewModel;

public class MainViewModel : INotifyPropertyChanged
{
    private Visibility _candlesAmountTextBoxVisibility = Visibility.Visible;
    private Visibility _timeTextBoxesVisibility = Visibility.Collapsed;
    private readonly Connector _connector = new Connector();

    public Visibility CandlesAmountTextBoxVisibility
    {
        get => _candlesAmountTextBoxVisibility;
        set
        {
            _candlesAmountTextBoxVisibility = value;
            OnPropertyChanged(nameof(CandlesAmountTextBoxVisibility));
        }
    }

    public Visibility TimeTextBoxesVisibility
    {
        get => _timeTextBoxesVisibility;
        set
        {
            _timeTextBoxesVisibility = value;
            OnPropertyChanged(nameof(TimeTextBoxesVisibility));
        }
    }
    public ObservableCollection<Trade> Trades { get; set; }
    private string _pair = "btcusd";
    private int _tradesAmount = 10;
    private int _candlesAmount = 10;
    public ObservableCollection<Candle> Candles { get; set; }
    public ObservableCollection<Trade> SocketTrades { get; set; }
    public ObservableCollection<Candle> SocketCandles { get; set; }
    public ObservableCollection<string> TimeFrames { get; set; }
    public string SelectedTimeFrame { get; set; }

    private string _selectedTimeFrom = "";
    private string _selectedTimeTo = "";

    public string SelectedTimeFrom
    {
        get => _selectedTimeFrom;
        set
        {
            _selectedTimeFrom = value;
            OnPropertyChanged(nameof(SelectedTimeFrom));
        }
    }

    public string SelectedTimeTo
    {
        get => _selectedTimeTo;
        set
        {
            _selectedTimeTo = value;
            OnPropertyChanged(nameof(SelectedTimeTo));
        }
    }
    public string Pair
    {
        get => _pair;
        set
        {
            _pair = value;
            OnPropertyChanged(nameof(Pair));
        }
    }

    public int TradesAmount
    {
        get => _tradesAmount;
        set
        {
            _tradesAmount = value;
            OnPropertyChanged(nameof(TradesAmount));
        }
    }

    public int CandlesAmount
    {
        get => _candlesAmount;
        set
        {
            _candlesAmount = value;
            OnPropertyChanged(nameof(CandlesAmount));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public MainViewModel()
    {
        TimeFrames = ["1m", "5m", "15m", "30m", "1h", "3h", "6h", "12h", "1D", "1W", "14D", "1M"];
        SelectedTimeFrame = TimeFrames[0];
        Trades = [];
        Candles = [];
        SocketTrades = [];
        SocketCandles = [];

        SelectedTimeFrom = DateTimeOffset.UtcNow.AddMinutes(-30).ToString();
        SelectedTimeTo = DateTimeOffset.UtcNow.ToString();
        
        LoadTradesAsync();
    }

    public async Task LoadTradesAsync()
    {
        var trades = await _connector.GetNewTradesAsync(Pair, TradesAmount);
        
        Trades.Clear();

        foreach (var trade in trades)
        {
            Trades.Add(trade);
        }
    }
    
    public async Task LoadCandlesAsync()
    {
        var candles = await _connector.GetCandleSeriesAsync(
            Pair, 
            TimeFrame.GetTimeFrameInInt(SelectedTimeFrame),
            DateTimeOffset.Parse(SelectedTimeFrom),
            DateTimeOffset.Parse(SelectedTimeTo),
            _timeTextBoxesVisibility == Visibility.Visible ? null : CandlesAmount);

        Candles.Clear();
        foreach (var candle in candles)
        {
            Candles.Add(candle);
        }
    }

    public async Task LoadSocketTradesAsync()
    {
        await Task.Run(async () =>
        {
            await _connector.ConnectAsync();

            _connector.NewBuyTrade += AddTradeToCollection;
            _connector.NewSellTrade += AddTradeToCollection;
            
            _connector.SubscribeTrades(Pair, TradesAmount);
        });
        
    }

    private void AddTradeToCollection(Trade trade)
    {
        Console.WriteLine(trade.Id);
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (SocketTrades.Any(t => t.Id == trade.Id))
                return;

            SocketTrades.Add(trade);

            if (SocketTrades.Count > TradesAmount)
                SocketTrades.RemoveAt(0);
        });
    }
}