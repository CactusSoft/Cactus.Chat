namespace Cactus.Chat
{
    public static class StringExtensions
    {
        public static string Secure(this string str)
        {
            if (str == null)
            {
                return null;
            }

            if (str.Length < 5)
            {
                return string.Empty;
            }

            if (str.Length > 4 && str.Length < 8)
            {
                return str.Substring(0, 3) + "*****";
            }

            if (str.Length > 7 && str.Length < 12)
            {
                return str.Substring(0, 4) + "*****";
            }

            if (str.Length > 11 && str.Length < 34)
            {
                return str.Substring(0, 6) + "*****";
            }

            if (str.Length > 23 && str.Length < 60)
            {
                return str.Substring(0, 5) + " ... " + str.Substring(str.Length - 5);
            }

            return str.Substring(0, 10) + " ... " + str.Substring(str.Length - 10);
        }
    }
}