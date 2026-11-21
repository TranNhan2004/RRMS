package com.roomrental.webapi.dto.common;

import lombok.Getter;
import lombok.AllArgsConstructor;
import lombok.Setter;
import lombok.NoArgsConstructor;
import java.util.List;

@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class FilterResponse<T> {
    private List<T> items;
    private int currentPage;
    private int itemsPerPage;
    private long totalItems;
    private int totalPages;
    private boolean hasPreviousPage;
    private boolean hasNextPage;

}
