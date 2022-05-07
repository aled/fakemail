using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fakemail.ApiModels
{
    public class GetTokenResponse
    {
        public bool Success { get; set; } = false;

        public string ErrorMessage { get; set; } = null;

        public string Username { get; set; }

        public bool IsAdmin { get; set; } = false;

        public string Token { get; set; } = null;
    }
}
