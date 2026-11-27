using AwesomeAssertions;
using RRMS.Api.Utils;
using RRMS.Domain.Enums.Common;
using RRMS.Domain.Exceptions;
using Xunit;

namespace RRMS.Api.Tests.Utils;

public class FilterGroupConverterTests
{
    [Fact]
    public void Build_ShouldAppendMissingCriteria_WhenPatternOmitsCriterion()
    {
        // Arrange
        const string pattern = "{0} OR (NOT {1})";
        const int criterionCount = 3;

        // Act
        var result = FilterGroupConverter.Build(pattern, criterionCount);

        // Assert
        result.Should().NotBeNull();
        result!.Logic.Should().Be(FilterGroupLogic.And);
        result.Children.Should().HaveCount(2);
        result.Children[0].Group!.Logic.Should().Be(FilterGroupLogic.Or);
        result.Children[1].CriterionIndex.Should().Be(2);
    }

    [Theory]
    [InlineData("{9}")]
    [InlineData("{0} AND")]
    [InlineData("({0}")]
    public void Build_ShouldRejectInvalidPattern_WhenPatternIsInvalid(string pattern)
    {
        // Arrange
        const int criterionCount = 3;

        // Act
        var action = () => FilterGroupConverter.Build(pattern, criterionCount);

        // Assert
        action.Should().Throw<UnprocessableEntityException>();
    }
}
