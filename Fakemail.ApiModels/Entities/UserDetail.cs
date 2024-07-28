using System.Collections.Generic;

namespace Fakemail.ApiModels
{
    public class UserDetail
    {
        public string Username { get; set; }

        public bool IsAdmin { get; set; }

        public List<SmtpUserDetail> SmtpUsers { get; set; }

        public int CurrentEmailCount { get; set; }
    }
}
