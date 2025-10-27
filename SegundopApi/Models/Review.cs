namespace SegundopApi.Models;

public class Review
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int ClientId { get; set; }
    public User? Client { get; set; }

    public int Rating { get; set; } // 1 a 5
    public string Comment { get; set; } = string.Empty;

    public DateTime Date { get; set; } = DateTime.Now;
}