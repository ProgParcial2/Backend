using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SegundopApi.Data;
using SegundopApi.DTOs;
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

    // 🟢 Crear producto (solo Empresa)
    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] ProductCreateDto dto)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var empresa = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (empresa == null) return Unauthorized("Usuario no válido.");

        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            Stock = dto.Stock,
            UserId = empresa.Id
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Mapea a DTO de respuesta limpia
        var result = new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock
        };

        return CreatedAtAction(nameof(GetMyProducts), new { id = product.Id }, result);
    }

    // 🔵 Ver productos de la empresa autenticada
    [HttpGet]
    public async Task<IActionResult> GetMyProducts()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var empresa = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (empresa == null) return Unauthorized("Usuario no válido.");

        var productos = await _context.Products
            .Where(p => p.UserId == empresa.Id)
            .Select(p => new ProductResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Stock = p.Stock
            })
            .ToListAsync();

        return Ok(productos);
    }

    // 🟠 Actualizar producto propio
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductCreateDto dto)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var empresa = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (empresa == null) return Unauthorized("Usuario no válido.");

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == empresa.Id);

        if (product == null) return NotFound("Producto no encontrado o no te pertenece.");

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.Stock = dto.Stock;

        await _context.SaveChangesAsync();

        var result = new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock
        };

        return Ok(result);
    }

    // 🔴 Eliminar producto propio
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

    // 🌐 Ver todos los productos (rol Cliente o público)
    [AllowAnonymous]
    [HttpGet("all")]
    public async Task<IActionResult> GetAllProducts(
        [FromQuery] int? empresaId,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice)
    {
        var query = _context.Products.AsQueryable();

        if (empresaId.HasValue)
            query = query.Where(p => p.UserId == empresaId.Value);

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        var productos = await query
            .Select(p => new ProductResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Stock = p.Stock
            })
            .ToListAsync();

        return Ok(productos);
    }
}
