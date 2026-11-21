package com.roomrental.webapi.repository;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.verifyNoInteractions;
import static org.mockito.Mockito.when;

import java.time.ZoneId;
import java.util.List;
import java.util.Set;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import com.roomrental.webapi.abstraction.repository.FilterQuery;
import com.roomrental.webapi.entity.Ward;
import com.roomrental.webapi.exception.UnprocessableEntityException;
import com.roomrental.webapi.model.common.FilterFieldMap;
import com.roomrental.webapi.model.common.FilterRequestModel;
import com.roomrental.webapi.model.common.SortField;

import jakarta.persistence.EntityManager;
import jakarta.persistence.TypedQuery;
import jakarta.persistence.criteria.CriteriaBuilder;
import jakarta.persistence.criteria.CriteriaQuery;
import jakarta.persistence.criteria.Expression;
import jakarta.persistence.criteria.Order;
import jakarta.persistence.criteria.Path;
import jakarta.persistence.criteria.Predicate;
import jakarta.persistence.criteria.Root;
import jakarta.persistence.criteria.Selection;

@ExtendWith(MockitoExtension.class)
class FilterHandlerTests {
    record WardRow(String name) {
    }

    @Mock
    private EntityManager entityManager;
    @Mock
    private FilterQuery<Ward, WardRow> unusedDefinition;
    @InjectMocks
    private FilterHandler handler;

    @Test
    @SuppressWarnings({ "unchecked" })
    void applyFilter_ShouldCombineBaseRestrictionsAndPageResults_WhenQueryIsExecuted() {
        // Arrange
        var builder = mock(CriteriaBuilder.class);
        CriteriaQuery<WardRow> data = mock(CriteriaQuery.class);
        CriteriaQuery<Long> count = mock(CriteriaQuery.class);
        Root<Ward> dataRoot = mock(Root.class);
        Root<Ward> countRoot = mock(Root.class);
        Predicate baseData = mock(Predicate.class);
        Predicate baseCount = mock(Predicate.class);
        Predicate dynamic = mock(Predicate.class);
        Predicate dataCombined = mock(Predicate.class);
        Predicate countCombined = mock(Predicate.class);
        Selection<WardRow> selection = mock(Selection.class);
        Expression<Long> countSelection = mock(Expression.class);
        TypedQuery<Long> countExecutor = mock(TypedQuery.class);
        TypedQuery<WardRow> dataExecutor = mock(TypedQuery.class);
        Path<Object> name = mock(Path.class);
        Order order = mock(Order.class);
        var fields = new FilterFieldMap().map("name", "name", String.class);
        var request = new FilterRequestModel<WardRow>(2, 2, null, null, List.of(),
                List.of(new SortField("name", true)), ZoneId.of("UTC"), fields);
        var definition = new FilterQuery<Ward, WardRow>() {
            public Class<Ward> entityType() {
                return Ward.class;
            }

            public void configure(CriteriaBuilder cb, CriteriaQuery<?> query, Root<Ward> root) {
            }

            public Selection<WardRow> select(CriteriaBuilder cb, Root<Ward> root, Class<WardRow> type) {
                return selection;
            }
        };
        when(entityManager.getCriteriaBuilder()).thenReturn(builder);
        when(builder.createQuery(WardRow.class)).thenReturn(data);
        when(builder.createQuery(Long.class)).thenReturn(count);
        when(data.from(Ward.class)).thenReturn(dataRoot);
        when(count.from(Ward.class)).thenReturn(countRoot);
        when(data.getRoots()).thenReturn(Set.of(dataRoot));
        when(count.getRoots()).thenReturn(Set.of(countRoot));
        when(data.getRestriction()).thenReturn(baseData);
        when(count.getRestriction()).thenReturn(baseCount);
        when(builder.and()).thenReturn(dynamic);
        when(builder.and(baseData, dynamic)).thenReturn(dataCombined);
        when(builder.and(baseCount, dynamic)).thenReturn(countCombined);
        when(builder.count(countRoot)).thenReturn(countSelection);
        when(dataRoot.get("name")).thenReturn(name);
        when(builder.asc(name)).thenReturn(order);
        when(entityManager.createQuery(count)).thenReturn(countExecutor);
        when(entityManager.createQuery(data)).thenReturn(dataExecutor);
        when(countExecutor.getSingleResult()).thenReturn(5L);
        when(dataExecutor.setFirstResult(2)).thenReturn(dataExecutor);
        when(dataExecutor.setMaxResults(2)).thenReturn(dataExecutor);
        when(dataExecutor.getResultList()).thenReturn(List.of(new WardRow("A"), new WardRow("B")));

        // Act
        var result = handler.applyFilter(request, definition, WardRow.class);

        // Assert
        verify(data).where(dataCombined);
        verify(count).where(countCombined);
        verify(data).orderBy(List.of(order));
        verify(dataExecutor).setFirstResult(2);
        verify(dataExecutor).setMaxResults(2);
        assertThat(result.getItems()).containsExactly(new WardRow("A"), new WardRow("B"));
        assertThat(result.getTotalItems()).isEqualTo(5);
        assertThat(result.getTotalPages()).isEqualTo(3);
        assertThat(result.isHasPreviousPage()).isTrue();
        assertThat(result.isHasNextPage()).isTrue();
    }

    @Test
    void applyFilter_ShouldRejectOutOfRangeOffset_WhenPageIsTooLarge() {
        // Arrange
        var request = new FilterRequestModel<WardRow>(Integer.MAX_VALUE, 2, null, null,
                List.of(), List.of(), ZoneId.of("UTC"), new FilterFieldMap());

        // Act
        org.assertj.core.api.ThrowableAssert.ThrowingCallable action = () -> handler.applyFilter(request,
                unusedDefinition,
                WardRow.class);

        // Assert
        assertThatThrownBy(action).isInstanceOf(UnprocessableEntityException.class);
        verifyNoInteractions(entityManager);
    }

    @Test
    void applyFilter_ShouldRejectMapping_WhenResponseModelDoesNotContainField() {
        // Arrange
        var fields = new FilterFieldMap().map("missing", "name", String.class);
        var request = new FilterRequestModel<WardRow>(1, 10, null, null, List.of(), List.of(), ZoneId.of("UTC"),
                fields);

        // Act
        org.assertj.core.api.ThrowableAssert.ThrowingCallable action = () -> handler.applyFilter(request,
                unusedDefinition,
                WardRow.class);

        // Assert
        assertThatThrownBy(action).isInstanceOf(IllegalArgumentException.class)
                .hasMessageContaining("missing");
        verifyNoInteractions(entityManager);
    }
}
