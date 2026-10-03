namespace GoldWatch.Api.Models;


public class GoldPrice
{
    public int Id {get; set; }    
    public string GoldType {get; set;} = string.Empty;
    public decimal BuyPrice {get; set;}
    public decimal SellPrice {get; set;}
    public string Currency {get; set;} = "VND";
    public string Source {get; set;} = string.Empty;
    public DateTime CollectedAt {get; set;}
}
