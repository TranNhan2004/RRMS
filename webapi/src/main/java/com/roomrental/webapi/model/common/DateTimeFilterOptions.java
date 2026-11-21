package com.roomrental.webapi.model.common;

import com.roomrental.webapi.enums.common.TemporalPartType;

import lombok.Getter;
import lombok.AllArgsConstructor;
@Getter
@AllArgsConstructor
public class DateTimeFilterOptions {
    private final int offsetMinutes;
    private final TemporalPartType temporalPartType;

}
