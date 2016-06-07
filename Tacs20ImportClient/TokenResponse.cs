using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Tacs20ImportClient
{
    public class TokenResponse
    {
        public TokenResponse(string raw)
        {
            Json = JObject.Parse(raw);
        }

        public JObject Json { get; private set; }
        public long ExpiresIn
        {
            get { return GetLongOrNull("expires_in"); }
        }
        public string AccessToken
        {
            get { return GetStringOrNull("access_token"); }

        }

        private string GetStringOrNull(string propertyName)
        {
            JToken value;
            if (Json != null && Json.TryGetValue(propertyName, StringComparison.OrdinalIgnoreCase, out value))
            {
                return value.ToString();
            }

            return null;
        }

        long GetLongOrNull(string propertyName)
        {
            JToken value;
            if (Json != null && Json.TryGetValue(propertyName, out value))
            {
                long longValue;
                if (long.TryParse(value.ToString(), out longValue))
                {
                    return longValue;
                }
            }

            return 0;
        }
    }
}
