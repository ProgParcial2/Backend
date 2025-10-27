using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SegundopApi.Data;
using SegundopApi.Models;

namespace SegundopApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly AppDbContext _context;
    public OrderController(AppDbContext context)
    {
        _context = context;
    }

    // 🟢 Crear pedido (Cliente)
    [Authorize(Roles = "Cliente")]
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] List<OrderItem> items)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var cliente = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (cliente == null) return Unauthorized();

        if (items == null || items.Count == 0)
            return BadRequest("El pedido está vacío.");

        // Todos los productos deben pertenecer a la misma empresa
        var firstProduct = await _context.Products.FindAsync(items.First().ProductId);
        if (firstProduct == null) return BadRequest("Producto no válido.");

        int empresaId = firstProduct.UserId;
        foreach (var item in items)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product == null) return BadRequest("Producto inexistente.");
            if (product.Stock < item.Quantity)
                return BadRequest($"No hay stock suficiente para {product.Name}.");
            if (product.UserId != empresaId)
                return BadRequest("Todos los productos deben pertenecer a la misma empresa.");
        }

        var order = new Order
        {
            ClientId = cliente.Id,
            CompanyId = empresaId,
            Date = DateTime.Now,
            Status = "Nuevo",
            Items = new List<OrderItem>()
        };

        foreach (var item in items)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            product!.Stock -= item.Quantity;

            order.Items.Add(new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            });
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Pedido realizado con éxito.", orderId = order.Id });
    }

    // 🔵 Ver historial de pedidos (Cliente)
    [Authorize(Roles = "Cliente")]
    [HttpGet("mis-pedidos")]
    public async Task<IActionResult> GetMyOrders()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var cliente = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        var pedidos = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Where(o => o.ClientId == cliente!.Id)
            .ToListAsync();

        return Ok(pedidos);
    }

    // 🟣 Empresa cambia el estado del pedido
    [Authorize(Roles = "Empresa")]
    [HttpPut("{orderId}/estado")]
    public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] string nuevoEstado)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var empresa = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.CompanyId == empresa!.Id);
        if (order == null) return NotFound("Pedido no encontrado.");

        order.Status = nuevoEstado;
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Estado actualizado a {nuevoEstado}." });
    }
}
