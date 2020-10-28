using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Netcore.Simplest.Chat.Test
{
    internal class Auth
    {
        public Auth()
        {
            AuthType = "TIMMY";
        }

        public Auth(string user) : this()
        {
            User = user;
            UserPrettyName = user;
        }

        public Auth(string user, params KeyValuePair<string, string>[] claims) : this(user)
        {
            Claims = claims;
        }

        public string User { get; set; }

        public string UserPrettyName { get; set; }

        public IEnumerable<KeyValuePair<string, string>> Claims { get; set; }

        public string AuthType { get; set; }

        public string BuildAuthToken()
        {
            string token;
            if (Claims != null)
                token = BuildComplexToken();
            else token = User;
            return AuthType + ' ' + token;
        }

        private string BuildComplexToken()
        {
            var res = "!" + Claims
                          .Select(e => Uri.EscapeDataString(e.Key) + '=' + Uri.EscapeDataString(e.Value))
                          .Aggregate((a, v) => a + '&' + v);

            if (Claims.Any(e => e.Key != ClaimTypes.NameIdentifier))
            {
                res += '&' + Uri.EscapeDataString(ClaimTypes.NameIdentifier) + '=' + Uri.EscapeDataString(User);
            }

            if (Claims.Any(e => e.Key != ClaimTypes.Name))
            {
                res += '&' + Uri.EscapeDataString(ClaimTypes.Name) + '=' + Uri.EscapeDataString(User);
            }
            return res;
        }

        public static Auth Timmy(string salt = null)
        {
            return new Auth("timmy" + salt + "@south.park", new KeyValuePair<string, string>("sub", "5926f2438832e05034fc324d"))
            {
                UserPrettyName = "Timmy"
            };
        }

        public static Auth Butters(string salt = null)
        {
            return new Auth("butters" + salt + "@south.park",
                new KeyValuePair<string, string>("sub", "5926f2438832e05034fc324d"))
            {
                UserPrettyName = "Butters"
            };
        }

        public static Auth Kartman(string salt = null)
        {
            return new Auth("kartman" + salt + "@south.park",
                new KeyValuePair<string, string>("sub", "5926f2438832e05034fc325d"))
            {
                UserPrettyName = "Kartman"
            };
        }

        public static Auth Stan(string salt = null)
        {
            return new Auth("stan" + salt + "@south.park",
                new KeyValuePair<string, string>("sub", "5926f2438832e05034fc326d"))
            {
                UserPrettyName = "Stan"
            };
        }

        public static Auth Kenny(string salt = null)
        {
            return new Auth("kenny" + salt + "@south.park",
                new KeyValuePair<string, string>("sub", "5926f2438832e05034fc326d"))
            {
                UserPrettyName = "Kenny"
            };
        }

        public static Auth Stranger(string salt = null)
        {
            return new Auth("stranger" + salt + "@void.com",
                new KeyValuePair<string, string>("sub", "5d26f2438832e05034fc32ad"))
            {
                UserPrettyName = "Stranger & shooter"
            };
        }
    }
}
