using crud_api.Data;
using crud_api.common;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace crud_api.Services
{
    public class FileService(FileDbContext context,IConfiguration _config)
    {
        private readonly FileDbContext _context = context;
        private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "storage");

        public List<models.File> GetAllUserFiles(int userId)
        {
            var user = _context.Users.Include(u => u.Files).FirstOrDefault(u => u.Id == userId);
            return user?.Files?.Where(file=> file.IsDeleted==false).ToList() ?? [];
        }

        public List<models.File> GetDeletedUserFiles(int userId)
        {
            var user = _context.Users.Include(u => u.Files).FirstOrDefault(u => u.Id == userId);
            return user?.Files?.Where(file=> file.IsDeleted==true).ToList() ?? [];
        }


        public models.File? GetUserFilebyId(int id, int fileId)
        {
            var user = _context.Users.Include(u => u.Files).FirstOrDefault(f => f.Id == id) ?? null;
            if (user is not null)
            {

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
            long totalSize = 0;
            if (!string.IsNullOrEmpty(user.RemainingSize))
            {
                _ = long.TryParse(user.RemainingSize, out totalSize);
            }
            if ((totalSize - fileSize) < 0) return null;
            user.RemainingSize = (totalSize - fileSize).ToString();
            newFile.FileType = uploadFile.ContentType;
            newFile.FileSize = fileSize.ToString();
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
        public bool DeleteFile(int userId, int fileId, bool permanently)
        {
            if (!FileExists(userId, fileId))
            {
                return false;
            }
            var user = _context.Users.Where(u => u.Id == userId).FirstOrDefault();
            var file = _context.Files.Where(f => f.UserId == userId && f.FileId == fileId).FirstOrDefault();
            if (file is null || file.FilePath is null)
            {
                return false;
            }
            if (file.IsDeleted == false && !permanently)
            {
                file.IsDeleted = true;
                _context.SaveChanges();
                return true;
            }
            if (file.IsDeleted == true || permanently)
            {
                string fullpath = Path.Combine(_storagePath, file.FilePath);
                long delSize = DeleteuploadFile(fullpath);
                long curSize = 0;
                long maxSize = 0;
                if (user != null && !string.IsNullOrEmpty(user.RemainingSize) && !string.IsNullOrEmpty(user.TotalSize))
                {
                    _ = long.TryParse(user.RemainingSize, out curSize);
                    _ = long.TryParse(user.TotalSize, out maxSize);
                }
                int filesLength = _context.Files.Where(f => f.UserId == userId).ToArray().Length;
                if (user != null)
                {
                    lock (user)
                    {
                        if (curSize + delSize > maxSize || filesLength <= 1)
                        {
                            user.TotalSize = _config["fileSizeLimit"] ?? "1073741824";
                            user.RemainingSize = _config["fileSizeLimit"] ?? "1073741824";
                        }
                        else
                        {
                            user.RemainingSize = (curSize + delSize).ToString();
                        }
                    }
                }

                _context.Files.Remove(file);
                
                _context.SaveChanges();
                return true;
            }
            return false;

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

        public long DeleteuploadFile(string filePath){
        try
        {
            if (File.Exists(filePath))
            {
                long fileSize = new FileInfo(filePath).Length;
                File.Delete(filePath);
                return fileSize;
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting file: {ex.Message}");
            return 0;
        }
            
        }

        public string  GetFullFilepath(string shortPath){
            return Path.Combine(_storagePath,shortPath);
        }
    }
}
