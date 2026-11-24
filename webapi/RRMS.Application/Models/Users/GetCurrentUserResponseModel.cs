using RRMS.Domain.Enums.Users;

namespace RRMS.Application.Models.Users;

public class GetCurrentUserResponseModel
{
    public Guid Id { get; init; }
    public string? UserName { get; init; }
    public string Email { get; init; } = null!;
    public GetCurrentUserProfileResponseModel? Profile { get; init; }
}

public class GetCurrentUserProfileResponseModel
{
    public string? NationalIdNumber { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public DateOnly? BirthDate { get; init; }
    public UserSex? Sex { get; init; }
    public DateTimeOffset? AddressUpdatedAt { get; init; }
}
