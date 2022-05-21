using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fakemail.ApiModels
{
    public class DeleteEmailRequest : IUserRequest
    {
        public Guid UserId { get; set; }

        public Guid EmailId { get; set; }
    }
}
