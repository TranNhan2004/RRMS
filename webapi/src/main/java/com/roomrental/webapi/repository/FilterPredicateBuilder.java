package com.roomrental.webapi.repository;

import java.math.BigDecimal;
import java.time.DateTimeException;
import java.time.Instant;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.time.OffsetDateTime;
import java.util.ArrayList;
import java.util.EnumSet;
import java.util.List;
import java.util.Objects;
import java.util.UUID;

import com.roomrental.webapi.enums.common.FilterOperator;
import com.roomrental.webapi.enums.common.TemporalPartType;
import com.roomrental.webapi.exception.UnprocessableEntityException;
import com.roomrental.webapi.model.common.FilterCriterion;
import com.roomrental.webapi.model.common.FilterFieldMap;
import com.roomrental.webapi.model.common.FilterGroup;
import com.roomrental.webapi.model.common.FilterRequestModel;
import com.roomrental.webapi.utils.EnumUtils;

import jakarta.persistence.criteria.CriteriaBuilder;
import jakarta.persistence.criteria.Expression;
import jakarta.persistence.criteria.From;
import jakarta.persistence.criteria.Join;
import jakarta.persistence.criteria.JoinType;
import jakarta.persistence.criteria.Path;
import jakarta.persistence.criteria.Predicate;
import jakarta.persistence.criteria.Root;
import jakarta.persistence.metamodel.Attribute;

final class FilterPredicateBuilder {
    private FilterPredicateBuilder() {
    }

    static Predicate build(Root<?> root, CriteriaBuilder cb, FilterRequestModel<?> request) {
        List<Predicate> parts = new ArrayList<>();
        addCriteriaPredicate(parts, root, cb, request, request.getFilterFieldMap());
        addGlobalSearchPredicate(parts, root, cb, request.getSearchGlobalText(), request.getFilterFieldMap());
        return cb.and(parts.toArray(Predicate[]::new));
    }

    private static void addCriteriaPredicate(
            List<Predicate> parts, Root<?> root, CriteriaBuilder cb,
            FilterRequestModel<?> request, FilterFieldMap fields) {
        List<FilterCriterion> criteria = request.getFilterCriteria() == null
                ? List.of()
                : request.getFilterCriteria();
        List<Predicate> predicates = criteria.stream()
                .map(criterion -> criterion(root, cb, criterion, fields))
                .toList();
        if (predicates.isEmpty()) {
            return;
        }

        FilterGroup group = request.getFilterGroup();
        if (group == null || group.getChildren().isEmpty()) {
            parts.add(cb.and(predicates.toArray(Predicate[]::new)));
        } else {
            parts.add(group(cb, group, predicates));
        }
    }

    private static void addGlobalSearchPredicate(
            List<Predicate> parts, Root<?> root, CriteriaBuilder cb,
            String searchText, FilterFieldMap fields) {
        if (searchText == null || searchText.isBlank()) {
            return;
        }

        var pattern = "%" + escape(searchText.trim()) + "%";
        List<Predicate> matches = fields.fields().stream()
                .filter(field -> field.getType() == String.class)
                .map(field -> {
                    Expression<String> fieldPath = path(root, field.getPath());
                    return cb.and(cb.isNotNull(fieldPath), cb.like(fieldPath, pattern, '\\'));
                })
                .toList();
        parts.add(cb.or(matches.toArray(Predicate[]::new)));
    }

    private static Predicate group(CriteriaBuilder cb, FilterGroup group, List<Predicate> predicates) {
        if (group.getLogic() == null || group.getChildren() == null || group.getChildren().isEmpty()) {
            throw invalid("Filter group must contain children and logic.");
        }

        List<Predicate> children = group.getChildren().stream()
                .map(node -> groupNode(cb, node, predicates))
                .toList();
        return switch (group.getLogic()) {
            case AND -> cb.and(children.toArray(Predicate[]::new));
            case OR -> cb.or(children.toArray(Predicate[]::new));
            case NOT -> {
                if (children.size() != 1) {
                    throw invalid("NOT filter groups must contain exactly one child.");
                }
                yield cb.not(children.getFirst());
            }
        };
    }

