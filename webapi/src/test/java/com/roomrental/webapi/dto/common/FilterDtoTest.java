package com.roomrental.webapi.dto.common;

import static org.assertj.core.api.Assertions.assertThat;

import java.util.List;
import org.junit.jupiter.api.Test;
import tools.jackson.databind.JsonNode;
import tools.jackson.databind.json.JsonMapper;

class FilterDtoTest {
    public static class TestRequest extends FilterRequest<Object> {
        @Override public com.roomrental.webapi.model.common.FilterFieldMap getFilterFieldMap() {
            return new com.roomrental.webapi.model.common.FilterFieldMap().map("name", "name", String.class);
        }
    }
    private final JsonMapper mapper = JsonMapper.builder().build();

    @Test
    void deserialize_ShouldReadFilterFields_WhenRequestUsesApiPropertyNames() {
        // Arrange
        String json = """
                {"currentPage":2,"itemsPerPage":10,"sortFields":[{"fieldName":"name","isAscending":true}]}
                """;

        // Act
        var request = mapper.readValue(json, TestRequest.class);

        // Assert
        assertThat(request.getCurrentPage()).isEqualTo(2);
        assertThat(request.getSortFields().getFirst().isAscending()).isTrue();
    }

    @Test
    void serialize_ShouldKeepResponsePropertyNames_WhenResponseIsClass() {
        // Arrange
        var response = new FilterResponse<>(List.of("item"), 1, 10, 1, 1, false, false);

        // Act
        JsonNode json = mapper.readTree(mapper.writeValueAsString(response));

        // Assert
        assertThat(json.get("items").get(0).asString()).isEqualTo("item");
        assertThat(json.get("hasNextPage").asBoolean()).isFalse();
    }

    @Test
    void serialize_ShouldHideServerDefinedMappings_WhenRequestIsSerialized() {
        // Arrange
        var request = new TestRequest();

        // Act
        JsonNode json = mapper.readTree(mapper.writeValueAsString(request));

        // Assert
        assertThat(json.has("filterFieldMap")).isFalse();
    }
}
