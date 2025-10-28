namespace SegundopApi.DTOs
{
    public class OrderResponseDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public List<OrderItemResponseDto> Items { get; set; } = new();
    }

    public class OrderItemResponseDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}