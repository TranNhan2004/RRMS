package com.roomrental.webapi.service.auditlog;

import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.LongAdder;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.context.SmartLifecycle;
import org.springframework.dao.TransientDataAccessException;
import org.springframework.stereotype.Component;

import com.roomrental.webapi.config.auditlog.AuditProperties;
import com.roomrental.webapi.utils.TimestampUtils;

@Component
public class AuditWorkerManager implements SmartLifecycle {
    private static final Logger log = LoggerFactory.getLogger(AuditWorkerManager.class);
    private final AuditQueue queue;
    private final AuditProperties properties;
    private final AuditService persistence;
    private final LongAdder persisted = new LongAdder(), failed = new LongAdder();
    private volatile boolean running;
    private ExecutorService executor;

    public AuditWorkerManager(AuditQueue queue, AuditProperties properties, AuditService persistence) {
        this.queue = queue;
        this.properties = properties;
        this.persistence = persistence;
    }

    @Override
    public synchronized void start() {
        if (running || !properties.enabled())
            return;
        running = true;
        executor = Executors.newFixedThreadPool(properties.queue().workerCount(), r -> {
            var thread = new Thread(r, "audit-worker");
            thread.setDaemon(true);
            return thread;
        });
        for (int i = 0; i < properties.queue().workerCount(); i++)
            executor.submit(this::work);
    }

    private void work() {
        List<AuditSnapshot> batch = new ArrayList<>();
        int batchSize = properties.batch().enabled() ? properties.batch().size() : 1;
        try {
            while (running || queue.size() > 0) {
                AuditSnapshot first = queue.poll(Math.min(properties.batch().flushIntervalMs(), 250));
                if (first == null)
                    continue;
                batch.add(first);
                long deadline = System.nanoTime() + TimeUnit.MILLISECONDS.toNanos(properties.batch().flushIntervalMs());
                while (batch.size() < batchSize && running) {
                    long remaining = TimeUnit.NANOSECONDS.toMillis(deadline - System.nanoTime());
                    if (remaining <= 0)
                        break;
                    AuditSnapshot next = queue.poll(remaining);
                    if (next == null)
                        break;
                    batch.add(next);
                }
                persist(batch);
                batch.clear();
            }
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
            if (!batch.isEmpty()) {
                failed.add(batch.size());
                log.error("Audit worker interrupted with {} pending records", batch.size());
            }
        } catch (RuntimeException e) {
            failed.add(batch.size());
            log.error("Audit worker failed; pending records={} exceptionType={}", batch.size(), e.getClass().getName());
        }
    }

    private void persist(List<AuditSnapshot> batch) throws InterruptedException {
        long delay = properties.retry().initialDelayMs();
        long start = System.nanoTime();
        for (int attempt = 1; attempt <= properties.retry().maxAttempts(); attempt++) {
            try {
                persistence.save(List.copyOf(batch));
                persisted.add(batch.size());
                log.debug("Persisted {} audit records in {} ms; queueSize={}", batch.size(),
                        TimeUnit.NANOSECONDS.toMillis(System.nanoTime() - start), queue.size());
                return;
            } catch (RuntimeException e) {
                if (!(e instanceof TransientDataAccessException) && batch.size() > 1) {
                    int midpoint = batch.size() / 2;
                    persist(batch.subList(0, midpoint));
                    persist(batch.subList(midpoint, batch.size()));
                    return;
                }
                if (!(e instanceof TransientDataAccessException) || attempt == properties.retry().maxAttempts()) {
                    failed.add(batch.size());
                    AuditSnapshot first = batch.getFirst();
                    log.error("Audit batch failed: count={} transactionId={} traceId={} requestId={} exceptionType={}",
                            batch.size(), first.transactionId(), first.context().traceId(), first.context().requestId(),
                            e.getClass().getName());
                    return;
                }
                log.warn("Transient audit persistence failure: attempt={} batchSize={}", attempt, batch.size());
                Thread.sleep(delay);
                delay = TimestampUtils.calculateExponentialAttempt(delay, properties.retry().maxDelayMs(),
                        properties.retry().multiplier());
            }
        }
    }

    @Override
    public synchronized void stop() {
        running = false;
        if (executor == null)
            return;
        executor.shutdown();
        try {
            if (!executor.awaitTermination(properties.shutdown().timeoutSeconds(), TimeUnit.SECONDS)) {
                executor.shutdownNow();
                log.error("Audit shutdown timed out; queued records={}", queue.size());
            }
        } catch (InterruptedException e) {
            executor.shutdownNow();
            Thread.currentThread().interrupt();
        }
    }

    @Override
    public boolean isRunning() {
        return running;
    }

    public long persistedCount() {
        return persisted.sum();
    }

    public long failedCount() {
        return failed.sum();
    }
}
