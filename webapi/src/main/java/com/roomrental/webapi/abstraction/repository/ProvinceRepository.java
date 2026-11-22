package com.roomrental.webapi.abstraction.repository;

import java.util.UUID;

import org.springframework.data.jpa.repository.JpaRepository;

import com.roomrental.webapi.entity.Province;

public interface ProvinceRepository extends JpaRepository<Province, UUID>, ProvinceRepositoryCustom {

}
