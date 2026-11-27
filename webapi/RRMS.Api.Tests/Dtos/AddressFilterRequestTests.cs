using AwesomeAssertions;
using RRMS.Api.Dtos.Common;
using RRMS.Api.Dtos.V1.Addresses;
using RRMS.Domain.Exceptions;
using Xunit;

namespace RRMS.Api.Tests.Dtos;

public class AddressFilterRequestTests
{
    [Fact]
    public void ToModel_ShouldRejectUnknownFields_WhenFieldIsNotAllowlisted()
    {
        // Arrange
        var request = new GetFilteredProvincesRequest
        {
            FilterCriteria = [new FilterCriterionRequest
            {
                FieldName = "secret",
                Operator = "Equals",
                Values = ["x"]
            }]
        };

        // Act
        var action = () => GetFilteredProvincesRequest.ToModel(request);

        // Assert
        action.Should().Throw<UnprocessableEntityException>();
    }

    [Fact]
    public void ToModel_ShouldRejectTimeZone_WhenTimeZoneIsUnknown()
    {
        // Arrange
        var request = new GetFilteredProvincesRequest
        {
            TimeZoneId = "invalid/timezone"
        };

        // Act
        var action = () => GetFilteredProvincesRequest.ToModel(request);

        // Assert
        action.Should().Throw<UnprocessableEntityException>();
    }
}
