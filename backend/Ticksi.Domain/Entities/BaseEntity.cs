namespace Ticksi.Domain.Entities;

public abstract class BaseEntity
{
    //Internal key - used in backend and database
    public int Id {get; set;}

    //Public key - used for API, Frontend
    public Guid PublicId { get; set; } = Guid.NewGuid();


}