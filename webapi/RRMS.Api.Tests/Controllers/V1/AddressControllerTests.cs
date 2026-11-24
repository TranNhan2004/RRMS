using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RRMS.Api.Controllers.V1;
using RRMS.Api.Dtos.V1.Addresses;
using RRMS.Application.Interfaces.Services.Addresses;
using RRMS.Application.Models.Addresses;
using RRMS.Application.Models.Common;
using Xunit;

namespace RRMS.Api.Tests.Controllers.V1;

public class AddressControllerTests
{
    [Fact]
    public async Task GetFilteredProvincesAsync_ShouldReturnMappedResponse_WhenServiceReturnsResults()
    {
        var service = new Mock<IAddressQueryService>();
        var request = new GetFilteredProvincesRequest { CurrentPage = 2, ItemsPerPage = 10 };
        var id = Guid.NewGuid();
        var result = new FilterResult<GetFilteredProvinceResponseItemModel>
        {
            Items = [new GetFilteredProvinceResponseItemModel { Id = id, Name = "Can Tho" }],
            CurrentPage = 2,
            ItemsPerPage = 10,
            TotalItems = 11,
            TotalPages = 2
        };
        service.Setup(x => x.GetFilteredProvincesAsync(It.Is<GetFilteredProvincesRequestModel>(model =>
            model.CurrentPage == 2 && model.ItemsPerPage == 10), CancellationToken.None)).ReturnsAsync(result);
        var controller = new AddressController(service.Object);

        var response = await controller.GetFilteredProvincesAsync(request);

        var ok = response.Should().BeOfType<OkObjectResult>().Subject;
        var body = ok.Value.Should().BeOfType<GetFilteredProvincesResponse>().Subject;
        body.Items.Should().ContainSingle().Which.Id.Should().Be(id);
        body.TotalItems.Should().Be(11);
        service.VerifyAll();
    }

    [Fact]
    public async Task GetFilteredWardsAsync_ShouldPassProvinceIdAndCancellation_WhenRequestIsValid()
    {
        var service = new Mock<IAddressQueryService>();
        var provinceId = Guid.NewGuid();
        var request = new GetFilteredWardsRequest { ProvinceId = provinceId };
        using var cancellation = new CancellationTokenSource();
        service.Setup(x => x.GetFilteredWardsAsync(It.Is<GetFilteredWardsRequestModel>(model =>
            model.ProvinceId == provinceId), cancellation.Token)).ReturnsAsync(new FilterResult<GetFilteredWardResponseItemModel>());
        var controller = new AddressController(service.Object);

        var response = await controller.GetFilteredWardsAsync(request, cancellation.Token);

        var ok = response.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<GetFilteredWardsResponse>();
        service.VerifyAll();
    }
}
