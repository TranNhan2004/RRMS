namespace RRMS.Domain.Constants.CacheKeys;

public static class FileStorageCacheKeys
{
    private const string GetPresignedDownloadUrl = "rrms:file-storages:download:{fileId}";
    private const string GetPresignedViewUrl = "rrms:file-storages:view:{fileId}";

    public static CacheKeyObject GetPresignedDownloadUrlKey(string fileId, TimeSpan? expiry = null) => new()
    {
        Key = GetPresignedDownloadUrl.Replace("{fileId}", fileId),
        Expiration = expiry
    };

    public static CacheKeyObject GetPresignedViewUrlKey(string fileId, TimeSpan? expiry = null) => new()
    {
        Key = GetPresignedViewUrl.Replace("{fileId}", fileId),
        Expiration = expiry
    };

}
