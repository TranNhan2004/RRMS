package com.roomrental.webapi.abstraction.repository;

import java.util.UUID;

import org.springframework.data.jpa.repository.JpaRepository;

import com.roomrental.webapi.entity.Ward;

public interface WardRepository extends JpaRepository<Ward, UUID>, WardRepositoryCustom {

}
