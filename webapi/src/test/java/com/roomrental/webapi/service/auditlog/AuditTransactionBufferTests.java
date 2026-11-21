package com.roomrental.webapi.service.auditlog;

import static org.assertj.core.api.Assertions.assertThat;

import java.util.List;

import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Test;
import org.springframework.transaction.support.TransactionSynchronization;
import org.springframework.transaction.support.TransactionSynchronizationManager;

import com.roomrental.webapi.config.auditlog.AuditProperties;
import com.roomrental.webapi.enums.common.AuditAction;
import com.roomrental.webapi.utils.UUIDUtils;

class AuditTransactionBufferTests {
    private static AuditProperties settings(int capacity) {
        return new AuditProperties(true, new AuditProperties.Queue(capacity, 1, 0),
                new AuditProperties.Batch(true, 2, 10), new AuditProperties.Retry(2, 0, 1, 2),
                new AuditProperties.Shutdown(1), java.util.Set.of());
    }

    @AfterEach
    void cleanup() {
        if (TransactionSynchronizationManager.isSynchronizationActive())
            TransactionSynchronizationManager.clearSynchronization();
        TransactionSynchronizationManager.setActualTransactionActive(false);
    }

    @Test
    void add_ShouldDispatchOnlyAfterCommit_WhenTransactionSucceeds() throws Exception {
        // Arrange
        var queue = new AuditQueue(settings(5));
        var buffer = new AuditTransactionBuffer(queue);
        TransactionSynchronizationManager.setActualTransactionActive(true);
        TransactionSynchronizationManager.initSynchronization();

        // Act
        buffer.add(AuditAction.INSERT, "Item", "1", null, "{}");
        buffer.add(AuditAction.UPDATE, "Item", "1", "{}", "{}");
        int sizeBeforeCommit = queue.size();
        List<TransactionSynchronization> callbacks = TransactionSynchronizationManager.getSynchronizations();
        callbacks.getFirst().afterCompletion(TransactionSynchronization.STATUS_COMMITTED);
        AuditSnapshot first = queue.poll(1);
        AuditSnapshot second = queue.poll(1);

        // Assert
        assertThat(sizeBeforeCommit).isZero();
        assertThat(callbacks).hasSize(1);
        assertThat(first).isNotNull();
        assertThat(second).isNotNull();
        assertThat(first.transactionId()).isEqualTo(second.transactionId());
        assertThat(first.transactionId().version()).isEqualTo(7);
        assertThat(first.context().actorId()).isEqualTo("system");
    }

    @Test
    void add_ShouldDiscardRecords_WhenTransactionRollsBack() {
        // Arrange
        var queue = new AuditQueue(settings(5));
        var buffer = new AuditTransactionBuffer(queue);
        TransactionSynchronizationManager.setActualTransactionActive(true);
        TransactionSynchronizationManager.initSynchronization();

        // Act
        buffer.add(AuditAction.DELETE, "Item", "1", "{}", null);
        TransactionSynchronizationManager.getSynchronizations().getFirst()
                .afterCompletion(TransactionSynchronization.STATUS_ROLLED_BACK);

        // Assert
        assertThat(queue.size()).isZero();
    }

    @Test
    void submit_ShouldCountRejectedRecords_WhenQueueIsFull() {
        // Arrange
        var queue = new AuditQueue(settings(1));
        var context = new AuditContext(null, null, null, null, null, "system");
        var snapshot = new AuditSnapshot(AuditAction.INSERT, "Item", "1", null, "{}", context, UUIDUtils.generateV7(),
                java.time.Instant.now());

        // Act
        queue.submit(snapshot);
        queue.submit(snapshot);

        // Assert
        assertThat(queue.size()).isEqualTo(1);
        assertThat(queue.rejectedCount()).isEqualTo(1);
    }

    @Test
    void add_ShouldDiscardNestedRecords_WhenSavepointRollsBack() throws Exception {
        // Arrange
        var queue = new AuditQueue(settings(5));
        var buffer = new AuditTransactionBuffer(queue);
        TransactionSynchronizationManager.setActualTransactionActive(true);
        TransactionSynchronizationManager.initSynchronization();

        // Act
        buffer.add(AuditAction.INSERT, "Item", "1", null, "{}");
        TransactionSynchronization callback = TransactionSynchronizationManager.getSynchronizations().getFirst();
        var savepoint = new Object();
        callback.savepoint(savepoint);
        buffer.add(AuditAction.INSERT, "Item", "2", null, "{}");
        callback.savepointRollback(savepoint);
        callback.afterCompletion(TransactionSynchronization.STATUS_COMMITTED);
        AuditSnapshot result = queue.poll(1);

        // Assert
        assertThat(result.entityId()).isEqualTo("1");
        assertThat(queue.size()).isZero();
    }
}
