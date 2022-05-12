using System.ComponentModel.DataAnnotations;

namespace Fakemail.Data.EntityFramework
{
    /// <summary>
    /// This exists only for the SMTP server to map email addresses to the unix account owning the mailbox
    /// </summary>
    public class SmtpAlias
    {
        [Required]
        [Key]
        public string Account { get; set; }
    }
}
