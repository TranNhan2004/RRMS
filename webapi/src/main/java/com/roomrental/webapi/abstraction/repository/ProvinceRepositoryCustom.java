package com.roomrental.webapi.abstraction.repository;

import com.roomrental.webapi.entity.Province;
import com.roomrental.webapi.model.address.GetFilteredProvinceResponseItemModel;

public interface ProvinceRepositoryCustom {
    FilterQuery<Province, GetFilteredProvinceResponseItemModel> findFilteredProvincesQuery();
}
