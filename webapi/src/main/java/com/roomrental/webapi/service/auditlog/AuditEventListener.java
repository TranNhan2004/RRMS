package com.roomrental.webapi.service.auditlog;

import java.math.BigDecimal;
import java.math.BigInteger;
import java.time.temporal.TemporalAccessor;
import java.util.Base64;
import java.util.Date;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Set;
import java.util.UUID;

import org.hibernate.bytecode.enhance.spi.LazyPropertyInitializer;
import org.hibernate.event.spi.PostDeleteEvent;
import org.hibernate.event.spi.PostDeleteEventListener;
import org.hibernate.event.spi.PostInsertEvent;
import org.hibernate.event.spi.PostInsertEventListener;
import org.hibernate.event.spi.PostUpdateEvent;
import org.hibernate.event.spi.PostUpdateEventListener;
import org.hibernate.persister.entity.EntityPersister;
import org.hibernate.type.BasicType;
import org.hibernate.type.Type;
import org.hibernate.type.descriptor.converter.spi.BasicValueConverter;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

import com.roomrental.webapi.enums.common.AuditAction;
import com.roomrental.webapi.utils.EnumUtils;

import tools.jackson.databind.json.JsonMapper;

/**
 * Captures basic values only. Associations, collections, embedded values and
 * unknown object types are omitted.
 */
@Component
public class AuditEventListener implements PostInsertEventListener, PostUpdateEventListener, PostDeleteEventListener {
    private static final Logger log = LoggerFactory.getLogger(AuditEventListener.class);
    private static final Object OMIT = new Object();
    private final AuditMetadataCache metadata;
    private final AuditTransactionBuffer buffer;
    private final JsonMapper json = JsonMapper.builder().build();

    public AuditEventListener(AuditMetadataCache metadata, AuditTransactionBuffer buffer) {
        this.metadata = metadata;
        this.buffer = buffer;
    }

    @Override
    public void onPostInsert(PostInsertEvent event) {
        capture(event.getPersister(), event.getId(), AuditAction.INSERT, null, event.getState(), null);
    }

    @Override
    public void onPostDelete(PostDeleteEvent event) {
        capture(event.getPersister(), event.getId(), AuditAction.DELETE, event.getDeletedState(), null, null);
    }

    @Override
    public void onPostUpdate(PostUpdateEvent event) {
        capture(event.getPersister(),
                event.getId(),
                AuditAction.UPDATE,
                event.getOldState(),
                event.getState(),
                event.getDirtyProperties());
    }

    @Override
    public boolean requiresPostCommitHandling(EntityPersister persister) {
        return false;
    }

    private void capture(EntityPersister persister, Object id, AuditAction action, Object[] oldState, Object[] newState,
            int[] dirty) {
        AuditMetadataCache.AuditMetadata info = metadata.get(persister);
        if (!info.enabled())
            return;
        if (id == null) {
            log.error("Audit event has no identifier: entity={}", persister.getEntityName());
            return;
        }
        if (action == AuditAction.UPDATE && dirty == null) {
            log.warn("Audit update skipped: dirty properties unavailable for entity={}", persister.getEntityName());
            return;
        }
        Map<String, Object> oldValues = oldState == null ? null
                : values(persister, oldState, info.excludedFields(), dirty);
        Map<String, Object> newValues = newState == null ? null
                : values(persister, newState, info.excludedFields(), dirty);
        if (action == AuditAction.UPDATE && (newValues == null || newValues.isEmpty()))
            return;
        try {
            buffer.add(action, persister.getEntityName(), String.valueOf(id),
                    oldValues == null ? null : json.writeValueAsString(oldValues),
                    newValues == null ? null : json.writeValueAsString(newValues));
        } catch (RuntimeException e) {
            log.error("Could not serialize audit snapshot: entity={} exceptionType={}", persister.getEntityName(),
                    e.getClass().getName());
        }
    }

    private Map<String, Object> values(EntityPersister persister, Object[] state, Set<String> excluded, int[] dirty) {
        Map<String, Object> result = new LinkedHashMap<>();
        String[] names = persister.getPropertyNames();
        Type[] types = persister.getPropertyTypes();
        for (int i = 0; i < names.length; i++) {
            if (excluded.contains(names[i]) || (dirty != null && !contains(dirty, i)) || i >= state.length
                    || types[i].isAssociationType() || types[i].isCollectionType() || types[i].isComponentType())
                continue;
            Object value = scalar(state[i]);
            if (value == OMIT && state[i] != LazyPropertyInitializer.UNFETCHED_PROPERTY
                    && types[i] instanceof BasicType<?> basic) {
                value = convertedValue(basic, state[i]);
            }
            if (value != OMIT)
                result.put(names[i], value);
        }
        return result;
    }

    private static boolean contains(int[] values, int value) {
        for (int item : values)
            if (item == value)
                return true;
        return false;
    }

    private static Object scalar(Object value) {
        if (value == LazyPropertyInitializer.UNFETCHED_PROPERTY)
            return OMIT;
        if (value == null || value instanceof String || value instanceof Boolean || value instanceof Byte
                || value instanceof Short || value instanceof Integer || value instanceof Long
                || value instanceof Float || value instanceof Double || value instanceof BigInteger
                || value instanceof BigDecimal)
            return value;
        if (value instanceof byte[] bytes)
            return Base64.getEncoder().encodeToString(bytes);
        if (value instanceof Enum<?> e)
            return EnumUtils.toString(e);
        if (value instanceof Character || value instanceof UUID || value instanceof TemporalAccessor)
            return value.toString();
        if (value instanceof Date date)
            return java.time.Instant.ofEpochMilli(date.getTime()).toString();
        return OMIT;
    }

    @SuppressWarnings({ "rawtypes", "unchecked" })
    private static Object convertedValue(BasicType<?> type, Object value) {
        BasicValueConverter converter = type.getValueConverter();
        if (converter == null)
            return OMIT;
        try {
            return scalar(converter.toRelationalValue(value));
        } catch (RuntimeException e) {
            log.warn("Could not convert audit property: exceptionType={}", e.getClass().getName());
            return OMIT;
        }
    }
}
