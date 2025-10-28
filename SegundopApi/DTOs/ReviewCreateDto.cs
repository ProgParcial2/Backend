namespace SegundopApi.DTOs
{
    public class ReviewCreateDto
    {
        public int ProductId { get; set; }
        public int Rating { get; set; } // 1 a 5
        public string Comment { get; set; } = string.Empty;
    }
}