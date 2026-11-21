package com.roomrental.webapi.model.common;

import lombok.Getter;
import lombok.AllArgsConstructor;
import java.util.List;

@Getter
@AllArgsConstructor
public class FilterResponseModel<T> {
    private final List<T> items;
    private final int currentPage;
    private final int itemsPerPage;
    private final long totalItems;
    private final int totalPages;
    private final boolean hasPreviousPage;
    private final boolean hasNextPage;

}
