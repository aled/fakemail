using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fakemail.ApiModels
{
    public class ListUserRequest
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
