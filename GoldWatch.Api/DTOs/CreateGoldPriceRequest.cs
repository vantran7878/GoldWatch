namespace GoldWatch.Api.DTOs;

public class CreateGoldPriceRequest
{
    public string GoldType {get; set;} = string.Empty;
    public decimal BuyPrice {get; set;}
    public decimal SellPrice {get; set;}
    public string Currency {get; set;} = string.Empty;
    public string Source {get; set;} = string.Empty;
}