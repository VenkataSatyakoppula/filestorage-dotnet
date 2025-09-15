using System.Net.Mime;
using System.Text;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.EntityFrameworkCore.Infrastructure;

public class CloudflareR2Service
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public CloudflareR2Service(IConfiguration config)
    {
        var accountId = config["CloudflareR2:AccountId"];
        var accessKeyId = config["CloudflareR2:AccessKeyId"];
        var secretAccessKey = config["CloudflareR2:SecretAccessKey"];
        _bucketName = config["CloudflareR2:BucketName"] ?? "filestorage";

        var r2Endpoint = $"https://{accountId}.r2.cloudflarestorage.com";

        var s3Config = new AmazonS3Config
        {
            ServiceURL = r2Endpoint,
            ForcePathStyle = true
        };

        _s3Client = new AmazonS3Client(accessKeyId, secretAccessKey, s3Config);
    }

    public bool Upload(string key, IFormFile file)
    {
        try
        {

            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = file.OpenReadStream(),
                DisablePayloadSigning = true,
                DisableDefaultChecksumValidation = true,
                ContentType = file.ContentType,
            };
            putRequest.Metadata.Add("Content-Disposition",$"attachment; filename=\"{file.FileName}\"");
            putRequest.Metadata.Add("Content-Type",file.ContentType);
            _s3Client.PutObjectAsync(putRequest).GetAwaiter().GetResult();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Upload failed: {ex.Message}");
            return false;
        }
    }

    public async Task<Amazon.S3.Model.GetObjectResponse> GetS3ObjectAsync(string key)
    {
        var getRequest = new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = key
        };

        using var response = await _s3Client.GetObjectAsync(getRequest);
        return response;
    }

    public string GetPresignedUrl(crud_api.models.File file)
    {
        var presign = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = file.FilePath,
            Expires = DateTime.Now.AddMinutes(15),
            ResponseHeaderOverrides = new ResponseHeaderOverrides
            {
                ContentDisposition = $"attachment; filename=\"{file.FileName}\"",
                ContentType = file.FileType
            }
        };

        var presignedUrl = _s3Client.GetPreSignedURL(presign);
        return presignedUrl;
    }


    public long Delete(string key)
    {
        try
        {
            var metadata = _s3Client.GetObjectMetadataAsync(_bucketName, key).GetAwaiter().GetResult();
            long size = metadata.ContentLength;
            _s3Client.DeleteObjectAsync(_bucketName, key).GetAwaiter().GetResult();
            return size;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Upload failed: {ex.Message}");
            return 0;
        }
    }
}
