using System.ComponentModel.DataAnnotations;

namespace Fakemail.ApiModels
{
    public class ListEmailRequest
    {
        [Required]
        public string SmtpUsername { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;

    }
}
