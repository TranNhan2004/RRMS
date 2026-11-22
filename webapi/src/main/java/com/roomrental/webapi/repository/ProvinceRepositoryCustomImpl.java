package com.roomrental.webapi.repository;

import org.springframework.stereotype.Repository;

import com.roomrental.webapi.abstraction.repository.FilterQuery;
import com.roomrental.webapi.abstraction.repository.ProvinceRepositoryCustom;
import com.roomrental.webapi.entity.Province;
import com.roomrental.webapi.model.address.GetFilteredProvinceResponseItemModel;

import jakarta.persistence.criteria.CriteriaBuilder;
import jakarta.persistence.criteria.CriteriaQuery;
import jakarta.persistence.criteria.Root;
import jakarta.persistence.criteria.Selection;
import lombok.RequiredArgsConstructor;

@Repository
@RequiredArgsConstructor
public class ProvinceRepositoryCustomImpl implements ProvinceRepositoryCustom {

    @Override
    public FilterQuery<Province, GetFilteredProvinceResponseItemModel> findFilteredProvincesQuery() {
        return new FilterQuery<>() {
            @Override
            public Class<Province> entityType() {
                return Province.class;
            }

            @Override
            public void configure(CriteriaBuilder builder, CriteriaQuery<?> query, Root<Province> root) {
            }

            @Override
            public Selection<GetFilteredProvinceResponseItemModel> select(
                    CriteriaBuilder builder,
                    Root<Province> root,
                    Class<GetFilteredProvinceResponseItemModel> modelType) {
                return builder.construct(modelType,
                        root.get("id"),
                        root.get("provinceCode"),
                        root.get("name"),
                        root.get("shortName"),
                        root.get("code"),
                        root.get("placeType"),
                        root.get("country"));
            }
        };
    }
}
