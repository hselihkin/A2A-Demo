using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Configuration
{

    public class ApplicationOptions
    {
        [Required]
        public Uri? Server { get; set; } = null;
        public Uri? PushNotificationClient { get; set; }
        public bool Streaming { get; set; }
        public string? Auth { get; set; }
    }
}

