package com.roomrental.webapi.repository;

import java.beans.IntrospectionException;
import java.beans.Introspector;
import java.lang.reflect.Modifier;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Locale;
import java.util.Objects;

import org.springframework.stereotype.Component;

import com.roomrental.webapi.abstraction.repository.FilterQuery;
import com.roomrental.webapi.exception.UnprocessableEntityException;
import com.roomrental.webapi.model.common.FilterRequestModel;
import com.roomrental.webapi.model.common.FilterResponseModel;
import com.roomrental.webapi.model.common.SortField;

import jakarta.persistence.EntityManager;
import jakarta.persistence.criteria.CriteriaBuilder;
import jakarta.persistence.criteria.CriteriaQuery;
import jakarta.persistence.criteria.Order;
import jakarta.persistence.criteria.Predicate;
import jakarta.persistence.criteria.Root;

/**
 * Executes a repository-defined query only after filtering and paging are
 * applied.
 * Call from a service-level read-only transaction so both queries share its
 * context.
 */
@Component
public class FilterHandler {
    private final EntityManager entityManager;

    public FilterHandler(EntityManager entityManager) {
        this.entityManager = entityManager;
    }

    public <E, M> FilterResponseModel<M> applyFilter(
            FilterRequestModel<M> request,
            FilterQuery<E, M> definition,
            Class<M> modelType) {
        Objects.requireNonNull(request, "request");
        Objects.requireNonNull(definition, "definition");
        Objects.requireNonNull(modelType, "modelType");
        validateFields(request, modelType);
        if (request.getCurrentPage() < 1 || request.getItemsPerPage() < 1) {
            throw new UnprocessableEntityException("Pagination values must be positive.");
        }
        long offset = (long) (request.getCurrentPage() - 1) * request.getItemsPerPage();
        if (offset > Integer.MAX_VALUE) {
            throw new UnprocessableEntityException("Page offset exceeds the supported range.");
        }

        CriteriaBuilder builder = entityManager.getCriteriaBuilder();
        CriteriaQuery<M> dataQuery = builder.createQuery(modelType);
        Root<E> dataRoot = dataQuery.from(definition.entityType());
        definition.configure(builder, dataQuery, dataRoot);
        rejectUnsupported(dataQuery);
        dataQuery.select(definition.select(builder, dataRoot, modelType));
        applyRestrictions(builder, dataQuery, dataRoot, request);
        applySort(builder, dataQuery, dataRoot, request);

        CriteriaQuery<Long> countQuery = builder.createQuery(Long.class);
        Root<E> countRoot = countQuery.from(definition.entityType());
        definition.configure(builder, countQuery, countRoot);
        rejectUnsupported(countQuery);
        // Rebuild projection joins as well, so both queries have the same row set.
        definition.select(builder, countRoot, modelType);
        applyRestrictions(builder, countQuery, countRoot, request);
        countQuery.select(builder.count(countRoot));
        countQuery.orderBy(List.of());

        long total = entityManager.createQuery(countQuery).getSingleResult();
        List<M> items = entityManager.createQuery(dataQuery)
                .setFirstResult((int) offset)
                .setMaxResults(request.getItemsPerPage())
                .getResultList();
        long pages = total / request.getItemsPerPage() + (total % request.getItemsPerPage() == 0 ? 0 : 1);
        int totalPages = (int) Math.min(pages, Integer.MAX_VALUE);
        return new FilterResponseModel<>(items, request.getCurrentPage(), request.getItemsPerPage(), total,
                totalPages, request.getCurrentPage() > 1, request.getCurrentPage() < pages);
    }

    private static void validateFields(FilterRequestModel<?> request, Class<?> modelType) {
        var properties = new HashSet<String>();
        for (Class<?> type = modelType; type != null && type != Object.class; type = type.getSuperclass()) {
            for (var field : type.getDeclaredFields()) {
                if (!Modifier.isStatic(field.getModifiers()) && !field.isSynthetic()) {
                    properties.add(field.getName().toLowerCase(Locale.ROOT));
                }
            }
        }
        try {
            for (var property : Introspector.getBeanInfo(modelType).getPropertyDescriptors()) {
                if (property.getReadMethod() != null)
                    properties.add(property.getName().toLowerCase(Locale.ROOT));
            }
        } catch (IntrospectionException e) {
            throw new IllegalArgumentException("Cannot inspect filter model type", e);
        }
        for (String name : request.getFilterFieldMap().names()) {
            if (!properties.contains(name)) {
                throw new IllegalArgumentException("Unknown response model field: " + name);
            }
        }
    }

    private static void rejectUnsupported(CriteriaQuery<?> query) {
        if (query.isDistinct() || !query.getGroupList().isEmpty() || query.getGroupRestriction() != null
                || query.getRoots().size() != 1) {
            throw new IllegalArgumentException(
                    "Grouped, distinct, and multiple-root filter queries are not supported.");
        }
        if (query.getRoots().stream().anyMatch(root -> !root.getFetches().isEmpty())) {
            throw new IllegalArgumentException("Fetch joins are not supported in filter queries.");
        }
    }

    private static void applyRestrictions(CriteriaBuilder builder, CriteriaQuery<?> query, Root<?> root,
            FilterRequestModel<?> request) {
        Predicate existing = query.getRestriction();
        Predicate filter = FilterPredicateBuilder.build(root, builder, request);
        query.where(existing == null ? filter : builder.and(existing, filter));
    }

    private static void applySort(CriteriaBuilder builder, CriteriaQuery<?> query, Root<?> root,
            FilterRequestModel<?> request) {
        if (request.getSortFields() == null || request.getSortFields().isEmpty())
            return;
        List<Order> orders = new ArrayList<>();
        for (SortField field : request.getSortFields()) {
            if (field == null)
                throw new UnprocessableEntityException("Sort field is required.");
            var path = FilterPredicateBuilder.path(root,
                    request.getFilterFieldMap().require(field.getFieldName()).getPath());
            orders.add(field.isAscending() ? builder.asc(path) : builder.desc(path));
        }
        query.orderBy(orders);
    }
}