    private static Predicate groupNode(CriteriaBuilder cb, FilterGroup.Node node, List<Predicate> predicates) {
        if (node == null || (node.getGroup() == null) == (node.getCriterionIndex() == null)) {
            throw invalid("Invalid filter group node.");
        }
        if (node.getGroup() != null) {
            return group(cb, node.getGroup(), predicates);
        }
        int index = node.getCriterionIndex();
        if (index < 0 || index >= predicates.size()) {
            throw invalid("Invalid criterion index.");
        }
        return predicates.get(index);
    }

    private static Predicate criterion(
            Root<?> root, CriteriaBuilder cb, FilterCriterion criterion, FilterFieldMap fields) {
        if (criterion == null || criterion.getOperator() == null) {
            throw invalid("Invalid filter criterion.");
        }

        FilterFieldMap.Field field = fields.require(criterion.getFieldName());
        Expression<?> fieldPath = path(root, field.getPath());
        List<String> values = normalizedValues(criterion);
        validateValues(criterion.getOperator(), field.getType(), values);

        if (criterion.getOperator() == FilterOperator.IS_NULL) {
            return cb.isNull(fieldPath);
        }

        ComparisonField comparison = comparisonField(cb, fieldPath, field.getType(), criterion);
        List<Predicate> choices = values.stream()
                .map(value -> valuePredicate(cb, comparison, criterion.getOperator(), value))
                .toList();
        return cb.and(cb.isNotNull(fieldPath), cb.or(choices.toArray(Predicate[]::new)));
    }

    private static List<String> normalizedValues(FilterCriterion criterion) {
        if (criterion.getValues() == null) {
            return List.of();
        }
        return criterion.getValues().stream()
                .filter(Objects::nonNull)
                .map(value -> Objects.requireNonNull(value).trim())
                .filter(value -> !value.isEmpty())
                .toList();
    }

    private static void validateValues(FilterOperator operator, Class<?> type, List<String> values) {
        if (operator == FilterOperator.IS_NULL) {
            if (!values.isEmpty()) {
                throw invalid("IS_NULL does not accept values.");
            }
            return;
        }
        if (values.isEmpty()) {
            throw invalid("Filter field must contain at least one value.");
        }

        boolean isComparison = EnumSet.of(
                FilterOperator.GREATER_THAN, FilterOperator.GREATER_THAN_OR_EQUALS,
                FilterOperator.LESS_THAN, FilterOperator.LESS_THAN_OR_EQUALS).contains(operator);
        if (isComparison && values.size() != 1) {
            throw invalid("Comparison operators require exactly one value.");
        }
        if ((type == Boolean.class || type == boolean.class) && operator != FilterOperator.EQUALS) {
            throw invalid("Boolean fields only support equality.");
        }
    }

    private static ComparisonField comparisonField(
            CriteriaBuilder cb, Expression<?> expression, Class<?> type, FilterCriterion criterion) {
        if (criterion.getDateTimeFilterOptions() == null) {
            return new ComparisonField(expression, type);
        }

        TemporalPartType part = criterion.getDateTimeFilterOptions().getTemporalPartType();
        if (part == null) {
            throw invalid("Temporal part is required.");
        }
        if (part == TemporalPartType.NONE) {
            return new ComparisonField(expression, type);
        }
        if (type != LocalDate.class && type != LocalDateTime.class
                && type != OffsetDateTime.class && type != Instant.class) {
            throw invalid("Temporal filters are not supported for this type.");
        }
        if (part == TemporalPartType.DATE_ONLY) {
            Expression<?> date = type == LocalDate.class
                    ? expression
                    : cb.function("date", LocalDate.class, expression);
            return new ComparisonField(date, LocalDate.class);
        }

        String function = switch (part) {
            case DATE -> "day";
            case MONTH -> "month";
            case YEAR -> "year";
            default -> throw invalid("Invalid temporal part.");
        };
        return new ComparisonField(cb.function(function, Integer.class, expression), Integer.class);
    }

    private static Predicate valuePredicate(
            CriteriaBuilder cb, ComparisonField field, FilterOperator operator, String value) {
        Object parsed = parse(value, field.type);
        return switch (operator) {
            case EQUALS -> cb.equal(field.expression, parsed);
            case CONTAINS, STARTS_WITH, ENDS_WITH -> stringPredicate(cb, field, operator, value);
            default -> compare(cb, field.expression, parsed, operator);
        };
    }

