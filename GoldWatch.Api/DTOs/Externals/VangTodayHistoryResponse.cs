using System.Dynamic;
using System.Text.Json.Serialization;

namespace GoldWatch.Api.DTOs;

public class VangTodayHistoryResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("days")]
    public int Days { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("history")]
    public List<VangTodayHistoryDateItem> History { get; set; }
}

public class VangTodayHistoryDateItem
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;
    [JsonPropertyName("prices")]
    public Dictionary<string, VangTodayHistoryPriceDetail> Prices { get; set; } = [];
}

public class VangTodayHistoryPriceDetail
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("buy")]
    public decimal Buy { get; set; }

    [JsonPropertyName("sell")]
    public decimal Sell { get; set; }

}