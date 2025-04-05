

namespace crud_api.models
{
    public class LoginResponse
    {
        public int? UserId {get;set;}
        public string? AccessToken {get;set;}
        public int ExpiresIn {get;set;}
    }

}