package com.roomrental.webapi.service.address;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import com.roomrental.webapi.abstraction.repository.ProvinceRepository;
import com.roomrental.webapi.abstraction.repository.WardRepository;
import com.roomrental.webapi.abstraction.service.address.AddressQueryService;
import com.roomrental.webapi.model.address.GetFilteredProvinceResponseItemModel;
import com.roomrental.webapi.model.address.GetFilteredProvincesRequestModel;
import com.roomrental.webapi.model.address.GetFilteredWardResponseItemModel;
import com.roomrental.webapi.model.address.GetFilteredWardsRequestModel;
import com.roomrental.webapi.model.common.FilterResponseModel;
import com.roomrental.webapi.repository.FilterHandler;

import lombok.RequiredArgsConstructor;

@Service
@RequiredArgsConstructor
@Transactional(readOnly = true)
public class AddressQueryServiceImpl implements AddressQueryService {
    private final ProvinceRepository provinceRepository;
    private final WardRepository wardRepository;
    private final FilterHandler filterHandler;

    @Override
    public FilterResponseModel<GetFilteredProvinceResponseItemModel> getFilteredProvinces(
            GetFilteredProvincesRequestModel request) {
        return filterHandler.applyFilter(
                request,
                provinceRepository.findFilteredProvincesQuery(),
                GetFilteredProvinceResponseItemModel.class);
    }

    @Override
    public FilterResponseModel<GetFilteredWardResponseItemModel> getFilteredWards(
            GetFilteredWardsRequestModel request) {
        return filterHandler.applyFilter(
                request,
                wardRepository.findFilteredWardsQuery(request.getProvinceId()),
                GetFilteredWardResponseItemModel.class);
    }
}
