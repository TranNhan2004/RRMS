package com.roomrental.webapi.model.address;

import com.roomrental.webapi.model.common.FilterRequestModel;

public class GetFilteredProvincesRequestModel extends FilterRequestModel<GetFilteredProvinceResponseItemModel> {
    public GetFilteredProvincesRequestModel(FilterRequestModel<GetFilteredProvinceResponseItemModel> request) {
        super(request);
    }
}
