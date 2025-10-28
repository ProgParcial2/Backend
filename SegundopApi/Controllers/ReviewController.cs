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
public class ReviewController : ControllerBase
{
    private readonly AppDbContext _context;
    public ReviewController(AppDbContext context)
    {
        _context = context;
    }

    // 🟢 Crear reseña (solo clientes)
    [Authorize(Roles = "Cliente")]
    [HttpPost]
    public async Task<IActionResult> CreateReview([FromBody] ReviewCreateDto dto)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var client = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (client == null) return Unauthorized("Cliente no válido.");

        var product = await _context.Products.FindAsync(dto.ProductId);
        if (product == null) return NotFound("Producto no encontrado.");

        // Verificar si el cliente ya compró este producto
        bool hasOrder = await _context.Orders
            .Include(o => o.Items)
            .AnyAsync(o => o.ClientId == client.Id && 
                           o.Items.Any(i => i.ProductId == dto.ProductId));
        if (!hasOrder)
            return BadRequest("Solo puedes reseñar productos que hayas comprado.");

        // Crear reseña
        var review = new Review
        {
            ProductId = dto.ProductId,
            ClientId = client.Id,
            Rating = dto.Rating,
            Comment = dto.Comment,
            Date = DateTime.UtcNow
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        return Ok(new ReviewResponseDto
        {
            Id = review.Id,
            ProductName = product.Name,
            Rating = review.Rating,
            Comment = review.Comment,
            Date = review.Date
        });
    }

    // 🔵 Ver reseñas por producto
    [AllowAnonymous]
    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetReviewsByProduct(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null) return NotFound("Producto no encontrado.");

        var reviews = await _context.Reviews
            .Where(r => r.ProductId == productId)
            .Select(r => new ReviewResponseDto
            {
                Id = r.Id,
                ProductName = product.Name,
                Rating = r.Rating,
                Comment = r.Comment,
                Date = r.Date
            })
            .ToListAsync();

        return Ok(reviews);
    }
}
