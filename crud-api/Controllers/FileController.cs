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
    public class FileController(FileService fileService) : ControllerBase
    {
        private readonly FileService _fileService = fileService;

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
            newFile.UserId = ExtractuserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var file = _fileService.CreateFile(newFile.UserId, newFile, formFile);
            if (file is null)
            {
                return BadRequest(new
                {
                    error = "File Already Exists or User Doesn't exist or File upload Failed"
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
            string fileName;
            string filePath;
            string fileType;
            bool isTempZip = false;

            if (fileIds.Length > 1)
            {
            var (filesExist, filePaths) = _fileService.CheckAllFilesExist(fileIds);
            if (!filesExist)
            {
                return BadRequest(new
                {
                error = "A File is Missing!"
                });
            }
            fileName = _fileService.CreateTempZipFile(filePaths);
            filePath = fileName;
            fileType = "zip";
            isTempZip = true;
            }
            else
            {
            models.File? file = _fileService.GetUserFilebyId(ExtractuserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!), fileIds[0]);
            if (file is null || file.IsDeleted == true)
            {
                return BadRequest(new
                {
                error = "File not Found!"
                });
            }
            if (string.IsNullOrEmpty(file.FilePath) || string.IsNullOrEmpty(file.FileType))
            {
                return BadRequest(new
                {
                error = "File path or Type is invalid or missing!"
                });
            }
            fileName = file.FileName;
            filePath = file.FilePath;
            fileType = file.FileType;
            }

            long expiry = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60;
            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
            string signature = Utilities.CreateSignature(filePath, expiry, ip);

            string url = $"download?filename={fileName}&file={Uri.EscapeDataString(filePath)}&filetype={fileType}&expiry={expiry}&sig={signature}";

            var cancellationToken = HttpContext.RequestAborted;
            cancellationToken.Register(() =>
            {
                string fullPath = _fileService.GetZipPath(filePath);
                if (System.IO.File.Exists(_fileService.GetZipPath(filePath)) && isTempZip)
                {
                    try { System.IO.File.Delete(_fileService.GetZipPath(filePath)); } catch { }
                }
            });

            return Ok(new { url, fileName });

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