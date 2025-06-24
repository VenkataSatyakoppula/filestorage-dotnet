namespace crud_api.Dto {
    public class UserDto
        {
            public int Id { get; set; }
            public required string Name { get; set; }
            public required string Email { get; set; }

            public required models.LoginResponse Credentials {get;set;}
        }
    public class CreateUserDto
        {
            public required string Name { get; set; }
            public required string Email { get; set; }
            public required string Password { get; set; }
        }
    
    public class UpdateUserDto
    {
        public required string Name { get; set; }
        public required string Email { get; set; }
        public string? Password { get; set; }
    }


}