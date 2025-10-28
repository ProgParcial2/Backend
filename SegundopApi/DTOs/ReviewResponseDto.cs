namespace SegundopApi.DTOs
{
    public class ReviewResponseDto
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
}