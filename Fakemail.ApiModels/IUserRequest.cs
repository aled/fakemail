using System;

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
