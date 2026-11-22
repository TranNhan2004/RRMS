package com.roomrental.webapi.service.auditlog;

import java.util.ArrayList;
import java.util.List;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Propagation;
import org.springframework.transaction.annotation.Transactional;

import com.roomrental.webapi.abstraction.repository.AuditLogRepository;
import com.roomrental.webapi.entity.AuditLog;

@Service
public class AuditService {
    private final AuditLogRepository repository;

    public AuditService(AuditLogRepository repository) {
        this.repository = repository;
    }

    @Transactional(propagation = Propagation.REQUIRES_NEW)
    public void save(List<AuditSnapshot> snapshots) {
        List<AuditLog> records = new ArrayList<>(snapshots.size());
        for (AuditSnapshot snapshot : snapshots) {
            var log = new AuditLog();
            log.setAction(snapshot.action());
            log.setEntityName(snapshot.entityName());
            log.setEntityId(snapshot.entityId());
            log.setOldValues(snapshot.oldValues());
            log.setNewValues(snapshot.newValues());
            log.setTransactionId(snapshot.transactionId());
            log.setCreatedAt(snapshot.createdAt());
            AuditContext context = snapshot.context();
            log.setTraceId(context.traceId());
            log.setRequestId(context.requestId());
            log.setClientActionId(context.clientActionId());
            log.setClientRequestId(context.clientRequestId());
            log.setIpAddress(context.ipAddress());
            log.setActorId(context.actorId());
            records.add(log);
        }
        repository.saveAllAndFlush(records);
    }
}
