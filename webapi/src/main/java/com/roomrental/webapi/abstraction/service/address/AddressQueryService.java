package com.roomrental.webapi.abstraction.service.address;

import com.roomrental.webapi.model.address.*;
import com.roomrental.webapi.model.common.FilterResponseModel;

public interface AddressQueryService {
    FilterResponseModel<GetFilteredProvinceResponseItemModel> getFilteredProvinces(GetFilteredProvincesRequestModel request);
    FilterResponseModel<GetFilteredWardResponseItemModel> getFilteredWards(GetFilteredWardsRequestModel request);
}
