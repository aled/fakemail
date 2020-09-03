using System;
using System.Collections.Generic;
using System.Text;

namespace Fakemail.Core
{
    public class CreateMailboxResult
    {
        public bool Success { get; set; } = false;
        public string ErrorMessage { get; set; } = string.Empty;
        public string Mailbox { get; set; } = string.Empty;
    }
}
