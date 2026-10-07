using System.ComponentModel;
using System.Text.Json.Serialization;

namespace GoldWatch.Api.DTOs;

public class VangTodayPriceResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("buy")]
    public decimal Buy { get; set; }

    [JsonPropertyName("sell")]
    public decimal Sell { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
}