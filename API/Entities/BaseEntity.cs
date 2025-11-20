namespace API.Entities;

public abstract class BaseEntity
{
    //Internal key - used in backend and database
    public int Id {get; set;}

    //Public key - used for API, Frontend
    public string PublicId { get; set; } = Guid.NewGuid().ToString();
}