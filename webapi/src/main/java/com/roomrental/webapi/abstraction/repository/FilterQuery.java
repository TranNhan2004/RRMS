package com.roomrental.webapi.abstraction.repository;

import jakarta.persistence.criteria.CriteriaBuilder;
import jakarta.persistence.criteria.CriteriaQuery;
import jakarta.persistence.criteria.Root;
import jakarta.persistence.criteria.Selection;

/**
 * A reusable definition, not an executed query. Configure all joins that affect
 * row cardinality using explicit joins in configure() or select(). Both methods
 * are invoked independently for the result and count queries and must not
 * execute
 * SQL or depend on query-specific mutable state. Association projections must
 * use
 * explicit joins instead of implicit path navigation so the count sees the same
 * joins.
 * Fetch joins, grouped and distinct queries, and multiple roots are not
 * supported.
 */
public interface FilterQuery<TEntity, TModel> {
    Class<TEntity> entityType();

    void configure(CriteriaBuilder builder, CriteriaQuery<?> query, Root<TEntity> root);

    Selection<TModel> select(CriteriaBuilder builder, Root<TEntity> root, Class<TModel> modelType);
}
