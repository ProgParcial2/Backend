using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SegundopApi.Data;
using SegundopApi.Models;

namespace SegundopApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Empresa")]
public class ProductController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductController(AppDbContext context)
    {
        _context = context;
    }

    // 🟢 CREATE (POST)
    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] Product product)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var empresa = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (empresa == null) return Unauthorized("Usuario no válido.");

        product.UserId = empresa.Id;

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMyProducts), new { id = product.Id }, product);
    }

    // 🔵 READ (GET)
    [HttpGet]
    public async Task<IActionResult> GetMyProducts()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var empresa = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (empresa == null) return Unauthorized("Usuario no válido.");

        var productos = await _context.Products
            .Where(p => p.UserId == empresa.Id)
            .ToListAsync();

        return Ok(productos);
    }

    // 🟠 UPDATE (PUT)
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product updated)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var empresa = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (empresa == null) return Unauthorized("Usuario no válido.");

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == empresa.Id);

        if (product == null) return NotFound("Producto no encontrado o no te pertenece.");

        product.Name = updated.Name;
        product.Description = updated.Description;
        product.Price = updated.Price;
        product.Stock = updated.Stock;

        await _context.SaveChangesAsync();

        return Ok(product);
    }

    // 🔴 DELETE
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var empresa = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (empresa == null) return Unauthorized("Usuario no válido.");

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == empresa.Id);

        if (product == null) return NotFound("Producto no encontrado o no te pertenece.");

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Producto eliminado correctamente." });
    }
    [AllowAnonymous]
    [HttpGet("all")]
    public async Task<IActionResult> GetAllProducts(
        [FromQuery] int? empresaId,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice)
    {
        var query = _context.Products.Include(p => p.User).AsQueryable();

        if (empresaId.HasValue)
            query = query.Where(p => p.UserId == empresaId.Value);

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        var productos = await query.ToListAsync();
        return Ok(productos);
    }
}
