using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fakemail.Data.EntityFramework
{

    public class SmtpUser : BaseEntity
    {
        [Required]
        [Key]
        public string SmtpUsername { get; set; }

        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Uses the SHA512 crypt() function
        /// </summary>
        [Required]
        public string SmtpPasswordCrypt { get; set; }

        public User User { get; set; }

        public List<Email> Emails {get; set;}
    }
}
