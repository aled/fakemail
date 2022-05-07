using System.Collections.Generic;

namespace Fakemail.ApiModels
{
    public class ListUserResponse
    {
        public bool Success { get; set; } = false;

        public string ErrorMessage { get; set; } = string.Empty;

        public List<UserDetail> Users {get; set;}

        public int Page { get; set; }

        public int PageSize { get; set; }
    }
}
