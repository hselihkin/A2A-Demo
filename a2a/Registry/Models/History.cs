namespace Registry.Models
{
    public class History
    {
        public Guid Id { get; set; }
        public Uri Uri { get; set; }
        public DateTime JoinTime { get; set; }
        public DateTime? LeaveTime { get; set; }
    }
}
