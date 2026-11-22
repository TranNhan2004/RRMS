package com.roomrental.webapi.repository;

import java.util.UUID;

import org.springframework.stereotype.Repository;

import com.roomrental.webapi.abstraction.repository.FilterQuery;
import com.roomrental.webapi.abstraction.repository.WardRepositoryCustom;
import com.roomrental.webapi.entity.Ward;
import com.roomrental.webapi.model.address.GetFilteredWardResponseItemModel;

import jakarta.persistence.criteria.CriteriaBuilder;
import jakarta.persistence.criteria.CriteriaQuery;
import jakarta.persistence.criteria.Root;
import jakarta.persistence.criteria.Selection;
import lombok.RequiredArgsConstructor;

@Repository
@RequiredArgsConstructor
public class WardRepositoryCustomImpl implements WardRepositoryCustom {
    @Override
    public FilterQuery<Ward, GetFilteredWardResponseItemModel> findFilteredWardsQuery(UUID provinceId) {
        java.util.Objects.requireNonNull(provinceId, "provinceId");
        return new FilterQuery<>() {
            @Override
            public Class<Ward> entityType() {
                return Ward.class;
            }

            @Override
            public void configure(CriteriaBuilder builder, CriteriaQuery<?> query, Root<Ward> root) {
                query.where(builder.equal(root.get("province").get("id"), provinceId));
            }

            @Override
            public Selection<GetFilteredWardResponseItemModel> select(
                    CriteriaBuilder builder,
                    Root<Ward> root,
                    Class<GetFilteredWardResponseItemModel> modelType) {
                return builder.construct(
                        modelType,
                        root.get("id"),
                        root.get("wardCode"),
                        root.get("name"),
                        root.get("province").get("id"));
            }
        };
    }
}
