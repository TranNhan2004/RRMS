using AwesomeAssertions;
using Moq;
using RRMS.Application.Interfaces.Repositories;
using RRMS.Application.Interfaces.Services.ObjectStorages;
using RRMS.Application.Services.FileStorages;
using RRMS.Domain.Entities;
using Xunit;

namespace RRMS.Application.Tests.Services.FileStorages;

public class FileStorageCommandServiceTests
{
    [Fact]
    public async Task CreateFileStorageAsync_ShouldDeleteUploadedObject_WhenMetadataSaveFails()
    {
        // Arrange
        var repository = new Mock<IFileStorageRepository>();
        var storage = new Mock<IObjectStorageService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var file = new FileStorage
        {
            CreatedById = Guid.NewGuid(),
            ObjectKey = "files/example",
            ContentType = "text/plain"
        };
        using var stream = new MemoryStream();
        unitOfWork.Setup(x => x.SaveChangesAsync(file.CreatedById.Value.ToString(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("save failed"));
        var service = new FileStorageCommandService(repository.Object, storage.Object, unitOfWork.Object);

        // Act
        var action = () => service.CreateFileStorageAsync(stream, file);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("save failed");
        storage.Verify(x => x.UploadAsync(file.ObjectKey, stream, file.ContentType, It.IsAny<CancellationToken>()), Times.Once);
        storage.Verify(x => x.DeleteAsync(file.ObjectKey, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CreateFileStorageAsync_ShouldNotPersistOrDelete_WhenUploadFails()
    {
        // Arrange
        var repository = new Mock<IFileStorageRepository>();
        var storage = new Mock<IObjectStorageService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var file = new FileStorage
        {
            CreatedById = Guid.NewGuid(),
            ObjectKey = "files/existing",
            ContentType = "text/plain"
        };
        using var stream = new MemoryStream();
        storage.Setup(x => x.UploadAsync(file.ObjectKey, stream, file.ContentType, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("upload failed"));
        var service = new FileStorageCommandService(repository.Object, storage.Object, unitOfWork.Object);

        // Act
        var action = () => service.CreateFileStorageAsync(stream, file);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("upload failed");
        repository.Verify(x => x.Add(It.IsAny<FileStorage>()), Times.Never);
        storage.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
