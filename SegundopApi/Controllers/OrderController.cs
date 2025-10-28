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
    public async Task<IActionResult> CreateOrder([FromBody] OrderCreateDto dto)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var cliente = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (cliente == null) return Unauthorized("Cliente no válido.");

        if (dto.Items == null || dto.Items.Count == 0)
            return BadRequest("El pedido está vacío.");

        var empresa = await _context.Users.FindAsync(dto.CompanyId);
        if (empresa == null)
            return NotFound("Empresa no encontrada.");

        var order = new Order
        {
            ClientId = cliente.Id,
            CompanyId = dto.CompanyId,
            Date = DateTime.UtcNow,
            Status = "Nuevo",
            Items = new List<OrderItem>()
        };

        // Verificar y descontar stock
        foreach (var itemDto in dto.Items)
        {
            var product = await _context.Products.FindAsync(itemDto.ProductId);
            if (product == null)
                return BadRequest($"Producto {itemDto.ProductId} no existe.");
            if (product.UserId != dto.CompanyId)
                return BadRequest($"El producto {product.Name} no pertenece a la empresa seleccionada.");
            if (product.Stock < itemDto.Quantity)
                return BadRequest($"Stock insuficiente para {product.Name}.");

            // Descontar stock
            product.Stock -= itemDto.Quantity;

            // Crear item del pedido
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = itemDto.Quantity,
                UnitPrice = product.Price
            });
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Pedido realizado con éxito.",
            orderId = order.Id,
            order.Status,
            order.Date
        });
    }

    // 🔵 Ver historial de pedidos (Cliente)
    [Authorize(Roles = "Cliente")]
    [HttpGet("mis-pedidos")]
    public async Task<IActionResult> GetMyOrders()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var cliente = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (cliente == null) return Unauthorized("Cliente no válido.");

        var pedidos = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Where(o => o.ClientId == cliente.Id)
            .Select(o => new OrderResponseDto
            {
                Id = o.Id,
                Status = o.Status,
                Date = o.Date,
                Items = o.Items.Select(i => new OrderItemResponseDto
                {
                    ProductName = i.Product.Name,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            })
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
        if (empresa == null) return Unauthorized("Empresa no válida.");

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId && o.CompanyId == empresa.Id);
        if (order == null) return NotFound("Pedido no encontrado.");

        order.Status = nuevoEstado;
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Estado actualizado a '{nuevoEstado}'." });
    }
}
