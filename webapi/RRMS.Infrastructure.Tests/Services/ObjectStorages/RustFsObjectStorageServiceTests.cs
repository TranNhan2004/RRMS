using Amazon.S3;
using Amazon.S3.Model;
using AwesomeAssertions;
using Microsoft.Extensions.Options;
using Moq;
using RRMS.Infrastructure.Options;
using RRMS.Infrastructure.Services.ObjectStorages;
using Xunit;

namespace RRMS.Infrastructure.Tests.Services.ObjectStorages;

public class RustFsObjectStorageServiceTests
{
    [Fact]
    public async Task UploadAsync_ShouldRequireAnUnusedObjectKey_WhenUploading()
    {
        // Arrange
        var client = new Mock<IAmazonS3>();
        PutObjectRequest? actual = null;
        client.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((request, _) => actual = request)
            .ReturnsAsync(new PutObjectResponse());
        var service = new RustFsObjectStorageService(client.Object, Microsoft.Extensions.Options.Options.Create(new ObjectStorageOptions
        {
            Endpoint = "http://localhost:9000",
            BucketName = "files"
        }));
        using var content = new MemoryStream();

        // Act
        await service.UploadAsync("key", content, "text/plain");

        // Assert
        actual.Should().NotBeNull();
        actual!.IfNoneMatch.Should().Be("*");
        actual.Key.Should().Be("key");
    }
}
