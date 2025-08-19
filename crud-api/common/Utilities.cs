using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace crud_api.common
{
    class Utilities
    {
        private const string SecretKey = "bwakugjhaw43782384gfvb37845tg38gfb";
        public static string ComputeSHA256(string rawData)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(rawData);
            byte[] hashBytes = SHA256.HashData(bytes);
            return Convert.ToHexString(hashBytes);
        }
        public static string GenerateUUID()
        {
            Guid newGuid = Guid.NewGuid();
            return newGuid.ToString("N");
        }

        public static bool IsvalidFileName(string fileName)
        {
            string pattern = @"^(?!.*([<>:""/\\|?*]|[.]{2,}|\/|\.\.))([a-zA-Z0-9_-]+(?:[ .a-zA-Z0-9_-]*[a-zA-Z0-9_-])?)(?:\.[a-zA-Z0-9]+)?$";
            Regex regex = new(pattern);
            return regex.IsMatch(fileName);
        }

        public static string FileFormat(long fileSize)
        {
            const double KB = 1024.0;
            const double MB = KB * 1024.0;
            const double GB = MB * 1024.0;

            if (fileSize < KB)
            {
                return $"{fileSize:F2} B";
            }
            else if (fileSize < MB)
            {
                return $"{fileSize / KB:F2} KB";
            }
            else if (fileSize < GB)
            {
                return $"{fileSize / MB:F2} MB";
            }
            else
            {
                return $"{fileSize / GB:F2} GB";
            }
        }
        public static string CreateSignature(string file, long expiry, string ip)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SecretKey));
            var data = $"{file}:{expiry}:{ip}";
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
        
    }
}
