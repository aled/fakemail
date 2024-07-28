using System;
using System.ComponentModel.DataAnnotations;

namespace Fakemail.ApiModels
{
    public class ListEmailsBySequenceNumberRequest : IUserRequest
    {
        [Required]
        public Guid UserId { get; set; }

        public string SmtpUsername { get; set; }

        public int MinSequenceNumber { get; set; } = 0;

        public int LimitEmailCount { get; set; } = 100;
    }
}
