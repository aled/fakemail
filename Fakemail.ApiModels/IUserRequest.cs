using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fakemail.ApiModels
{
    /// <summary>
    /// Interface used by requests that include the (unauthenticated) UserId
    /// </summary>
    public interface IUserRequest
    {
        public Guid UserId { get; set; }
    }
}
