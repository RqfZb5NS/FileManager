using FileManager.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FileManager.Core.Interfaces.Services;
public interface IPublicFileService
    {
        Task<FileEntity> UploadPublicFileAsync(Guid userId, Guid? folderId, Stream fileStream, string fileName, string contentType);
        Task<IEnumerable<FileEntity>> GetPublicFilesAsync(int pageNumber, int pageSize);
        Task<IEnumerable<FileEntity>> GetUserPublicFilesAsync(Guid userId, int pageNumber, int pageSize);
        Task<FileEntity> GetPublicFileByIdAsync(Guid fileId);
        Task<Stream> DownloadPublicFileAsync(Guid fileId);
        Task DeletePublicFileAsync(Guid userId, Guid fileId);
    }