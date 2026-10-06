namespace Ticksi.Application.DTOs;

public class EventCategoryReadDto
{
    public Guid PublicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
}
