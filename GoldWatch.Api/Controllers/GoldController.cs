using GoldWatch.Api.DTOs;
using GoldWatch.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GoldWatch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GoldController : ControllerBase
{
    private readonly IGoldPriceService _goldPriceService;

    public GoldController(IGoldPriceService goldPriceService)
    {
        _goldPriceService = goldPriceService;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<Models.GoldPrice>> GetAll()
    {
        return Ok(_goldPriceService.GetAll());
    }

    [HttpGet("{id:int}")]
    public ActionResult<Models.GoldPrice> GetByID(int id)
    {
        var goldPrice = _goldPriceService.GetByID(id);

        if (goldPrice is null)
        {
            return NotFound();
        }

        return Ok(goldPrice);
    }

    [HttpPost]
    public ActionResult<Models.GoldPrice> Create(CreateGoldPriceRequest request)
    {
        var goldPrice = _goldPriceService.Create(request);

        return CreatedAtAction(
            nameof(GetByID),
            new { id = goldPrice.Id },
            goldPrice
        );
    }

    [HttpPut("{id:int}")]
    public IActionResult Update(
        int id,
        CreateGoldPriceRequest request)
    {
        var updated = _goldPriceService.Update(id, request);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var deleted = _goldPriceService.Delete(id);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}