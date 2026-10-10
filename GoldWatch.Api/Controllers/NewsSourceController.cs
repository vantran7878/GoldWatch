using System.Runtime.Versioning;
using GoldWatch.Api.DTOs;
using GoldWatch.Api.Models;
using GoldWatch.Api.Services.News;
using Microsoft.AspNetCore.Mvc;

namespace GoldWatch.Api.Controllers;

[ApiController]
[Route("api/news/sources")]
public class NewsSourceController : ControllerBase
{
    private readonly INewsSourceService _newsSourceService;

    public NewsSourceController(INewsSourceService newsSourceService)
    {
        _newsSourceService = newsSourceService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NewsSource>>> GetAll()
    {
        var sources = await _newsSourceService.GetAllAsync();

        return Ok(sources);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<NewsSource>> GetById(int id)
    {
        var source = await _newsSourceService.GetByIdAsync(id);
        if (source is null)
        {
            return BadRequest();
        }
        return Ok(source);
    }

    [HttpPost]
    public async Task<ActionResult<NewsSource>> Create(CreateNewsSourceRequest request)
    {
        var created_source = await _newsSourceService.CreateAsync(request);
        if (created_source is null)
        {
            return BadRequest();
        }

        return CreatedAtAction(nameof(GetById), new { id = created_source.Id }, created_source);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateNewsSourceRequest request)
    {
        var updated = await _newsSourceService.UpdateAsync(id, request);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _newsSourceService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }
        return NoContent();
    }
}