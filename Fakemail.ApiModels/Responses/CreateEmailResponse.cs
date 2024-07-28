using System;

namespace Fakemail.ApiModels
{
    public class CreateEmailResponse
    {
        public bool Success { get; set; } = false;

        public string ErrorMessage { get; set; } = null;

        public Guid EmailId { get; set; }
    }
}
