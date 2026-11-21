package com.roomrental.webapi.service.auditlog;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.catchThrowable;
import static org.mockito.Mockito.doReturn;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.times;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

import java.util.List;
import java.util.Set;
import java.util.stream.IntStream;

import org.hibernate.persister.entity.EntityPersister;
import org.junit.jupiter.api.Test;

import com.roomrental.webapi.annotation.Auditable;
import com.roomrental.webapi.config.auditlog.AuditProperties;

class AuditMetadataCacheTests {
    @Auditable(excludeFields = "secret")
    static class Item {
    }

    @Auditable(excludeFields = "missing")
    static class Invalid {
    }

    private final AuditProperties settings = new AuditProperties(true, new AuditProperties.Queue(5, 1, 0),
            new AuditProperties.Batch(true, 2, 10), new AuditProperties.Retry(2, 0, 1, 2),
            new AuditProperties.Shutdown(1), Set.of("createdAt"));

    @Test
    void get_ShouldCacheMergedExclusions_WhenCalledConcurrently() {
        // Arrange
        var persister = mock(EntityPersister.class);
        doReturn(Item.class).when(persister).getMappedClass();
        when(persister.getPropertyNames()).thenReturn(new String[] { "name", "secret" });
        var cache = new AuditMetadataCache(settings);

        // Act
        List<AuditMetadataCache.AuditMetadata> results = IntStream.range(0, 100).parallel()
                .mapToObj(i -> cache.get(persister)).toList();

        // Assert
        assertThat(results).allSatisfy(result -> {
            assertThat(result.enabled()).isTrue();
            assertThat(result.excludedFields()).containsExactlyInAnyOrder("createdAt", "secret");
        });
        verify(persister, times(1)).getPropertyNames();
    }

    @Test
    void get_ShouldRejectUnknownExcludedProperty_WhenEntityIsAudited() {
        // Arrange
        var persister = mock(EntityPersister.class);
        doReturn(Invalid.class).when(persister).getMappedClass();
        when(persister.getPropertyNames()).thenReturn(new String[] { "name" });
        var cache = new AuditMetadataCache(settings);

        // Act
        Throwable error = catchThrowable(() -> cache.get(persister));

        // Assert
        assertThat(error).isInstanceOf(IllegalArgumentException.class);
    }
}
