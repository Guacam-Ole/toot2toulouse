namespace Toot2Toulouse.Backend.Models
{
    public class Crosspost
    {
        public DateTime? CreatedAt { get; set; }=DateTime.UtcNow; 
        public string TootId { get; set; }
        public List<long> TwitterIds { get; set; } = new List<long>();
    }
}