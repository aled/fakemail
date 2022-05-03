using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fakemail.Core
{
    public static class Extensions
    {
        public static string Truncate(this string s, int len)
        {
            if (s == null)
                return "";

            if (s.Length <= len)
                return s;

            return s.Substring(0, len);
        }
    }
}
