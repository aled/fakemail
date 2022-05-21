using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fakemail.ApiModels
{
    public class DeleteEmailResponse
    {
        public bool Success { get; set; } = false;

        public string ErrorMessage { get; set; } = null;
    }
}
