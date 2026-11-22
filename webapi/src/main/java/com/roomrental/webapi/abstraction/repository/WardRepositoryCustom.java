package com.roomrental.webapi.abstraction.repository;

import java.util.UUID;

import com.roomrental.webapi.entity.Ward;
import com.roomrental.webapi.model.address.GetFilteredWardResponseItemModel;

public interface WardRepositoryCustom {
    FilterQuery<Ward, GetFilteredWardResponseItemModel> findFilteredWardsQuery(UUID provinceId);
}
