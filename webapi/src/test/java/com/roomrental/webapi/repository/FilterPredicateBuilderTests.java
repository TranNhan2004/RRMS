package com.roomrental.webapi.repository;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.catchThrowable;
import static org.mockito.Mockito.*;

import java.time.ZoneId;
import java.util.List;

import org.junit.jupiter.api.Test;

import com.roomrental.webapi.enums.common.FilterOperator;
import com.roomrental.webapi.exception.UnprocessableEntityException;
import com.roomrental.webapi.model.common.FilterCriterion;
import com.roomrental.webapi.model.common.FilterFieldMap;
import com.roomrental.webapi.model.common.FilterRequestModel;

import jakarta.persistence.criteria.CriteriaBuilder;
import jakarta.persistence.criteria.Path;
import jakarta.persistence.criteria.Root;
import jakarta.persistence.criteria.Join;
import jakarta.persistence.criteria.JoinType;
import jakarta.persistence.metamodel.SingularAttribute;

class FilterPredicateBuilderTests {
    private enum Status { AVAILABLE, OCCUPIED }

    @Test
    @SuppressWarnings({"unchecked", "rawtypes"})
    void path_ShouldUseLeftJoin_WhenNavigatingAnAssociation() {
        // Arrange
        Root<Object> root = mock(Root.class);
        Path<Object> association = mock(Path.class);
        Path<Object> name = mock(Path.class);
        Join<Object, Object> join = mock(Join.class);
        SingularAttribute attribute = mock(SingularAttribute.class);
        when(root.get("province")).thenReturn(association);
        when(association.getModel()).thenReturn(attribute);
        when(attribute.isAssociation()).thenReturn(true);
        when(root.join("province", JoinType.LEFT)).thenReturn(join);
        when(join.get("name")).thenReturn(name);

        // Act
        Path<Object> result = FilterPredicateBuilder.path(root, "province.name");

        // Assert
        org.assertj.core.api.Assertions.assertThat(result).isSameAs(name);
        verify(root).join("province", JoinType.LEFT);
    }

    @Test
    @SuppressWarnings("unchecked")
    void build_ShouldRejectMultipleValues_WhenComparisonRequiresOne() {
        // Arrange
        Root<Object> root = mock(Root.class);
        when(root.get("age")).thenReturn(mock(Path.class));
        var fields = new FilterFieldMap().map("age", "age", Integer.class);
        var request = request("age", FilterOperator.GREATER_THAN, List.of("1", "2"), fields);
        var builder = mock(CriteriaBuilder.class);

        // Act
        Throwable error = catchThrowable(() -> FilterPredicateBuilder.build(root, builder, request));

        // Assert
        assertThat(error)
                .isInstanceOf(UnprocessableEntityException.class);
    }

    @Test
    @SuppressWarnings("unchecked")
    void build_ShouldRejectInvalidValue_WhenValueCannotBeParsed() {
        // Arrange
        Root<Object> root = mock(Root.class);
        when(root.get("age")).thenReturn(mock(Path.class));
        var fields = new FilterFieldMap().map("age", "age", Integer.class);
        var request = request("age", FilterOperator.EQUALS, List.of("not-a-number"), fields);
        var builder = mock(CriteriaBuilder.class);

        // Act
        Throwable error = catchThrowable(() -> FilterPredicateBuilder.build(root, builder, request));

        // Assert
        assertThat(error)
                .isInstanceOf(UnprocessableEntityException.class);
    }

    @Test
    @SuppressWarnings("unchecked")
    void build_ShouldParseEnumName_WhenValueMatchesJavaName() {
        // Arrange
        Root<Object> root = mock(Root.class);
        when(root.get("status")).thenReturn(mock(Path.class));
        var fields = new FilterFieldMap().map("status", "status", Status.class);
        var request = request("status", FilterOperator.EQUALS, List.of("AVAILABLE"), fields);
        var builder = mock(CriteriaBuilder.class);

        // Act
        FilterPredicateBuilder.build(root, builder, request);

        // Assert
        verify(builder).equal(any(), eq(Status.AVAILABLE));
    }

    @Test
    @SuppressWarnings("unchecked")
    void build_ShouldRejectEnumValue_WhenNameIsUnknown() {
        // Arrange
        Root<Object> root = mock(Root.class);
        when(root.get("status")).thenReturn(mock(Path.class));
        var fields = new FilterFieldMap().map("status", "status", Status.class);
        var request = request("status", FilterOperator.EQUALS, List.of("available"), fields);
        var builder = mock(CriteriaBuilder.class);

        // Act
        Throwable error = catchThrowable(() -> FilterPredicateBuilder.build(root, builder, request));

        // Assert
        assertThat(error)
                .isInstanceOf(UnprocessableEntityException.class)
                .hasMessage("Unsupported filter value or field type.");
    }

    private static FilterRequestModel<Object> request(String field, FilterOperator operator,
            List<String> values, FilterFieldMap fields) {
        return new FilterRequestModel<>(1, 10, null, null,
                List.of(new FilterCriterion(field, operator, values, null)), List.of(), ZoneId.of("UTC"), fields);
    }
}
