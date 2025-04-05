using System.Text.Json.Serialization;

namespace crud_api.models
{
    public class User
    {
        public int Id {get; set;}
        public required  String Name {get; set;}
        public required  String Password {get; set;}
        public required  String Email {get; set;}
        public ICollection<File>? Files { get;}
    }
    
}