package com.roomrental.webapi.model.common;

import com.roomrental.webapi.enums.common.FilterOperator;

import lombok.Getter;
import lombok.AllArgsConstructor;
import java.util.List;

@Getter
@AllArgsConstructor
public class FilterCriterion {
    private final String fieldName;
    private final FilterOperator operator;
    private final List<String> values;
    private final DateTimeFilterOptions dateTimeFilterOptions;

}
