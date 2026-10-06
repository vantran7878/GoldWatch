using GoldWatch.Api.DTOs;
using GoldWatch.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GoldWatch.Api.Controllers;

[ApiController] // Attribute
[Route("api/[controller]")] //Route token: lấy tên class nhưng bỏ đi controller

//Class là GoldController -> route là /api/gold
public class GoldController : ControllerBase
{
    private readonly IGoldPriceService _goldPriceService;

    public GoldController(IGoldPriceService goldPriceService)
    {
        _goldPriceService = goldPriceService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Models.GoldPrice>>> GetAll()
    {
        var price = await _goldPriceService.GetAllAsync();
        return Ok(price);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Models.GoldPrice>> GetByID(int id)
    {
        var goldPrice = await _goldPriceService.GetByIDAsync(id);

        if (goldPrice is null)
        {
            return NotFound();
        }

        return Ok(goldPrice);
    }

    [HttpPost]
    public async Task<ActionResult<Models.GoldPrice>> Create(CreateGoldPriceRequest request)
    {
        var goldPrice = await _goldPriceService.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetByID),
            new { id = goldPrice.Id },
            goldPrice
        );
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        CreateGoldPriceRequest request)
    {
        var updated = await _goldPriceService.UpdateAsync(id, request);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _goldPriceService.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}