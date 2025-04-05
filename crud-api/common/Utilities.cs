using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace crud_api.common
{
    class Utilities
    {
        public static string ComputeSHA256(string rawData)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(rawData);
            byte[] hashBytes = SHA256.HashData(bytes);
            return Convert.ToHexString(hashBytes);
        }
        public static string GenerateUUID(){
            Guid newGuid = Guid.NewGuid();
            return newGuid.ToString("N"); 
        }

        public static bool IsvalidFileName(string fileName){
            string pattern = @"^(?!.*([<>:""/\\|?*]|[.]{2,}|\/|\.\.))([a-zA-Z0-9_-]+(?:[ .a-zA-Z0-9_-]*[a-zA-Z0-9_-])?)(?:\.[a-zA-Z0-9]+)?$";
            Regex regex = new(pattern);
            return regex.IsMatch(fileName);
        }
    }
}
