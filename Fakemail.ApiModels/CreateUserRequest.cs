using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fakemail.ApiModels
{
    public class CreateUserRequest
    {
        /// <summary>
        /// Optional
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Optional. User will be unsecured.
        /// </summary>
        public string Password { get; set; }
    }
}
