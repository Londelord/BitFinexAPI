namespace TestTask.API;

public class TimeFrame
{
    public static string GetTimeFrameInString(int seconds)
    {
        return seconds switch
        {
            60 => "1m",
            300 => "5m",
            900 => "15m",
            1800 => "30m",
            3600 => "1h",
            10800 => "3h",
            21600 => "6h",
            12*60*60 => "12h",
            24*60*60 => "1D",
            7*24*60*60 => "1W",
            14*24*60*60 => "14D",
            30*24*60*60 => "1M",
            _ => "1m"
        };
    }
    
    public static int GetTimeFrameInInt(string timeFrame)
    {
        return timeFrame switch
        {
            "1m" => 60,
            "5m" => 300,
            "15m" => 900,
            "30m" => 1800,
            "1h" => 3600,
            "3h" => 10800,
            "6h" => 21600,
            "12h" => 12*60*60,
            "1D" => 24*60*60,
            "1W" => 7*24*60*60,
            "14D" => 14*24*60*60, 
            "1M" => 30*24*60*60,
            _ => 60
        };
    }
}