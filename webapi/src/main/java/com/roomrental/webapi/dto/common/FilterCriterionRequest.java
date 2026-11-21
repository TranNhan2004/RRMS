package com.roomrental.webapi.dto.common;

import lombok.Getter;
import lombok.AllArgsConstructor;
import lombok.Setter;
import lombok.NoArgsConstructor;
import java.util.List;

import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;

@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class FilterCriterionRequest {
    @NotBlank
    private String fieldName;
    @NotBlank
    private String operator;
    private List<String> values;
    @Valid
    private DateTimeFilterOptionsRequest dateTimeFilterOptions;

}
