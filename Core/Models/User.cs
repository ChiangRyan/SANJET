using System.Collections.Generic;

namespace SANJET.Core.Models
{
    public class User
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new List<string>();
    }
}