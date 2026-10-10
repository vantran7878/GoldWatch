namespace GoldWatch.Api.Models;

public class NewsSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;
    public string RssUrl { get; set; } = string.Empty;

    public SourceType SourceType { get; set; } = SourceType.Private;

    public bool IsActive { get; set; } = true;

    public ICollection<NewsArticle> Articles { get; set; } = new List<NewsArticle>();
}