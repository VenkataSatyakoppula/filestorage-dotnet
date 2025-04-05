using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace crud_api.models
{
    public class File
    {
        public int FileId {get; set;}
        public required string FileName {get; set;}
        public string? FilePath {get;set;}
        public int UserId { get; set; }

        [JsonIgnore]
        public User? User { get; set; }
        public string? FileSize {get; set;}

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime? CreatedAt {get;set;} = DateTime.UtcNow;
        public string? FileType {get;set;}
    }
}
