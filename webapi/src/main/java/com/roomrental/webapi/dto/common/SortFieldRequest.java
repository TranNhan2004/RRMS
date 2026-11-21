package com.roomrental.webapi.dto.common;

import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.Getter;
import lombok.AllArgsConstructor;
import lombok.Setter;
import lombok.NoArgsConstructor;
import jakarta.validation.constraints.NotBlank;

@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class SortFieldRequest {
    @NotBlank
    private String fieldName;
    @Getter(onMethod_ = @JsonProperty("isAscending"))
    @Setter(onMethod_ = @JsonProperty("isAscending"))
    private boolean ascending;

}
