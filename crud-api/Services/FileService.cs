using crud_api.Data;
using crud_api.common;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace crud_api.Services
{
    public class FileService(FileDbContext context)
    {
        private readonly FileDbContext _context = context;
        private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "storage");

        public List<models.File> GetAllUserFiles(int userId)
        {
            var user = _context.Users.Include(u => u.Files).FirstOrDefault(u => u.Id == userId);
            return user?.Files?.ToList() ?? [];
        }

        public models.File? GetUserFilebyId(int id,int fileId)
        {
            var user = _context.Users.Include(u => u.Files).FirstOrDefault(f => f.Id == id) ?? null;
            if(user is not null){
                
               return user.Files?.FirstOrDefault(f => f.FileId == fileId);
            }
            return null;
        }

        public models.File? CreateFile(int userId,models.File newFile,IFormFile uploadFile)
        {
            var user = _context.Users.Find(userId);
            if (user == null) return null;
            newFile.FileName += Path.GetExtension(uploadFile.FileName).ToLower();
            newFile.FilePath = userId+"_"+Utilities.GenerateUUID()+"_"+newFile.FileName;
            long fileSize = uploadFile.Length;
            bool fileExists = _context.Files.Any(f => f.UserId == userId && f.FileName == newFile.FileName);
            if (fileExists) return null;
            bool uploaded = UploadFile(newFile,uploadFile);
            if (!uploaded) return null;
            newFile.FileType = uploadFile.ContentType;
            newFile.FileSize = $"{fileSize / 1024.0:F2} KB";
            newFile.UserId = userId;
            _context.Files.Add(newFile);
            _context.SaveChanges();
            return newFile;
        }
        public models.File UpdateFile(models.File updatedFile)
        {
            _context.Files.Update(updatedFile);
            _context.SaveChanges();
            return updatedFile;
        }
        public bool DeleteFile(int userId,int fileId)
        {
            if (!FileExists(userId,fileId)){
                return false;
            }
            var file = _context.Files.Where(f => f.UserId ==userId && f.FileId == fileId).FirstOrDefault();
            if(file is null || file.FilePath is null){
                return false;

            }
            string fullpath = Path.Combine(_storagePath,file.FilePath);
            DeleteuploadFile(fullpath);
            _context.Files.Remove(file);
            _context.SaveChanges();
            return true;
        }

        public bool FileExists(int userId,int fileId){
            var user = _context.Users.Find(userId);
            if (user == null) return false;
            bool fileExists = _context.Files.Any(f => f.UserId == userId && f.FileId == fileId);
            return fileExists;
        }

        public bool UploadFile(models.File newFile,IFormFile file){
            try
            {
                if (!Directory.Exists(_storagePath))
                {
                    Directory.CreateDirectory(_storagePath);
                }
                if (newFile.FilePath is  null){
                    throw new Exception("Filepath is Empty");
                }
                string filePath = Path.Combine(_storagePath, newFile.FilePath);
                using var stream = new FileStream(filePath, FileMode.Create);
                file.CopyTo(stream);
                return true;
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                
                return false;
            }

        }

        public bool DeleteuploadFile(string filePath){
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting file: {ex.Message}");
            return false;
        }
            
        }

        public string  GetFullFilepath(string shortPath){
            return Path.Combine(_storagePath,shortPath);
        }
    }
}
