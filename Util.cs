using System;
using System.Collections.Generic;
using System.Text;

namespace EarthBuildplateEditor
{
    class Util
    {
        public static string Base64Encode(string normal)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(normal);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        public static string Base64Decode(string base64)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}
