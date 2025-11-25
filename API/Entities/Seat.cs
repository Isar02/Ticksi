namespace API.Entities
{
    public class Seat : BaseEntity
    {
        public int LocationId { get; set; }
        public Location? Location { get; set; }

        public int RowNumber { get; set; }
        public int SeatNumber { get; set; }
        public string Zone { get; set; } = string.Empty;
        public string Status { get; set; } = "Available";

        // Navigation
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
