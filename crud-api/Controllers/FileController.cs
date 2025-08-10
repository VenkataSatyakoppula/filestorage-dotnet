using Microsoft.AspNetCore.Mvc;
using crud_api.Services;
using crud_api.common;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
namespace crud_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController(FileService fileService) : ControllerBase
    {
        private readonly FileService  _fileService = fileService;

        [Authorize]
        [HttpGet("get")]
        public ActionResult<List<models.File>> GetUserFiles(){
            return Ok(_fileService.GetAllUserFiles(ExtractuserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!)));
        }

        [Authorize]
        [HttpGet("recycle")]
        public ActionResult<List<models.File>> GetDeletedFiles(){
            return Ok(_fileService.GetDeletedUserFiles(ExtractuserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!)));
        }



        [Authorize]
        [HttpGet("get/{fileId}")]
        public ActionResult<models.File> GetSingleUserFile(int fileId){
            
            models.File? file = _fileService.GetUserFilebyId(ExtractuserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!),fileId);
            if (file is null){
                return BadRequest(new {
                error = "File not Found!"
                });
            }
            return Ok(file);
        }

        [Authorize]
        [RequestSizeLimit(1073741824)] // 1GB
        [HttpPost("create")]
        public ActionResult<models.File> CreateFile([FromForm] models.File newFile,[FromForm] IFormFile formFile){
            if(formFile == null || formFile.Length == 0) return BadRequest(new {
                    error = "Empty File upload not allowed!"
                });
            if(!Utilities.IsvalidFileName(newFile.FileName)) return BadRequest(new {
                    error = "FileName is not valid"
                });
            newFile.UserId = ExtractuserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var file = _fileService.CreateFile(newFile.UserId,newFile,formFile);
            if (file is null){
                return BadRequest(new {
                    error = "File Already Exists or User Doesn't exist or File upload Failed"
                });
            }
            return Ok(file);
        }

        [Authorize]
        [HttpPut("update/{fileId}")]
        public ActionResult<models.File> UpdateFile(int fileId,models.File updatedFile){
            var file = _fileService.GetUserFilebyId(ExtractuserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!),fileId);
            if(file is null){
                return BadRequest(new {
                    error = "File Doesn't Exists or User Doesn't exist" 
                });
            }
            if (!Utilities.IsvalidFileName(updatedFile.FileName)){
                return BadRequest(new {
                    error = "FileName is not valid" 
                });
            }
            string extension = Path.GetExtension(file.FileName).ToLower();
            file.FileName = updatedFile.FileName+"."+extension;
            _fileService.UpdateFile(file);
            return Ok(file);
        }

        [Authorize]
        [HttpDelete("delete/{fileId}")]
        public ActionResult<models.File> DeleteFile(int fileId){
            if(!_fileService.DeleteFile(ExtractuserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!),fileId,false)) return BadRequest(new {
                error = "File not Found!"
            });
            return Ok(new { message= "File Successfully deleted!"});
        }


        [Authorize]
        [HttpDelete("wipe/{fileId}")]
        public ActionResult<models.File> WipeFile(int fileId){
            if(!_fileService.DeleteFile(ExtractuserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!),fileId,true)) return BadRequest(new {
                error = "File not Found!"
            });
            return Ok(new { message= "File permanently deleted!"});
        }


        [Authorize]
        [HttpGet("download/{fileId}")]
        public IActionResult DownloadFile(int fileId)
        {
            models.File? file = _fileService.GetUserFilebyId(ExtractuserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!),fileId);
            if (file is null || file.IsDeleted == true){
                return BadRequest(new {
                    error = "File not Found!"
                });
            }
            if (string.IsNullOrEmpty(file.FilePath) || string.IsNullOrEmpty(file.FileType))
            {
                return BadRequest(new {
                    error = "File path or Type is invalid or missing!"
                });
            }
            
            var contentDisposition = new System.Net.Mime.ContentDisposition
            {
                FileName = file.FileName,
                Inline = true
            };
            Response.Headers.ContentDisposition = contentDisposition.ToString();
            return PhysicalFile(_fileService.GetFullFilepath(file.FilePath), file.FileType,file.FilePath, enableRangeProcessing: true);
        }

        public int ExtractuserId(string userId)
        {
            return Int32.Parse(userId);
        }
    
    } 
}