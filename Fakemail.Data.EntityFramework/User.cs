using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fakemail.Data.EntityFramework
{
    public class User : BaseEntity
    {
        [Required]
        [Key]
        public Guid UserId { get; set; }

        [Required]
        public string Username { get; set; }

        /// <summary>
        /// Uses the SHA512 crypt() function
        /// </summary>
        [Required]
        public string PasswordCrypt { get; set; }

        public List<SmtpUser> SmtpUsers { get; set; }
    }
}
