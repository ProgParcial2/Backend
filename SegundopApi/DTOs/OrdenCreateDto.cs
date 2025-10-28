namespace SegundopApi.DTOs
{
    public class OrderCreateDto
    {
        public int CompanyId { get; set; } // empresa a la que se compra
        public List<OrderItemCreateDto> Items { get; set; } = new();
    }

    public class OrderItemCreateDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}