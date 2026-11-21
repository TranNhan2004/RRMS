package com.roomrental.webapi.model.common;

import java.util.Collection;
import java.util.LinkedHashMap;
import java.util.Locale;
import java.util.Map;

import com.roomrental.webapi.exception.UnprocessableEntityException;

import lombok.AllArgsConstructor;
import lombok.Getter;

/** Explicitly maps public field names to trusted JPA property paths. */
public final class FilterFieldMap {
    @Getter
    @AllArgsConstructor
    public static class Field {
        private final String path;
        private final Class<?> type;
    }

    private final Map<String, Field> fields = new LinkedHashMap<>();

    public FilterFieldMap map(String name, String path, Class<?> type) {
        if (name == null || name.isBlank() || path == null || path.isBlank() || type == null)
            throw new IllegalArgumentException("Field name, path and type are required.");
        fields.put(name.trim().toLowerCase(Locale.ROOT), new Field(path, type));
        return this;
    }

    public Field require(String name) {
        Field field = name == null ? null : fields.get(name.trim().toLowerCase(Locale.ROOT));
        if (field == null)
            throw new UnprocessableEntityException("Unknown filter or sort field.");
        return field;
    }

    public Collection<Field> fields() {
        return java.util.List.copyOf(fields.values());
    }

    public java.util.Set<String> names() {
        return java.util.Set.copyOf(fields.keySet());
    }

    /** Takes a defensive copy of a server-defined allowlist. */
    public FilterFieldMap copy() {
        var result = new FilterFieldMap();
        result.fields.putAll(fields);
        return result;
    }
}
