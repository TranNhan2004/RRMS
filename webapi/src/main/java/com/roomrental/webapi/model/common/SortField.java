package com.roomrental.webapi.model.common;

import lombok.Getter;
import lombok.AllArgsConstructor;
@Getter
@AllArgsConstructor
public class SortField {
    private final String fieldName;
    private final boolean ascending;

}
