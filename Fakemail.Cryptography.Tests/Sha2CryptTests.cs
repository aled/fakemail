using Xunit;

namespace Fakemail.Cryptography.Tests
{
    // These test cases copied from the specification document at https://akkadia.org/drepper/SHA-crypt.txt
    public class Sha2CryptTests
    {
        [Theory]
        [InlineData("$6$saltstring",
                    "Hello world!",
                    "$6$saltstring$svn8UoSVapNtMuq1ukKS4tPQd8iKwSMHWjl/O817G3uBnIFNjnQJuesI68u4OTLiBFdcbYEdFCoEOfaS35inz1")]
        [InlineData("$6$rounds=10000$saltstringsaltstring",
                    "Hello world!",
                    "$6$rounds=10000$saltstringsaltst$OW1/O6BYHV6BcXZu8QVeXbDWra3Oeqh0sbHbbMCVNSnCM/UrjmM0Dp8vOuZeHBy/YTBmSK6H9qs/y3RnOaw5v.")]
        [InlineData("$6$rounds=5000$toolongsaltstring",
                    "This is just a test",
                    "$6$rounds=5000$toolongsaltstrin$lQ8jolhgVRVhY4b5pZKaysCLi0QBxGoNeKQzQ3glMhwllF7oGDZxUhx1yxdYcz/e1JSbq3y6JMxxl8audkUEm0")]
        [InlineData("$6$rounds=1400$anotherlongsaltstring",
                    "a very much longer text to encrypt.  This one even stretches over morethan one line.",
                    "$6$rounds=1400$anotherlongsalts$POfYwTEok97VWcjxIiSOjiykti.o/pQs.wPvMxQ6Fm7I6IoYN3CmLs66x9t0oSwbtEW7o7UmJEiDwGqd8p4ur1")]
        [InlineData("$6$rounds=77777$short",
                    "we have a short salt string but not a short password",
                    "$6$rounds=77777$short$WuQyW2YR.hBNpjjRhpYD/ifIw05xdfeEyQoMxIXbkvr0gge1a1x3yRULJ5CCaUeOxFmtlcGZelFl5CxtgfiAc0")]
        [InlineData("$6$rounds=123456$asaltof16chars..",
                    "a short string",
                    "$6$rounds=123456$asaltof16chars..$BtCwjqMJGx5hrJhZywWvt0RLE8uZ4oPwcelCjmw2kSYu.Ec6ycULevoBK25fs2xXgMNrCzIMVcgEJAstJeonj1")]
        [InlineData("$6$rounds=10$roundstoolow",
                    "the minimum number is still observed",
                    "$6$rounds=1000$roundstoolow$kUMsbe306n21p9R.FRkW3IGn.S9NPN0x50YhH1xhLsPuWGsUSklZt58jaTfF4ZEQpyUNGc0dqbpBYYBaHHrsX.")]
        public void SHA512ShouldBeCalculatedCorrectly(string salt, string key, string expected)
        {
            var actual = Sha2Crypt.Sha512Crypt(key, salt);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("password")]
        public void SHA512ShouldBeValidatedCorrectly(string key)
        {
            var hash = Sha2Crypt.Sha512Crypt(key);

            Assert.True(Sha2Crypt.Validate(key, hash));
            Assert.False(Sha2Crypt.Validate("asdf", hash));
        }

        [Theory]
        [InlineData("password")]
        public void SHA256ShouldBeValidatedCorrectly(string key)
        {
            var hash = Sha2Crypt.Sha256Crypt(key);

            Assert.True(Sha2Crypt.Validate(key, hash));
            Assert.False(Sha2Crypt.Validate("asdf", hash));
        }

        [Theory]
        [InlineData("$5$saltstring",
                    "Hello world!",
                    "$5$saltstring$5B8vYYiY.CVt1RlTTf8KbXBH3hsxY/GNooZaBBGWEc5")]
        [InlineData("$5$rounds=10000$saltstringsaltstring",
                    "Hello world!",
                    "$5$rounds=10000$saltstringsaltst$3xv.VbSHBb41AL9AvLeujZkZRBAwqFMz2.opqey6IcA")]
        [InlineData("$5$rounds=5000$toolongsaltstring",
                    "This is just a test",
                    "$5$rounds=5000$toolongsaltstrin$Un/5jzAHMgOGZ5.mWJpuVolil07guHPvOW8mGRcvxa5")]
        [InlineData("$5$rounds=1400$anotherlongsaltstring",
                    "a very much longer text to encrypt.  This one even stretches over morethan one line.",
                    "$5$rounds=1400$anotherlongsalts$Rx.j8H.h8HjEDGomFU8bDkXm3XIUnzyxf12oP84Bnq1")]
        [InlineData("$5$rounds=77777$short",
                    "we have a short salt string but not a short password",
                    "$5$rounds=77777$short$JiO1O3ZpDAxGJeaDIuqCoEFysAe1mZNJRs3pw0KQRd/")]
        [InlineData("$5$rounds=123456$asaltof16chars..",
                    "a short string",
                    "$5$rounds=123456$asaltof16chars..$gP3VQ/6X7UUEW3HkBn2w1/Ptq2jxPyzV/cZKmF/wJvD")]
        [InlineData("$5$rounds=10$roundstoolow",
                    "the minimum number is still observed",
                    "$5$rounds=1000$roundstoolow$yfvwcWrQ8l/K0DAWyuPMDNHpIVlTQebY9l/gL972bIC")]
        public void SHA256ShouldBeCalculatedCorrectly(string salt, string key, string expected)
        {
            var actual = Sha2Crypt.Sha256Crypt(key, salt);

            Assert.Equal(expected, actual);
        }
    }
}