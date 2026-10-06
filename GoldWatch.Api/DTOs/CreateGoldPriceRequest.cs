using System.ComponentModel.DataAnnotations;

namespace GoldWatch.Api.DTOs;

public class CreateGoldPriceRequest
{
    [Required(ErrorMessage = "GoldType cannot be empty")]
    public string GoldType { get; set; } = string.Empty;
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must larger than 0")]
    public decimal BuyPrice { get; set; }
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must larger than 0")]
    public decimal SellPrice { get; set; }
    [Required]
    public string Currency { get; set; } = string.Empty;
    [Required]
    public string Source { get; set; } = string.Empty;
}