package com.roomrental.webapi.dto.v1.address;

import com.roomrental.webapi.dto.common.FilterRequest;
import com.roomrental.webapi.model.address.GetFilteredProvinceResponseItemModel;
import com.roomrental.webapi.model.common.FilterFieldMap;

public class GetFilteredProvincesRequest extends FilterRequest<GetFilteredProvinceResponseItemModel> {
    @Override
    public FilterFieldMap getFilterFieldMap() {
        return new FilterFieldMap()
                .map("name", "name", String.class);
    }
}
