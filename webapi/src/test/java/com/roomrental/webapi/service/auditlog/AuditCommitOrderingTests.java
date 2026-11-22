package com.roomrental.webapi.service.auditlog;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyList;
import static org.mockito.Mockito.doAnswer;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.spy;
import static org.mockito.Mockito.verify;

import java.util.Set;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.Executors;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;

import org.junit.jupiter.api.Test;
import org.springframework.transaction.TransactionDefinition;
import org.springframework.transaction.TransactionSystemException;
import org.springframework.transaction.support.AbstractPlatformTransactionManager;
import org.springframework.transaction.support.DefaultTransactionStatus;
import org.springframework.transaction.support.TransactionTemplate;

import com.roomrental.webapi.config.auditlog.AuditProperties;
import com.roomrental.webapi.enums.common.AuditAction;

class AuditCommitOrderingTests {
    private static final AuditProperties SETTINGS = new AuditProperties(true,
            new AuditProperties.Queue(5, 1, 0), new AuditProperties.Batch(false, 1, 10),
            new AuditProperties.Retry(1, 0, 1, 2), new AuditProperties.Shutdown(2), Set.of());

    @Test
    void add_ShouldEnqueueAndPersistOnlyAfterCommit_WhenWorkerIsRunning() throws Exception {
        // Arrange
        var queue = spy(new AuditQueue(SETTINGS));
        var buffer = new AuditTransactionBuffer(queue);
        var persistence = mock(AuditService.class);
        var commitStarted = new CountDownLatch(1);
        var allowCommit = new CountDownLatch(1);
        var persisted = new CountDownLatch(1);
        var commitFinished = new AtomicBoolean();
        var persistedAfterCommit = new AtomicBoolean();
        doAnswer(invocation -> {
            persistedAfterCommit.set(commitFinished.get());
            persisted.countDown();
            return null;
        }).when(persistence).save(anyList());
        var worker = new AuditWorkerManager(queue, SETTINGS, persistence);
        var manager = new BlockingTransactionManager(commitStarted, allowCommit, commitFinished);
        var template = new TransactionTemplate(manager);
        var executor = Executors.newSingleThreadExecutor();
        worker.start();

        // Act
        var transaction = executor.submit(() -> template.executeWithoutResult(status ->
                buffer.add(AuditAction.INSERT, "Item", "1", null, "{}")));
        try {
            assertThat(commitStarted.await(3, TimeUnit.SECONDS)).isTrue();
            verify(queue, never()).submit(any());
            verify(persistence, never()).save(anyList());
            allowCommit.countDown();
            transaction.get(3, TimeUnit.SECONDS);
            boolean workerFinished = persisted.await(3, TimeUnit.SECONDS);

            // Assert
            assertThat(workerFinished).isTrue();
            assertThat(persistedAfterCommit).isTrue();
            verify(queue).submit(any());
            verify(persistence).save(anyList());
        } finally {
            allowCommit.countDown();
            executor.shutdownNow();
            worker.stop();
        }
    }

    @Test
    void add_ShouldNotEnqueue_WhenTransactionRollsBack() {
        // Arrange
        var queue = spy(new AuditQueue(SETTINGS));
        var buffer = new AuditTransactionBuffer(queue);
        var manager = new BlockingTransactionManager(new CountDownLatch(1), new CountDownLatch(0), new AtomicBoolean());
        var template = new TransactionTemplate(manager);

        // Act
        template.executeWithoutResult(status -> {
            buffer.add(AuditAction.INSERT, "Item", "1", null, "{}");
            status.setRollbackOnly();
        });

        // Assert
        verify(queue, never()).submit(any());
        assertThat(queue.size()).isZero();
    }

    private static class BlockingTransactionManager extends AbstractPlatformTransactionManager {
        private final CountDownLatch commitStarted;
        private final CountDownLatch allowCommit;
        private final AtomicBoolean commitFinished;

        BlockingTransactionManager(CountDownLatch commitStarted, CountDownLatch allowCommit, AtomicBoolean commitFinished) {
            this.commitStarted = commitStarted;
            this.allowCommit = allowCommit;
            this.commitFinished = commitFinished;
        }

        @Override
        protected Object doGetTransaction() {
            return new Object();
        }

        @Override
        protected void doBegin(Object transaction, TransactionDefinition definition) {
        }

        @Override
        protected void doCommit(DefaultTransactionStatus status) {
            commitStarted.countDown();
            try {
                if (!allowCommit.await(3, TimeUnit.SECONDS)) {
                    throw new TransactionSystemException("Timed out waiting to commit test transaction");
                }
                commitFinished.set(true);
            } catch (InterruptedException e) {
                Thread.currentThread().interrupt();
                throw new TransactionSystemException("Interrupted while committing test transaction", e);
            }
        }

        @Override
        protected void doRollback(DefaultTransactionStatus status) {
        }
    }
}
