package com.roomrental.webapi.service.auditlog;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.Mockito.doReturn;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.when;

import java.util.Set;

import org.hibernate.event.spi.PostInsertEvent;
import org.hibernate.event.spi.PostUpdateEvent;
import org.hibernate.persister.entity.EntityPersister;
import org.hibernate.type.Type;
import org.junit.jupiter.api.Test;
import org.springframework.transaction.support.TransactionSynchronization;
import org.springframework.transaction.support.TransactionSynchronizationManager;

import com.roomrental.webapi.annotation.Auditable;
import com.roomrental.webapi.config.auditlog.AuditProperties;

class AuditEventListenerTests {
    @Auditable(excludeFields = "secret")
    static class Item {
    }

    private final AuditProperties settings = new AuditProperties(true, new AuditProperties.Queue(5, 1, 0),
            new AuditProperties.Batch(true, 2, 10), new AuditProperties.Retry(2, 0, 1, 2),
            new AuditProperties.Shutdown(1), Set.of("createdAt"));

    @Test
    void onPostUpdate_ShouldCaptureOnlyChangedNonExcludedValues_WhenEntityIsAudited() throws Exception {
        // Arrange
        var persister = mock(EntityPersister.class);
        doReturn(Item.class).when(persister).getMappedClass();
        when(persister.getEntityName()).thenReturn("Item");
        when(persister.getPropertyNames()).thenReturn(new String[] { "name", "secret" });
        when(persister.getPropertyTypes()).thenReturn(new Type[] { mock(Type.class), mock(Type.class) });
        var queue = new AuditQueue(settings);
        var listener = new AuditEventListener(new AuditMetadataCache(settings), new AuditTransactionBuffer(queue));
        TransactionSynchronizationManager.setActualTransactionActive(true);
        TransactionSynchronizationManager.initSynchronization();

        try {
            var event = new PostUpdateEvent(new Item(), "1", new Object[] { null, "new secret" },
                    new Object[] { "old", "old secret" }, new int[] { 0, 1 }, persister, null);

            // Act
            listener.onPostUpdate(event);
            TransactionSynchronizationManager.getSynchronizations().getFirst()
                    .afterCompletion(TransactionSynchronization.STATUS_COMMITTED);
            AuditSnapshot snapshot = queue.poll(1);

            // Assert
            assertThat(snapshot.oldValues()).isEqualTo("{\"name\":\"old\"}");
            assertThat(snapshot.newValues()).isEqualTo("{\"name\":null}");
        } finally {
            TransactionSynchronizationManager.clearSynchronization();
            TransactionSynchronizationManager.setActualTransactionActive(false);
        }
    }

    @Test
    void onPostInsert_ShouldIgnoreEntity_WhenAnnotationIsAbsent() {
        // Arrange
        var persister = mock(EntityPersister.class);
        doReturn(String.class).when(persister).getMappedClass();
        var queue = new AuditQueue(settings);
        var listener = new AuditEventListener(new AuditMetadataCache(settings), new AuditTransactionBuffer(queue));

        // Act
        listener.onPostInsert(new PostInsertEvent("value", "1", new Object[0], persister, null));

        // Assert
        assertThat(queue.size()).isZero();
    }
}
