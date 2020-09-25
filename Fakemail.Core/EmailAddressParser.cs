using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

using Fakemail.DataModels;

namespace Fakemail.Core
{
    public static class EmailAddressParser
    {
        public static bool TryParse(string value, out EmailAddress emailAddress)
        {
            emailAddress = null;

            var split = value.Split('@');
            if (split.Length != 2)
                return false;

            var prefix = split[0];
            var domain = split[1];

            // The rules for valid email addresses are complex. Fortunately we only need to support our own subset of addresses.
            // Use these rules:
            //  o The prefix and domain start with a letter or number
            //  o Subsequent characters can include symbols (currently - + _)
            //  o Dots are ignored in the prefix
            //  o Zero length domains are rejected (e.g. in abc..com)
            emailAddress = null;

            if (string.IsNullOrEmpty(prefix) || string.IsNullOrEmpty(domain))
                return false;

            var normalizedPrefix = prefix
                    .Replace(".", "")
                    .ToLower(CultureInfo.InvariantCulture);

            if (!Regex.IsMatch(normalizedPrefix, "[0-9a-z][0-9a-z+_-]*"))
                return false;

            var mailboxPrefix = new string(normalizedPrefix.TakeWhile(x => x != '+').ToArray());

            var normalizedDomain = domain.ToLower(CultureInfo.InvariantCulture);

            if (!Regex.IsMatch(normalizedDomain, "[0-9a-z][0-9a-z+_.-]*"))
                return false;

            var domains = domain.Split('.');
            if (domains.Length < 2)
                return false;

            if (split.Any(x => x.Length == 0))
                return false;

            emailAddress = new EmailAddress
            {
                Original = value,
                Mailbox = normalizedPrefix + "@" + normalizedDomain,
                Normalized = mailboxPrefix + "@" + normalizedDomain
            };

            return true;
        }
    }
}
