package com.roomrental.webapi.service.auditlog;

import java.util.Arrays;
import java.util.HashSet;
import java.util.Set;
import java.util.concurrent.ConcurrentHashMap;

import org.hibernate.persister.entity.EntityPersister;
import org.springframework.stereotype.Component;

import com.roomrental.webapi.annotation.Auditable;
import com.roomrental.webapi.config.auditlog.AuditProperties;

@Component
public class AuditMetadataCache {
    public record AuditMetadata(boolean enabled, Set<String> excludedFields) {
    }

    private final ConcurrentHashMap<Class<?>, AuditMetadata> cache = new ConcurrentHashMap<>();
    private final AuditProperties properties;

    public AuditMetadataCache(AuditProperties properties) {
        this.properties = properties;
    }

    public AuditMetadata get(EntityPersister persister) {
        return cache.computeIfAbsent(persister.getMappedClass(), type -> {
            var annotation = type.getAnnotation(Auditable.class);
            if (annotation == null)
                return new AuditMetadata(false, Set.of());
            Set<String> names = new HashSet<>(Arrays.asList(persister.getPropertyNames()));
            names.add(persister.getIdentifierPropertyName());
            for (String excluded : annotation.excludeFields()) {
                if (!names.contains(excluded))
                    throw new IllegalArgumentException("Unknown audited property " + type.getName() + "." + excluded);
            }
            Set<String> excluded = new HashSet<>(properties.excludedFields());
            excluded.addAll(Arrays.asList(annotation.excludeFields()));
            return new AuditMetadata(true, Set.copyOf(excluded));
        });
    }
}
