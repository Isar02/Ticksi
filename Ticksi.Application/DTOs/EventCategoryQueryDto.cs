namespace Ticksi.Application.DTOs
{
    public class EventCategoryQueryDto
    {
        public string? Search { get; set; }
        public string? Filter { get; set; }  // npr "popular", "active", "archived" (za kasnije)
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 8;
    }
}
