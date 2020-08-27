using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Fakemail.DataModels
{
    public class EmailAddress
    {
        // The validated email address, exactly as received
        public string Original { get; }

        // The validated email address, with dots removed and lower case
        // For example, the address Fred.Bloggs+Test@EVILCORP.COM should
        // be normaized to fred.bloggs+test@evilcorp.com.
        public string Normalized { get; }

        // The mailbox that emails to this address should go to.
        // This is the normalized address, with the prefix limited to the part 
        // before the first '+' character.
        //
        // For example, messages to the address Fred.Bloggs+Test@EVILCORP.COM should
        // go to the mailbox fred.bloggs@evilcorp.com.
        public string Mailbox { get; }

        private EmailAddress(string original, string normalized, string mailbox)
        {
            Original = original;
            Normalized = normalized;
            Mailbox = mailbox;
        }

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

            emailAddress = new EmailAddress(value, normalizedPrefix + "@" + normalizedDomain, mailboxPrefix + "@" + normalizedDomain);

            return true;
        }
    }
}
