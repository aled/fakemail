using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fakemail.ApiModels
{
    public class CreateEmailRequest : IUserRequest
    {
        public Guid UserId { get; set; }
        public byte[] MimeMessage { get; set; }
    }
}
