package com.roomrental.webapi.service.auditlog;

import java.time.Instant;
import java.util.ArrayList;
import java.util.IdentityHashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;
import org.springframework.transaction.support.TransactionSynchronization;
import org.springframework.transaction.support.TransactionSynchronizationManager;

import com.roomrental.webapi.enums.common.AuditAction;
import com.roomrental.webapi.utils.UUIDUtils;

@Component
public class AuditTransactionBuffer {
    private static final Logger log = LoggerFactory.getLogger(AuditTransactionBuffer.class);
    private final Object resourceKey = new Object();
    private final AuditQueue queue;

    public AuditTransactionBuffer(AuditQueue queue) {
        this.queue = queue;
    }

    private static final class Buffer {
        final UUID id = UUIDUtils.generateV7();
        final List<AuditSnapshot> records = new ArrayList<>();
    }

    public void add(AuditAction action, String entityName, String entityId, String oldValues, String newValues) {
        if (!TransactionSynchronizationManager.isSynchronizationActive()
                || !TransactionSynchronizationManager.isActualTransactionActive()) {
            log.error("Audit event skipped because no Spring-managed transaction is active: entity={}", entityName);
            return;
        }
        Buffer buffer = (Buffer) TransactionSynchronizationManager.getResource(resourceKey);
        if (buffer == null) {
            buffer = new Buffer();
            Buffer registered = buffer;
            TransactionSynchronizationManager.bindResource(resourceKey, registered);
            TransactionSynchronizationManager.registerSynchronization(new TransactionSynchronization() {
                private final Map<Object, Integer> savepoints = new IdentityHashMap<>();

                @Override
                public void suspend() {
                    TransactionSynchronizationManager.unbindResource(resourceKey);
                }

                @Override
                public void resume() {
                    TransactionSynchronizationManager.bindResource(resourceKey, registered);
                }

                @Override
                public void savepoint(Object savepoint) {
                    savepoints.put(savepoint, registered.records.size());
                }

                @Override
                public void savepointRollback(Object savepoint) {
                    Integer size = savepoints.remove(savepoint);
                    if (size != null)
                        registered.records.subList(size, registered.records.size()).clear();
                }

                @Override
                public void afterCompletion(int status) {
                    TransactionSynchronizationManager.unbindResourceIfPossible(resourceKey);
                    // Only publish after the database commit has succeeded; rollbacks and unknown
                    // outcomes are discarded.
                    if (status == STATUS_COMMITTED)
                        registered.records.forEach(queue::submit);
                }
            });
        }
        buffer.records.add(new AuditSnapshot(action, entityName, entityId, oldValues, newValues,
                AuditContext.capture(), buffer.id, Instant.now()));
    }

}
