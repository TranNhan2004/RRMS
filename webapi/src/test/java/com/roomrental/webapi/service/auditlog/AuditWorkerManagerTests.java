package com.roomrental.webapi.service.auditlog;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.anyList;
import static org.mockito.Mockito.doThrow;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.timeout;
import static org.mockito.Mockito.times;
import static org.mockito.Mockito.verify;

import java.time.Instant;
import java.util.Set;
import java.util.concurrent.TimeUnit;

import org.junit.jupiter.api.Test;
import org.springframework.dao.TransientDataAccessResourceException;

import com.roomrental.webapi.config.auditlog.AuditProperties;
import com.roomrental.webapi.enums.common.AuditAction;
import com.roomrental.webapi.utils.UUIDUtils;

class AuditWorkerManagerTests {
    private final AuditProperties settings = new AuditProperties(true, new AuditProperties.Queue(5, 1, 0),
            new AuditProperties.Batch(true, 5, 20), new AuditProperties.Retry(3, 1, 5, 2),
            new AuditProperties.Shutdown(2), Set.of());

    private AuditSnapshot snapshot() {
        return new AuditSnapshot(AuditAction.INSERT, "Item", "1", null, "{}",
                new AuditContext(null, null, null, null, null, "system"), UUIDUtils.generateV7(), Instant.now());
    }

    @Test
    void worker_ShouldRetryTransientFailure_WhenPersistingBatch() throws Exception {
        // Arrange
        var queue = new AuditQueue(settings);
        var persistence = mock(AuditService.class);
        doThrow(new TransientDataAccessResourceException("unavailable")).doNothing().when(persistence).save(anyList());
        var manager = new AuditWorkerManager(queue, settings, persistence);
        queue.submit(snapshot());

        // Act
        manager.start();

        // Assert
        try {
            verify(persistence, timeout(2000).times(2)).save(anyList());
        } finally {
            manager.stop();
        }
        assertThat(manager.persistedCount()).isEqualTo(1);
        assertThat(manager.failedCount()).isZero();
    }

    @Test
    void stop_ShouldDrainQueue_WhenRecordsArePending() {
        // Arrange
        var queue = new AuditQueue(settings);
        var persistence = mock(AuditService.class);
        var manager = new AuditWorkerManager(queue, settings, persistence);
        queue.submit(snapshot());
        queue.submit(snapshot());

        // Act
        manager.start();
        manager.stop();

        // Assert
        assertThat(manager.persistedCount()).isEqualTo(2);
        assertThat(queue.size()).isZero();
    }

    @Test
    void worker_ShouldCountFailure_WhenRetryIsExhausted() throws Exception {
        // Arrange
        var queue = new AuditQueue(settings);
        var persistence = mock(AuditService.class);
        doThrow(new TransientDataAccessResourceException("unavailable")).when(persistence).save(anyList());
        var manager = new AuditWorkerManager(queue, settings, persistence);
        queue.submit(snapshot());

        // Act
        manager.start();
        try {
            long deadline = System.nanoTime() + TimeUnit.SECONDS.toNanos(2);
            while (manager.failedCount() == 0 && System.nanoTime() < deadline)
                Thread.sleep(5);
        } finally {
            manager.stop();
        }

        // Assert
        assertThat(manager.failedCount()).isEqualTo(1);
        verify(persistence, times(3)).save(anyList());
    }
}
