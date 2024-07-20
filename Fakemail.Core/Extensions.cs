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
