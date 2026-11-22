package com.roomrental.webapi.model.address;

import java.util.UUID;

import lombok.AllArgsConstructor;
import lombok.Getter;

@Getter
@AllArgsConstructor
public class GetFilteredWardResponseItemModel {
    private UUID id;
    private String wardCode;
    private String name;
    private UUID provinceId;
}
