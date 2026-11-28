namespace RRMS.Domain.Constants.CacheKeys;

public static class DataProtectionKeyCacheKeys
{
    private const string DpkRing = "rrms:data-protection-keys";

    public static CacheKeyObject GetDpkRingKey() => new()
    {
        Key = DpkRing
    };
}
