namespace API.Entities;

public abstract class BaseEntity
{
    //Interni kljuc - koristi se samo u bazi i backend
    public int Id {get; set;}

    //Javni kljuc - koristi se za API, URL-ove i Frontend
    public string PublicId { get; set; } = Guid.NewGuid().ToString();
}