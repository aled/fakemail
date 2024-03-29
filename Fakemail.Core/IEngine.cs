﻿using System;
using System.IO;
using System.Threading.Tasks;

using Fakemail.ApiModels;

namespace Fakemail.Core
{
    public interface IEngine
    {
        /// <summary>
        /// Create a user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request);

        Task<GetTokenResponse> GetTokenAsync(GetTokenRequest user);
       
        Task<ListUserResponse> ListUsersAsync(ListUserRequest request);

        Task<CreateEmailResponse> CreateEmailAsync(Stream messageStream);

        Task<CreateEmailResponse> CreateEmailAsync(CreateEmailRequest request, Guid authenticatedUserId);

        Task<ListEmailsResponse> ListEmailsAsync(ListEmailsRequest request, Guid authenticatedUserId);
        
        Task<GetEmailResponse> GetEmailAsync(GetEmailRequest request, Guid authenticatedUserId);
        
        Task<DeleteEmailResponse> DeleteEmailAsync(DeleteEmailRequest request, Guid authenticatedUserId);
    }
}
