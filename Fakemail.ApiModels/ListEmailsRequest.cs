using System;
using System.ComponentModel.DataAnnotations;

namespace Fakemail.ApiModels
{
    public class ListEmailsRequest
    {
        [Required]
        public Guid UserId { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}
