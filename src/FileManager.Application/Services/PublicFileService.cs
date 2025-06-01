using FileManager.Application.Services;
using FileManager.Core.Entities;
using FileManager.Core.Interfaces.Repositories; // Assuming IPublicStorageRepository is here
using FileManager.Core.Interfaces.Services;
using FileManager.Infrastructure.Data; // For AppDbContext
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FileManager.Application.Services
{
    public class PublicFileService : IPublicFileService
    {
        private readonly IPublicStorageRepository _publicStorageRepository;
        private readonly AppDbContext _dbContext; // Assuming AppDbContext is used for entity operations

        public PublicFileService(IPublicStorageRepository publicStorageRepository, AppDbContext dbContext)
        {
            _publicStorageRepository = publicStorageRepository;
            _dbContext = dbContext;
        }

        public async Task<FileEntity> UploadPublicFileAsync(Guid userId, Guid? folderId, Stream fileStream, string fileName, string contentType)
        {
            // Logic: Create file entity, save to public storage, add to DB
            var fileId = Guid.NewGuid();
            var storagePath = $"public/{userId}/{fileId}_{fileName}"; // Example path

            // Save to actual storage
            await _publicStorageRepository.UploadFileAsync(storagePath, fileStream);

            // Create FileEntity in DB
            var fileEntity = new FileEntity
            {
                Id = fileId, // Set the new Guid Id
                OwnerId = userId,
                FolderId = folderId,
                FileName = fileName,
                ContentType = contentType,
                Size = fileStream.Length, // This will be the current length of the stream
                StoragePath = storagePath,
                StorageType = StorageType.Public, // Set to Public
                UploadedAt = DateTime.UtcNow,
                // UpdatedAt will be set by SaveChangesAsync in AppDbContext
            };

            _dbContext.Files.Add(fileEntity);
            await _dbContext.SaveChangesAsync();

            return fileEntity;
        }

        public async Task<IEnumerable<FileEntity>> GetPublicFilesAsync(int pageNumber, int pageSize)
        {
            // Logic: Retrieve all public files with pagination
            return await _dbContext.Files
                .Where(f => f.StorageType == StorageType.Public)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<FileEntity>> GetUserPublicFilesAsync(Guid userId, int pageNumber, int pageSize)
        {
            // Logic: Retrieve public files for a specific user with pagination
            return await _dbContext.Files
                .Where(f => f.OwnerId == userId && f.StorageType == StorageType.Public)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<FileEntity> GetPublicFileByIdAsync(Guid fileId)
        {
            // Logic: Retrieve a specific public file
            return await _dbContext.Files
                .FirstOrDefaultAsync(f => f.Id == fileId && f.StorageType == StorageType.Public);
        }

        public async Task<Stream> DownloadPublicFileAsync(Guid fileId)
        {
            // Logic: Retrieve file entity, then download from public storage
            var fileEntity = await _dbContext.Files
                .FirstOrDefaultAsync(f => f.Id == fileId && f.StorageType == StorageType.Public);

            if (fileEntity == null)
            {
                return null; // Or throw a specific NotFoundException
            }

            return await _publicStorageRepository.DownloadFileAsync(fileEntity.StoragePath);
        }

        public async Task DeletePublicFileAsync(Guid userId, Guid fileId)
        {
            // Logic: Delete file from DB and public storage, only if owner matches
            var fileEntity = await _dbContext.Files
                .FirstOrDefaultAsync(f => f.Id == fileId && f.OwnerId == userId && f.StorageType == StorageType.Public);

            if (fileEntity == null)
            {
                // File not found or not owned by user, throw appropriate exception
                throw new InvalidOperationException("Public file not found or not authorized to delete.");
            }

            // Delete from actual storage first
            await _publicStorageRepository.DeleteFileAsync(fileEntity.StoragePath);

            // Delete from DB
            _dbContext.Files.Remove(fileEntity);
            await _dbContext.SaveChangesAsync();
        }
    }
}