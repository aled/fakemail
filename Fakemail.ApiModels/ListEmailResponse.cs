using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fakemail.ApiModels
{
    public class ListEmailResponse
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int MaxPage { get; set; }
        public List<EmailSummary> Emails { get; set; }
    }
}
