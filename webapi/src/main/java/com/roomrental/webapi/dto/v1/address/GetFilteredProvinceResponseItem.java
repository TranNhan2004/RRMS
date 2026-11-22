package com.roomrental.webapi.dto.v1.address;

import java.util.UUID;
import lombok.AllArgsConstructor;
import lombok.Getter;

@Getter
@AllArgsConstructor
public class GetFilteredProvinceResponseItem {
    private UUID id;
    private String provinceCode;
    private String name;
    private String shortName;
    private String code;
    private String placeType;
    private String country;
}
