using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using GoldWatch.Api.Models;

namespace GoldWatch.Api.DTOs;

public class CreateNewsSourceRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string Url { get; set; } = string.Empty;
    [Required]
    public string RssUrl { get; set; } = string.Empty;

    public SourceType SourceType { get; set; } = SourceType.Private;

    public bool IsActive { get; set; } = true;
}