    private static Predicate stringPredicate(
            CriteriaBuilder cb, ComparisonField field, FilterOperator operator, String value) {
        if (field.type != String.class) {
            throw invalid("String operator requires a string field.");
        }
        String pattern = escape(value);
        if (operator != FilterOperator.STARTS_WITH) {
            pattern = "%" + pattern;
        }
        if (operator != FilterOperator.ENDS_WITH) {
            pattern += "%";
        }
        return cb.like(field.expression.as(String.class), pattern, '\\');
    }

    private static final class ComparisonField {
        private final Expression<?> expression;
        private final Class<?> type;

        private ComparisonField(Expression<?> expression, Class<?> type) {
            this.expression = expression;
            this.type = type;
        }
    }

    @SuppressWarnings({ "unchecked", "rawtypes" })
    private static Predicate compare(CriteriaBuilder cb, Expression<?> expression, Object value, FilterOperator op) {
        if (!(value instanceof Comparable))
            throw invalid("Field type does not support comparison.");
        Expression<? extends Comparable> path = (Expression<? extends Comparable>) expression;
        Comparable comparable = (Comparable) value;
        return switch (op) {
            case GREATER_THAN -> cb.greaterThan(path, comparable);
            case GREATER_THAN_OR_EQUALS -> cb.greaterThanOrEqualTo(path, comparable);
            case LESS_THAN -> cb.lessThan(path, comparable);
            case LESS_THAN_OR_EQUALS -> cb.lessThanOrEqualTo(path, comparable);
            default -> throw invalid("Unsupported filter operator.");
        };
    }

    @SuppressWarnings({ "rawtypes", "unchecked" })
    private static Object parse(String value, Class<?> type) {
        try {
            if (type == String.class)
                return value;
            if (type == Integer.class || type == int.class)
                return Integer.valueOf(value);
            if (type == Long.class || type == long.class)
                return Long.valueOf(value);
            if (type == Double.class || type == double.class)
                return Double.valueOf(value);
            if (type == Float.class || type == float.class)
                return Float.valueOf(value);
            if (type == BigDecimal.class)
                return new BigDecimal(value);
            if (type == UUID.class)
                return UUID.fromString(value);
            if (type == Boolean.class || type == boolean.class) {
                if (!value.equalsIgnoreCase("true") && !value.equalsIgnoreCase("false"))
                    throw new IllegalArgumentException();
                return Boolean.valueOf(value);
            }
            if (type == LocalDate.class)
                return LocalDate.parse(value);
            if (type == LocalDateTime.class)
                return LocalDateTime.parse(value);
            if (type == OffsetDateTime.class)
                return OffsetDateTime.parse(value);
            if (type == Instant.class)
                return Instant.parse(value);
            if (type.isEnum()) {
                try {
                    return EnumUtils.parse((Class) type.asSubclass(Enum.class), value);
                } catch (IllegalArgumentException e) {
                    throw invalid("Unsupported filter value or field type.");
                }
            }
        } catch (IllegalArgumentException | DateTimeException e) {
            throw invalid("Invalid filter value.");
        }
        throw invalid("Unsupported filter value or field type.");
    }

    @SuppressWarnings("unchecked")
    static <T> Path<T> path(From<?, ?> root, String path) {
        Path<?> result = root;
        for (String segment : path.split("\\.")) {
            try {
                Path<?> next = result.get(segment);
                if (result instanceof From<?, ?> from && next.getModel() instanceof Attribute<?, ?> attribute
                        && attribute.isAssociation()) {
                    if (attribute.isCollection()) {
                        throw invalid("Collection-valued filter paths are not supported.");
                    }
                    List<? extends Join<?, ?>> matches = from.getJoins().stream()
                            .filter(join -> join.getAttribute().getName().equals(segment))
                            .toList();
                    if (matches.size() > 1) {
                        throw invalid("Ambiguous mapped filter path.");
                    }
                    result = matches.isEmpty() ? from.join(segment, JoinType.LEFT) : matches.getFirst();
                } else {
                    result = next;
                }
            } catch (IllegalArgumentException e) {
                throw invalid("Invalid mapped filter path.");
            }
        }
        return (Path<T>) result;
    }

    private static String escape(String text) {
        return text.replace("\\", "\\\\").replace("%", "\\%").replace("_", "\\_");
    }

    private static UnprocessableEntityException invalid(String message) {
        return new UnprocessableEntityException(message);
    }
}
