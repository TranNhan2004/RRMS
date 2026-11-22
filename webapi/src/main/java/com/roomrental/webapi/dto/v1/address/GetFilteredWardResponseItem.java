package com.roomrental.webapi.dto.v1.address;

import java.util.UUID;

import lombok.AllArgsConstructor;
import lombok.Getter;

@Getter
@AllArgsConstructor
public class GetFilteredWardResponseItem {
    private UUID id;
    private String wardCode;
    private String name;
    private UUID provinceId;
}
