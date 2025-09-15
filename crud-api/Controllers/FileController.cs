using Microsoft.AspNetCore.Mvc;
using crud_api.Services;
using crud_api.common;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IO.Compression;
namespace crud_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController(FileService fileService,UserService userService) : ControllerBase
    {
        private readonly FileService _fileService = fileService;
        private readonly UserService _userService = userService;

        [Authorize]
        [HttpGet("get")]
        public ActionResult<List<models.File>> GetUserFiles()
        {
            return Ok(_fileService.GetAllUserFiles(ExtractuserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!)));
        }

        [Authorize]
        [HttpGet("recycle")]
        public ActionResult<List<models.File>> GetDeletedFiles()
        {
            return Ok(_fileService.GetDeletedUserFiles(ExtractuserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!)));
        }
        [HttpGet("remaining")]
        public ActionResult<bool> StorageAvailable()
        {
            return Ok(_fileService.CanAllocate());
        }

        [Authorize]
        [HttpGet("get/{fileId}")]
        public ActionResult<models.File> GetSingleUserFile(int fileId)
        {

            models.File? file = _fileService.GetUserFilebyId(ExtractuserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!), fileId);
            if (file is null)
            {
                return BadRequest(new
                {
                    error = "File not Found!"
                });
            }
            return Ok(file);
        }

        [Authorize]
        [RequestSizeLimit(1073741824)] // 1GB
        [HttpPost("create")]
        public ActionResult<models.File> CreateFile([FromForm] models.File newFile, [FromForm] IFormFile formFile)
        {
            if (formFile == null || formFile.Length == 0) return BadRequest(new
            {
                error = "Empty File upload not allowed!"
            });
            if (!Utilities.IsvalidFileName(newFile.FileName)) return BadRequest(new
            {
                error = "FileName is not valid"
            });
            models.User? user = _userService.GetUserById(ExtractuserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!));
            if (user == null)
            {
                return BadRequest(new
            {
                error = "User Not Found!"
            });
            }
            if (_fileService.CanAllocateFile(user,formFile.Length) == false)
            {
                return BadRequest(new
                {
                    error = "Storage Limit exceeded"
                });
            }
            newFile.UserId = ExtractuserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var file = _fileService.CreateFile(user, newFile, formFile);
            if (file is null)
            {
                return BadRequest(new
                {
                    error = "File Already Exists or File upload Failed"
                });
            }
            return Ok(file);
        }

        [Authorize]
        [HttpPut("update/{fileId}")]
        public ActionResult<models.File> UpdateFile(int fileId, models.File updatedFile)
        {
            var file = _fileService.GetUserFilebyId(ExtractuserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!), fileId);
            if (file is null)
            {
                return BadRequest(new
                {
                    error = "File Doesn't Exists or User Doesn't exist"
                });
            }
            if (!Utilities.IsvalidFileName(updatedFile.FileName))
            {
                return BadRequest(new
                {
                    error = "FileName is not valid"
                });
            }
            string extension = Path.GetExtension(file.FileName).ToLower();
            file.FileName = updatedFile.FileName + "." + extension;
            _fileService.UpdateFile(file);
            return Ok(file);
        }

        [Authorize]
        [HttpDelete("delete/{fileId}")]
        public ActionResult<models.File> DeleteFile(int fileId)
        {
            if (!_fileService.DeleteFile(ExtractuserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!), fileId, false)) return BadRequest(new
            {
                error = "File not Found!"
            });
            return Ok(new { message = "File Successfully deleted!" });
        }


        [Authorize]
        [HttpDelete("wipe/{fileId}")]
        public ActionResult<models.File> WipeFile(int fileId)
        {
            if (!_fileService.DeleteFile(ExtractuserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!), fileId, true)) return BadRequest(new
            {
                error = "File not Found!"
            });
            return Ok(new { message = "File permanently deleted!" });
        }

        [Authorize]
        [HttpPost("restore/{fileId}")]
        public ActionResult<models.File> RestoreFile(int fileId)
        {
            if (!_fileService.RestoreFile(ExtractuserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!), fileId)) return BadRequest(new
            {
                error = "File not Found or already restore done!"
            });
            return Ok(new { message = "File restored!" });
        }

        [Authorize]
        [HttpPost("generate-link")]
        public IActionResult GenerateLink([FromForm] int[] fileIds)
        {
            if (fileIds.Length == 0)
            {
            return BadRequest(new
                {
                error = "Zero FileIds!"
                });
            }
            int userId = ExtractuserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var links = new List<Dto.FileDownloadDto>();
            var (filesExist, files) = _fileService.CheckAllFilesExist(userId, fileIds);
            if (!filesExist)
            {
                return BadRequest(new
                {
                    error = "A File is Missing!"
                });
            }
            if (fileIds.Length > 1)
            {

                foreach (var item in files)
                {
                    links.Add(new Dto.FileDownloadDto
                    {
                        Url = _fileService.getPresigned(item),
                        FileName = item.FileName
                    });
                }
                return Ok(links);

            }
            models.File userFile = _fileService.GetUserFilebyId(userId, fileIds[0])!;
            links.Add(new Dto.FileDownloadDto
            {
                Url = _fileService.getPresigned(userFile),
                FileName = userFile.FileName
            });
            return Ok(links);
        }

        [HttpGet("download")]
        public IActionResult DownloadFile(string filename, string file, string filetype, long expiry, string sig)
        {
            if (string.IsNullOrEmpty(file) || expiry <= 0 || string.IsNullOrEmpty(sig))
            {
                return BadRequest(new
                {
                    error = "Bad Parameters!"
                });
            }
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiry)
            {
                return BadRequest(new
                {
                    error = "Link Expired!"
                });
            }

            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
            string expectedSig = Utilities.CreateSignature(file, expiry, ip);

            if (!string.Equals(sig, expectedSig, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    error = "Invalid signature"
                });
            }
            string filePath;

            if (filename == file && filetype == "zip")
            {
                filePath = _fileService.GetZipPath(filename);
                filetype = "application/zip";
            }
            else
            {
                filePath = _fileService.GetFullFilepath(file);
            }

            
            if (!System.IO.File.Exists(filePath))
            {
                return BadRequest(new
                {
                    error = "File not found"
                });
            }
            HttpContext.Response.OnCompleted(() =>
            {
                if (System.IO.File.Exists(filePath) && filename == file)
                {
                    System.IO.File.Delete(filePath);
                }
                return Task.CompletedTask;
            });
            var cancellationToken = HttpContext.RequestAborted;
            cancellationToken.Register(() =>
            {
                if (System.IO.File.Exists(filePath) && filename == file)
                {
                    try { System.IO.File.Delete(filePath); } catch { }
                }
            });
            return PhysicalFile(filePath, filetype, filename, enableRangeProcessing: true);

        }

        public int ExtractuserId(string userId)
        {
            return Int32.Parse(userId);
        }
    
    } 
}