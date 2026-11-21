package com.roomrental.webapi.dto.common;

import lombok.Getter;
import lombok.AllArgsConstructor;
import lombok.Setter;
import lombok.NoArgsConstructor;
import jakarta.validation.constraints.NotBlank;

@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class DateTimeFilterOptionsRequest {
    private int offsetMinutes;
    @NotBlank
    private String temporalPartType;

}
