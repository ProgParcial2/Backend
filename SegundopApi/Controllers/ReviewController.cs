using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SegundopApi.Data;
using SegundopApi.Models;

namespace SegundopApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Cliente")]
public class ReviewController : ControllerBase
{
    private readonly AppDbContext _context;
    public ReviewController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> AddReview([FromBody] Review review)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var cliente = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (cliente == null) return Unauthorized();

        // Validar que el cliente haya comprado el producto
        bool compró = await _context.Orders
            .Include(o => o.Items)
            .AnyAsync(o => o.ClientId == cliente.Id && o.Items.Any(i => i.ProductId == review.ProductId));

        if (!compró)
            return BadRequest("Solo puedes reseñar productos que compraste.");

        review.ClientId = cliente.Id;
        review.Date = DateTime.Now;

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Reseña guardada." });
    }

    [HttpGet("{productId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetReviewsByProduct(int productId)
    {
        var reviews = await _context.Reviews
            .Where(r => r.ProductId == productId)
            .Include(r => r.Client)
            .ToListAsync();

        return Ok(reviews);
    }
}