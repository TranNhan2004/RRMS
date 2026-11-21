package com.roomrental.webapi.model.common;

import java.time.ZoneId;
import java.util.List;

import lombok.AllArgsConstructor;
import lombok.Getter;

@Getter
@AllArgsConstructor
public class FilterRequestModel<TModel> {
    private final int currentPage;
    private final int itemsPerPage;
    private final String searchGlobalText;
    private final FilterGroup filterGroup;
    private final List<FilterCriterion> filterCriteria;
    private final List<SortField> sortFields;
    private final ZoneId timeZoneId;
    private final FilterFieldMap filterFieldMap;

    protected FilterRequestModel(FilterRequestModel<TModel> request) {
        this(request.currentPage,
                request.itemsPerPage,
                request.searchGlobalText,
                request.filterGroup,
                request.filterCriteria,
                request.sortFields,
                request.timeZoneId,
                request.filterFieldMap.copy());
    }
}
