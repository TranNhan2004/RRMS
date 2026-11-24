using Moq;
using RRMS.Application.Interfaces.Repositories;
using RRMS.Application.Models.Addresses;
using RRMS.Application.Models.Common;
using RRMS.Application.Services.Addresses;
using Xunit;

namespace RRMS.Application.Tests.Services.Addresses;

public class AddressQueryServiceTests
{
    [Fact]
    public async Task GetFilteredProvincesAsync_ReturnsRepositoryResult()
    {
        var provinces = new Mock<IProvinceRepository>();
        var wards = new Mock<IWardRepository>();
        var request = new GetFilteredProvincesRequestModel();
        var expected = new FilterResult<GetFilteredProvinceResponseItemModel>();
        provinces.Setup(x => x.FindFilteredProvincesAsync(request, CancellationToken.None)).ReturnsAsync(expected);
        var service = new AddressQueryService(provinces.Object, wards.Object);

        var result = await service.GetFilteredProvincesAsync(request);

        Assert.Same(expected, result);
        provinces.Verify(x => x.FindFilteredProvincesAsync(request, CancellationToken.None), Times.Once);
        wards.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetFilteredWardsAsync_PassesProvinceFilterAndCancellationToken()
    {
        var provinces = new Mock<IProvinceRepository>();
        var wards = new Mock<IWardRepository>();
        var request = new GetFilteredWardsRequestModel { ProvinceId = Guid.NewGuid() };
        using var source = new CancellationTokenSource();
        var expected = new FilterResult<GetFilteredWardResponseItemModel>();
        wards.Setup(x => x.FindFilteredWardsAsync(request, source.Token)).ReturnsAsync(expected);
        var service = new AddressQueryService(provinces.Object, wards.Object);

        var result = await service.GetFilteredWardsAsync(request, source.Token);

        Assert.Same(expected, result);
        wards.Verify(x => x.FindFilteredWardsAsync(request, source.Token), Times.Once);
        provinces.VerifyNoOtherCalls();
    }
}
