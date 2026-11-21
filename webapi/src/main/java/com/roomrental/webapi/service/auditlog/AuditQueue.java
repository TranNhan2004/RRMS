package com.roomrental.webapi.service.auditlog;

import java.util.concurrent.ArrayBlockingQueue;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.LongAdder;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

import com.roomrental.webapi.config.auditlog.AuditProperties;

@Component
public class AuditQueue {
    private static final Logger log = LoggerFactory.getLogger(AuditQueue.class);
    private final ArrayBlockingQueue<AuditSnapshot> queue;
    private final long timeout;
    private final LongAdder rejected = new LongAdder();

    public AuditQueue(AuditProperties properties) {
        queue = new ArrayBlockingQueue<>(properties.queue().capacity());
        timeout = properties.queue().enqueueTimeoutMs();
    }

    public void submit(AuditSnapshot snapshot) {
        try {
            if (queue.offer(snapshot, timeout, TimeUnit.MILLISECONDS))
                return;
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
        }
        rejected.increment();
        log.error("Audit queue rejected record transactionId={} traceId={} requestId={} queueSize={}",
                snapshot.transactionId(), snapshot.context().traceId(), snapshot.context().requestId(), queue.size());
    }

    AuditSnapshot poll(long millis) throws InterruptedException {
        return queue.poll(millis, TimeUnit.MILLISECONDS);
    }

    int size() {
        return queue.size();
    }

    public double utilization() {
        return (double) queue.size() / (queue.size() + queue.remainingCapacity());
    }

    public long rejectedCount() {
        return rejected.sum();
    }
}
