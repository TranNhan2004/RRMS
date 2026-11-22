package com.roomrental.webapi.mapper;

import com.roomrental.webapi.dto.v1.address.GetFilteredProvinceResponseItem;
import com.roomrental.webapi.dto.v1.address.GetFilteredProvincesRequest;
import com.roomrental.webapi.dto.v1.address.GetFilteredProvincesResponse;
import com.roomrental.webapi.dto.v1.address.GetFilteredWardResponseItem;
import com.roomrental.webapi.dto.v1.address.GetFilteredWardsRequest;
import com.roomrental.webapi.dto.v1.address.GetFilteredWardsResponse;
import com.roomrental.webapi.model.address.GetFilteredProvinceResponseItemModel;
import com.roomrental.webapi.model.address.GetFilteredProvincesRequestModel;
import com.roomrental.webapi.model.address.GetFilteredWardResponseItemModel;
import com.roomrental.webapi.model.address.GetFilteredWardsRequestModel;
import com.roomrental.webapi.model.common.FilterResponseModel;
import com.roomrental.webapi.utils.FilterRequestParser;

public final class AddressMapper {
    private AddressMapper() {
    }

    public static GetFilteredProvincesRequestModel toModel(GetFilteredProvincesRequest request) {
        return new GetFilteredProvincesRequestModel(FilterRequestParser.parse(request));
    }

    public static GetFilteredWardsRequestModel toModel(GetFilteredWardsRequest request) {
        return new GetFilteredWardsRequestModel(FilterRequestParser.parse(request), request.getProvinceId());
    }

    public static GetFilteredProvincesResponse toProvincesResponse(
            FilterResponseModel<GetFilteredProvinceResponseItemModel> result) {
        var items = result
                .getItems()
                .stream()
                .map(item -> new GetFilteredProvinceResponseItem(
                        item.getId(),
                        item.getProvinceCode(),
                        item.getName(),
                        item.getShortName(),
                        item.getCode(),
                        item.getPlaceType(),
                        item.getCountry()))
                .toList();

        return new GetFilteredProvincesResponse(
                items,
                result.getCurrentPage(),
                result.getItemsPerPage(),
                result.getTotalItems(),
                result.getTotalPages(),
                result.isHasPreviousPage(),
                result.isHasNextPage());
    }

    public static GetFilteredWardsResponse toWardsResponse(
            FilterResponseModel<GetFilteredWardResponseItemModel> result) {
        var items = result
                .getItems()
                .stream()
                .map(item -> new GetFilteredWardResponseItem(
                        item.getId(),
                        item.getWardCode(),
                        item.getName(),
                        item.getProvinceId()))
                .toList();

        return new GetFilteredWardsResponse(
                items,
                result.getCurrentPage(),
                result.getItemsPerPage(),
                result.getTotalItems(),
                result.getTotalPages(),
                result.isHasPreviousPage(),
                result.isHasNextPage());
    }
}
