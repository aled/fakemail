using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
    
namespace Fakemail.DataModels
{
    public class User
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Salt { get; set; }

        [Required]
        public string HashedPassword { get; set; }
    }

    public class SmtpUser
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string SmtpUsername { get; set; }

        [Required]        
        public string SmtpHashedPassword { get; set; }

        public User User { get; set; }
    }
}
