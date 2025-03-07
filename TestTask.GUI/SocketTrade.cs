namespace TestTask.GUI;

public class SocketTrade
{
    public string BuyOrSell { get; set; }
    public string Id { get; set; }
    public string Pair { get; set; }
    public decimal Price { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset Time { get; set; }
}