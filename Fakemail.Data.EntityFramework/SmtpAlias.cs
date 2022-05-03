using System.ComponentModel.DataAnnotations;

namespace Fakemail.Data.EntityFramework
{
    public class SmtpAlias
    {
        [Required]
        [Key]
        public string Account { get; set; }
    }
}
