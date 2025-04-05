using crud_api.Data;
using crud_api.Dto;
using crud_api.models;
using crud_api.Mapper;
using crud_api.common;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace crud_api.Services
{
    public class UserService(FileDbContext context)
    {
        private readonly FileDbContext _context = context;
        
        public List<User> GetAllUsers()
        {
            List<User> users = _context.Users.ToList();
            return users ?? [];
        }

        public User? GetUserById(int id)
        {
            var user = _context.Users.Where(f => f.Id == id).FirstOrDefault() ?? null;
            if(user is not null){
               return user;
            }
            return null;
        }

        public User? CreateUser(CreateUserDto newUser)
        {
            bool usersExists = _context.Users.Any(u=> u.Email == newUser.Email);
            if (usersExists){
                return null;
            }
            var user = new User 
            {
                Name = newUser.Name,
                Email = newUser.Email,
                Password = Utilities.ComputeSHA256(newUser.Password)
            };
            _context.Users.Add(user);
            _context.SaveChanges();
            return user;
        }

        public User UpdateUser(User updatedUser)
        {
            _context.Users.Update(updatedUser);
            _context.SaveChanges();
            return updatedUser;
        }
        public void DeleteUser(int id)
        {
            var user = GetUserById(id);
            if (user != null)
            {
                _context.Users.Where(u => u.Id ==user.Id).ExecuteDelete();
                _context.SaveChanges();
            }
        }

        public bool EmailExists(string email){
            return _context.Users.Any(u=> u.Email == email);
        }
    }
}
