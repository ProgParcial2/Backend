using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SegundopApi.Data;

namespace SegundopApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Cliente")]
public class PublicProductController : ControllerBase
{
    private readonly AppDbContext _context;
    public PublicProductController(AppDbContext context)
    {
        _context = context;
    }

    // 🔵 Ver todos los productos disponibles
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? companyId, [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice)
    {
        var query = _context.Products.AsQueryable();

        if (companyId.HasValue)
            query = query.Where(p => p.UserId == companyId.Value);

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        var result = await query.ToListAsync();
        return Ok(result);
    }
}