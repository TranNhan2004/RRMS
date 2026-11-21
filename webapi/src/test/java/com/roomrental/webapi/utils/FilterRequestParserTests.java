package com.roomrental.webapi.utils;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.catchThrowable;

import java.util.List;

import org.junit.jupiter.api.Test;

import com.roomrental.webapi.dto.common.FilterCriterionRequest;
import com.roomrental.webapi.dto.common.FilterRequest;
import com.roomrental.webapi.enums.common.FilterGroupLogic;
import com.roomrental.webapi.enums.common.FilterOperator;
import com.roomrental.webapi.exception.UnprocessableEntityException;
import com.roomrental.webapi.model.common.FilterFieldMap;
import com.roomrental.webapi.model.common.FilterRequestModel;

class FilterRequestParserTests {
    private static final class TestRequest extends FilterRequest<Object> {
        TestRequest(Integer page, Integer size, String search, String group, List<FilterCriterionRequest> criteria,
                List<com.roomrental.webapi.dto.common.SortFieldRequest> sorts, String zone) {
            super(page, size, search, group, criteria, sorts, zone);
        }

        @Override
        public FilterFieldMap getFilterFieldMap() {
            return new FilterFieldMap().map("name", "name", String.class).map("age", "age", Integer.class);
        }
    }

    private TestRequest request(String pattern) {
        return new TestRequest(1, 20, null, pattern, List.of(
                new FilterCriterionRequest("NAME", "CONTAINS", List.of("Ada"), null),
                new FilterCriterionRequest("age", "GREATER_THAN", List.of("18"), null),
                new FilterCriterionRequest("name", "IS_NULL", List.of(), null)), List.of(), null);
    }

    @Test
    void toModel_ShouldParsePrecedenceAndAppendMissingCriteria_WhenPatternOmitsCriterion() {
        // Arrange
        var input = request("{0} OR (NOT {1})");

        // Act
        FilterRequestModel<Object> model = FilterRequestParser.parse(input);

        // Assert
        assertThat(model.getFilterCriteria().getFirst().getOperator()).isEqualTo(FilterOperator.CONTAINS);
        assertThat(model.getFilterGroup().getLogic()).isEqualTo(FilterGroupLogic.AND);
        assertThat(model.getFilterGroup().getChildren().getFirst().getGroup().getLogic())
                .isEqualTo(FilterGroupLogic.OR);
        assertThat(model.getFilterGroup().getChildren().get(1).getCriterionIndex()).isEqualTo(2);
    }

    @Test
    void toModel_ShouldRejectInvalidPattern_WhenCriterionIsOutOfRange() {
        // Arrange
        var input = request("{9}");

        // Act
        Throwable error = catchThrowable(() -> FilterRequestParser.parse(input));

        // Assert
        assertThat(error).isInstanceOf(UnprocessableEntityException.class);
    }

    @Test
    void toModel_ShouldRejectUnknownFields_WhenNotAllowlisted() {
        // Arrange
        var request = new TestRequest(1, 10, null, null,
                List.of(new FilterCriterionRequest("secret", "Equals", List.of("x"), null)), null, null);

        // Act
        Throwable error = catchThrowable(() -> FilterRequestParser.parse(request));

        // Assert
        assertThat(error).isInstanceOf(UnprocessableEntityException.class);
    }

    @Test
    void toModel_ShouldRejectInvalidTimezone_WhenTimezoneIsUnknown() {
        // Arrange
        var request = new TestRequest(1, 10, null, null, null, null, "invalid/timezone");

        // Act
        Throwable error = catchThrowable(() -> FilterRequestParser.parse(request));

        // Assert
        assertThat(error).isInstanceOf(UnprocessableEntityException.class);
    }

    @Test
    void toModel_ShouldParseOperator_WhenNameUsesUpperSnakeCase() {
        // Arrange
        var request = new TestRequest(1, 10, null, null,
                List.of(new FilterCriterionRequest("age", "GREATER_THAN", List.of("18"), null)), null, null);

        // Act
        FilterRequestModel<Object> model = FilterRequestParser.parse(request);

        // Assert
        assertThat(model.getFilterCriteria().getFirst().getOperator())
                .isEqualTo(FilterOperator.GREATER_THAN);
    }

    @Test
    void toModel_ShouldRejectOperator_WhenValueIsInvalid() {
        // Arrange
        var request = new TestRequest(1, 10, null, null,
                List.of(new FilterCriterionRequest("age", "GreaterThan", List.of("18"), null)), null, null);

        // Act
        Throwable error = catchThrowable(() -> FilterRequestParser.parse(request));

        // Assert
        assertThat(error).isInstanceOf(UnprocessableEntityException.class);
    }
}
