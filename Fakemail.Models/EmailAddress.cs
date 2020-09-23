using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Fakemail.DataModels
{
    public record EmailAddress
    {
        // The validated email address, exactly as received
        public string Original { get; init; }

        // The validated email address, with dots removed and lower case
        // For example, the address Fred.Bloggs+Test@EVILCORP.COM should
        // be normaized to fred.bloggs+test@evilcorp.com.
        public string Normalized { get; init; }

        // The mailbox that emails to this address should go to.
        // This is the normalized address, with the prefix limited to the part 
        // before the first '+' character.
        //
        // For example, messages to the address Fred.Bloggs+Test@EVILCORP.COM should
        // go to the mailbox fred.bloggs@evilcorp.com.
        public string Mailbox { get; init; }
    }
}
