package com.roomrental.webapi.dto.v1.address;

import java.util.List;

import com.roomrental.webapi.dto.common.FilterResponse;

public class GetFilteredProvincesResponse extends FilterResponse<GetFilteredProvinceResponseItem> {
    public GetFilteredProvincesResponse(
            List<GetFilteredProvinceResponseItem> items,
            int currentPage,
            int itemsPerPage,
            long totalItems,
            int totalPages,
            boolean hasPreviousPage,
            boolean hasNextPage) {
        super(items, currentPage, itemsPerPage, totalItems, totalPages, hasPreviousPage, hasNextPage);
    }
}
