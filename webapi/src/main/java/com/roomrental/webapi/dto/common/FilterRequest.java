package com.roomrental.webapi.dto.common;

import java.util.List;

import jakarta.validation.Valid;
import jakarta.validation.constraints.Min;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;
import com.fasterxml.jackson.annotation.JsonIgnore;
import com.roomrental.webapi.model.common.FilterFieldMap;

@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public abstract class FilterRequest<TModel> {
    @Min(1)
    private Integer currentPage;
    @Min(1)
    private Integer itemsPerPage;
    private String searchGlobalText;
    private String groupPattern;
    @Valid
    private List<FilterCriterionRequest> filterCriteria;
    @Valid
    private List<SortFieldRequest> sortFields;
    private String timeZoneId;

    /** Server-defined, trusted mappings from response fields to entity paths. */
    @JsonIgnore
    public abstract FilterFieldMap getFilterFieldMap();
}
