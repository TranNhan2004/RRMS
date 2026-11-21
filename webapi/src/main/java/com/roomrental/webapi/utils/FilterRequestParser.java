package com.roomrental.webapi.utils;

import java.time.DateTimeException;
import java.time.ZoneId;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Set;

import com.roomrental.webapi.dto.common.DateTimeFilterOptionsRequest;
import com.roomrental.webapi.dto.common.FilterCriterionRequest;
import com.roomrental.webapi.dto.common.FilterRequest;
import com.roomrental.webapi.dto.common.SortFieldRequest;
import com.roomrental.webapi.enums.common.FilterGroupLogic;
import com.roomrental.webapi.enums.common.FilterOperator;
import com.roomrental.webapi.enums.common.TemporalPartType;
import com.roomrental.webapi.exception.UnprocessableEntityException;
import com.roomrental.webapi.model.common.DateTimeFilterOptions;
import com.roomrental.webapi.model.common.FilterCriterion;
import com.roomrental.webapi.model.common.FilterFieldMap;
import com.roomrental.webapi.model.common.FilterGroup;
import com.roomrental.webapi.model.common.FilterRequestModel;
import com.roomrental.webapi.model.common.SortField;

public final class FilterRequestParser {
    private FilterRequestParser() {
    }

    public static <T> FilterRequestModel<T> parse(FilterRequest<T> request) {
        FilterFieldMap fields = java.util.Objects.requireNonNull(request.getFilterFieldMap(), "filterFieldMap").copy();
        int page = request.getCurrentPage() == null ? 1 : request.getCurrentPage();
        int size = request.getItemsPerPage() == null ? Integer.MAX_VALUE : request.getItemsPerPage();
        if (page < 1 || size < 1)
            throw invalid("Pagination values must be positive.");
        ZoneId zone;
        try {
            zone = request.getTimeZoneId() == null ? ZoneId.of("UTC") : ZoneId.of(request.getTimeZoneId());
        } catch (DateTimeException e) {
            throw invalid("Invalid timezone ID.");
        }
        var criteria = request.getFilterCriteria() == null ? List.<FilterCriterionRequest>of()
                : request.getFilterCriteria();
        List<FilterCriterion> mapped = criteria.stream().map(c -> {
            if (c == null)
                throw invalid("Filter criterion is required.");
            fields.require(c.getFieldName());
            DateTimeFilterOptionsRequest options = c.getDateTimeFilterOptions();
            return new FilterCriterion(c.getFieldName(), parseEnum(FilterOperator.class, c.getOperator()),
                    c.getValues() == null ? List.of() : c.getValues(),
                    options == null ? null
                            : new DateTimeFilterOptions(options.getOffsetMinutes(),
                                    parseEnum(TemporalPartType.class, options.getTemporalPartType())));
        }).toList();
        var sorts = request.getSortFields() == null ? List.<SortFieldRequest>of() : request.getSortFields();
        List<SortField> mappedSorts = sorts.stream().map(s -> {
            if (s == null)
                throw invalid("Sort field is required.");
            fields.require(s.getFieldName());
            return new SortField(s.getFieldName(), s.isAscending());
        }).toList();
        return new FilterRequestModel<>(page, size, request.getSearchGlobalText(),
                new GroupParser(request.getGroupPattern(), mapped.size()).parse(), mapped, mappedSorts, zone, fields);
    }

    private static <E extends Enum<E>> E parseEnum(Class<E> type, String value) {
        if (value == null)
            throw invalid("Invalid " + type.getSimpleName() + ".");
        try {
            return EnumUtils.parse(type, value);
        } catch (IllegalArgumentException e) {
            throw invalid("Invalid " + type.getSimpleName() + ".");
        }
    }

    private static UnprocessableEntityException invalid(String message) {
        return new UnprocessableEntityException(message);
    }

    private static final class GroupParser {
        private final String source;
        private final int count;
        private final Set<Integer> used = new HashSet<>();
        private int position;

        GroupParser(String source, int count) {
            this.source = source == null ? "" : source;
            this.count = count;
        }

        FilterGroup parse() {
            if (count == 0)
                return null;
            FilterGroup.Node root = source.isBlank() ? null : or();
            skip();
            if (position != source.length())
                throw invalid("Invalid group pattern.");
            List<FilterGroup.Node> nodes = new ArrayList<>();
            if (root != null)
                nodes.add(root);
            for (int i = 0; i < count; i++)
                if (!used.contains(i))
                    nodes.add(FilterGroup.Node.criterion(i));
            if (nodes.size() == 1 && nodes.getFirst().getGroup() != null)
                return nodes.getFirst().getGroup();
            return new FilterGroup(FilterGroupLogic.AND, List.copyOf(nodes));
        }

        private FilterGroup.Node or() {
            FilterGroup.Node left = and();
            while (keyword("OR"))
                left = combine(FilterGroupLogic.OR, left, and());
            return left;
        }

        private FilterGroup.Node and() {
            FilterGroup.Node left = atom();
            while (keyword("AND"))
                left = combine(FilterGroupLogic.AND, left, atom());
            return left;
        }

        private FilterGroup.Node atom() {
            skip();
            if (take('(')) {
                boolean not = keyword("NOT");
                FilterGroup.Node node = or();
                if (!take(')'))
                    throw invalid("Unmatched '(' in group pattern.");
                return not ? FilterGroup.Node.group(new FilterGroup(FilterGroupLogic.NOT, List.of(node))) : node;
            }
            if (!take('{'))
                throw invalid("Expected criterion in group pattern.");
            skip();
            int start = position;
            while (position < source.length() && Character.isDigit(source.charAt(position)))
                position++;
            String value = source.substring(start, position);
            if (!take('}'))
                throw invalid("Invalid criterion index in group pattern.");
            try {
                var index = Integer.parseInt(value);
                if (index < 0 || index >= count)
                    throw invalid("Criterion index is outside the available range.");
                used.add(index);
                return FilterGroup.Node.criterion(index);
            } catch (NumberFormatException e) {
                throw invalid("Invalid criterion index in group pattern.");
            }
        }

        private FilterGroup.Node combine(FilterGroupLogic logic, FilterGroup.Node a, FilterGroup.Node b) {
            return FilterGroup.Node.group(new FilterGroup(logic, List.of(a, b)));
        }

        private void skip() {
            while (position < source.length() && Character.isWhitespace(source.charAt(position)))
                position++;
        }

        private boolean take(char c) {
            skip();
            if (position < source.length() && source.charAt(position) == c) {
                position++;
                return true;
            }
            return false;
        }

        private boolean keyword(String word) {
            skip();
            int end = position + word.length();
            if (end <= source.length() && source.regionMatches(true, position, word, 0, word.length())
                    && (end == source.length() || !Character.isLetterOrDigit(source.charAt(end)))) {
                position = end;
                return true;
            }
            return false;
        }
    }
}
