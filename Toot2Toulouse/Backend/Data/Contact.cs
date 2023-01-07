namespace Toot2Toulouse.Backend.Data
{
    public class Contact
    {
        public string MastodonHandle { get; set; }    
        public List<ContactAssignment> Assignments { get; set; }
    }

    public class ContactAssignment
    {
        public string CreatedBy { get; set; } // User
        public User.SearchOptions SearchOption { get; set; }
        public string TwitterHandle { get; set; }   
        public DateTime CreatedAt { get; set; } 
    }
}
