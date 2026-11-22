package com.roomrental.webapi.model.address;

import java.util.UUID;

import com.roomrental.webapi.model.common.FilterRequestModel;

import lombok.Getter;

@Getter
public class GetFilteredWardsRequestModel extends FilterRequestModel<GetFilteredWardResponseItemModel> {
    private UUID provinceId;

    public GetFilteredWardsRequestModel(FilterRequestModel<GetFilteredWardResponseItemModel> request, UUID provinceId) {
        super(request);
        this.provinceId = provinceId;
    }
}
