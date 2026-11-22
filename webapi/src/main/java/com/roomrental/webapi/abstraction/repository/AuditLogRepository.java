package com.roomrental.webapi.abstraction.repository;

import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;
import com.roomrental.webapi.entity.AuditLog;

public interface AuditLogRepository extends JpaRepository<AuditLog, UUID> {
}
