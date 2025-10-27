namespace SegundopApi.Models;

public class Order
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public User? Client { get; set; }

    public int CompanyId { get; set; }
    public User? Company { get; set; }

    public DateTime Date { get; set; } = DateTime.Now;
    public string Status { get; set; } = "Nuevo"; // Nuevo, Enviado, Entregado, Cancelado

    public ICollection<OrderItem>? Items { get; set; }
}