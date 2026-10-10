namespace GoldWatch.Api.Models;

public class NewsArticle
{
    public int Id { get; set; }
    public int SourceID { get; set; }
    public NewsSource? Source { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Author { get; set; }
    public DateTime PublishedAt { get; set; }
    public DateTime CollectedAt { get; set; }
}