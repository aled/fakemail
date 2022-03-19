using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fakemail.Smtp
{
    public class SmtpConfiguration
    {
        public List<int> Ports { get; set; } = new List<int>();
    }
}
