using crud_api.Data;
using crud_api.common;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using crud_api.models;

namespace crud_api.Services
{
    public class FileService(FileDbContext context,CloudflareR2Service s3Service,IConfiguration _config)
    {
        private readonly FileDbContext _context = context;
        private readonly CloudflareR2Service _s3Service = s3Service;

        private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "storage");
        private readonly string _tempPath = Path.Combine(Directory.GetCurrentDirectory(), "temp");

        public List<models.File> GetAllUserFiles(int userId)
        {
            var user = _context.Users.Include(u => u.Files).FirstOrDefault(u => u.Id == userId);
            return user?.Files?.Where(file => file.IsDeleted == false).ToList() ?? [];
        }
        public bool CanAllocate()
        {
            long totalAmount  = _context.Database.SqlQueryRaw<long>("SELECT SUM(TRY_CONVERT(BIGINT, TotalSize)) AS Total FROM Users;").AsEnumerable().FirstOrDefault();
            _ = long.TryParse(_config["TotalObjectStorage"],out long allocatedAmount);
            return totalAmount < allocatedAmount;
        }

        public bool CanAllocateFile(User user, long fileSize)
        {
            long totalSize = 0;
            if (!string.IsNullOrEmpty(user.RemainingSize))
            {
                _ = long.TryParse(user.RemainingSize, out totalSize);
            }
            if ((totalSize - fileSize) < 0) return false;
            return true;
        
        }

        public List<models.File> GetDeletedUserFiles(int userId)
        {
            var user = _context.Users.Include(u => u.Files).FirstOrDefault(u => u.Id == userId);
            return user?.Files?.Where(file => file.IsDeleted == true).ToList() ?? [];
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

        public models.File? CreateFile(User user, models.File newFile, IFormFile uploadFile)
        {
            newFile.FileName += Path.GetExtension(uploadFile.FileName).ToLower();
            newFile.FilePath = user.Id + "_" + Utilities.GenerateUUID() + "_" + newFile.FileName;
            if (user.userType == "guest")
            {
                newFile.FilePath = "guest" + newFile.FilePath;
            }
            bool fileExists = _context.Files.Any(f => f.UserId == user.Id && f.FileName == newFile.FileName);
            if (fileExists) return null;
            bool uploaded = UploadFile(newFile, uploadFile);
            if (!uploaded) return null;
            long totalSize = 0;
            long fileSize = uploadFile.Length;
            if (!string.IsNullOrEmpty(user.RemainingSize))
            {
                _ = long.TryParse(user.RemainingSize, out totalSize);
            }
            user.RemainingSize = (totalSize - fileSize).ToString();
            newFile.FileType = uploadFile.ContentType;
            newFile.FileSize = fileSize.ToString();
            newFile.UserId = user.Id;
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
                long delSize = _s3Service.Delete(file.FilePath);
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

        public bool FileExists(int userId, int fileId)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return false;
            bool fileExists = _context.Files.Any(f => f.UserId == userId && f.FileId == fileId);
            return fileExists;
        }

        public bool UploadFile(models.File newFile, IFormFile file)
        {
            try
            {
                if (!Directory.Exists(_storagePath))
                {
                    Directory.CreateDirectory(_storagePath);
                }
                if (newFile.FilePath is null)
                {
                    throw new Exception("Filepath is Empty");
                }
                return _s3Service.Upload(newFile.FilePath, file);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");

                return false;
            }

        }

        public string GetFullFilepath(string shortPath)
        {
            return Path.Combine(_storagePath, shortPath);
        }

        public bool RestoreFile(int userId, int fileId)
        {
            if (!FileExists(userId, fileId))
            {
                return false;
            }
            var file = _context.Files.Where(f => f.UserId == userId && f.FileId == fileId).FirstOrDefault();
            if (file is null || file.FilePath is null)
            {
                return false;
            }
            if (file.IsDeleted == true)
            {
                file.IsDeleted = false;
                _context.SaveChanges();
                return true;
            }
            return false;
        }

        public string GetZipPath(string shortPath)
        {
            return Path.Combine(_tempPath, shortPath);
        }

        public async Task<Amazon.S3.Model.GetObjectResponse> GetFile(string key)
        {
            return await _s3Service.GetS3ObjectAsync(key);
        }
        public string getPresigned(models.File file)
        {
            return _s3Service.GetPresignedUrl(file);
        }

        public (bool, List<models.File>) CheckAllFilesExist(int userId,int[] fileIds)
        {
            var matchedFiles = _context.Files
                .Where(f => fileIds.Contains(f.FileId) && f.UserId == userId)
                .ToList();

            bool allExist = matchedFiles.Select(f => f.FileId).Distinct().Count() == fileIds.Length;

            return (allExist, matchedFiles);
        }
    }

}
