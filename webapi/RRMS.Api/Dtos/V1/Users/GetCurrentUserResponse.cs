using RRMS.Application.Models.Users;

namespace RRMS.Api.Dtos.V1.Users;

public class GetCurrentUserResponse
{
    public Guid Id { get; init; }
    public string? UserName { get; init; }
    public string Email { get; init; } = null!;
    public GetCurrentUserProfileResponse? Profile { get; init; }

    public static GetCurrentUserResponse FromModel(GetCurrentUserResponseModel model) => new()
    {
        Id = model.Id,
        UserName = model.UserName,
        Email = model.Email,
        Profile = model.Profile == null ? null : GetCurrentUserProfileResponse.FromModel(model.Profile)
    };
}

public class GetCurrentUserProfileResponse
{
    public string? NationalIdNumber { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public DateOnly? BirthDate { get; init; }
    public string? Sex { get; init; }
    public DateTimeOffset? AddressUpdatedAt { get; init; }

    public static GetCurrentUserProfileResponse FromModel(GetCurrentUserProfileResponseModel model) => new()
    {
        NationalIdNumber = model.NationalIdNumber,
        FirstName = model.FirstName,
        LastName = model.LastName,
        BirthDate = model.BirthDate,
        Sex = model.Sex?.ToString().ToUpperInvariant(),
        AddressUpdatedAt = model.AddressUpdatedAt
    };
}
