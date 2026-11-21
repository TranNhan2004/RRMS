package com.roomrental.webapi.entity;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

@Getter
@Setter
@NoArgsConstructor
@Entity
@Table(name = "provinces")
public class Province extends BaseEntity {
    @Column(name = "province_code", nullable = false, length = 10, unique = true)
    private String provinceCode;

    @Column(name = "name", nullable = false, length = 255, unique = true)
    private String name;

    @Column(name = "short_name", nullable = false, length = 255)
    private String shortName;

    @Column(name = "code", nullable = false, length = 10, unique = true)
    private String code;

    @Column(name = "place_type", nullable = false, length = 255)
    private String placeType;

    @Column(name = "country", nullable = false, length = 255)
    private String country;
}
