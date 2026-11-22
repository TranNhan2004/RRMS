package com.roomrental.webapi.service.auditlog;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;

import java.time.Instant;
import java.util.List;

import org.junit.jupiter.api.Test;
import org.mockito.ArgumentCaptor;

import com.roomrental.webapi.abstraction.repository.AuditLogRepository;
import com.roomrental.webapi.entity.AuditLog;
import com.roomrental.webapi.enums.common.AuditAction;
import com.roomrental.webapi.utils.UUIDUtils;

class AuditServiceTests {
    @Test
    @SuppressWarnings({ "rawtypes", "unchecked" })
    void save_ShouldPreserveSnapshotFields_WhenRecordsAreSaved() {
        // Arrange
        var repository = mock(AuditLogRepository.class);
        var service = new AuditService(repository);
        var context = new AuditContext("trace", "request", "action", "client", "127.0.0.1", "user");
        var transactionId = UUIDUtils.generateV7();
        var created = Instant.now();
        ArgumentCaptor<List<AuditLog>> captor = ArgumentCaptor.forClass((Class) List.class);

        // Act
        service.save(List.of(new AuditSnapshot(AuditAction.UPDATE, "Item", "42", "{\"name\":null}",
                "{\"name\":\"new\"}", context, transactionId, created)));

        // Assert
        verify(repository).saveAllAndFlush(captor.capture());
        AuditLog saved = captor.getValue().getFirst();
        assertThat(saved.getAction()).isEqualTo(AuditAction.UPDATE);
        assertThat(saved.getEntityName()).isEqualTo("Item");
        assertThat(saved.getEntityId()).isEqualTo("42");
        assertThat(saved.getOldValues()).isEqualTo("{\"name\":null}");
        assertThat(saved.getNewValues()).isEqualTo("{\"name\":\"new\"}");
        assertThat(saved.getTransactionId()).isEqualTo(transactionId);
        assertThat(saved.getCreatedAt()).isEqualTo(created);
        assertThat(saved.getTraceId()).isEqualTo("trace");
        assertThat(saved.getRequestId()).isEqualTo("request");
        assertThat(saved.getClientActionId()).isEqualTo("action");
        assertThat(saved.getClientRequestId()).isEqualTo("client");
        assertThat(saved.getIpAddress()).isEqualTo("127.0.0.1");
        assertThat(saved.getActorId()).isEqualTo("user");
        verify(repository, never()).deleteAll();
    }
}
