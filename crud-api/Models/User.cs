using System.Text.Json.Serialization;

namespace crud_api.models
{
    public class User
    {
        public int Id { get; set; }
        public required String Name { get; set; }
        public required String Password { get; set; }
        public required String Email { get; set; }
        public string? userType { get; set; }
        public ICollection<File>? Files { get; }
        public string? Token { get; set; }
        public string? TotalSize { get; set; } = "1073741824"; // 1GB
        public string? RemainingSize { get; set; } = "1073741824";
    }
    
}