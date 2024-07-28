using System;

namespace Fakemail.ApiModels
{
    public class TestSmtpResponse
    {
        public bool Success { get; set; } = false;

        public string ErrorMessage { get; set; } = null;
    }
}
