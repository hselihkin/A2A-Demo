using System.ComponentModel.DataAnnotations;

namespace Registry.Models
{
    public class Servers
    {
        [Key]
        public required String Uri { get; set; }
        public DateTime JoinTime { get; set; }
    }
}

