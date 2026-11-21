package com.roomrental.webapi.middleware;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.catchThrowable;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import java.util.Map;
import java.util.stream.Stream;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.Arguments;
import org.junit.jupiter.params.provider.MethodSource;
import org.springframework.http.MediaType;
import org.springframework.security.access.AccessDeniedException;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.MvcResult;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RestController;

import com.roomrental.webapi.exception.ConflictException;
import com.roomrental.webapi.exception.ForbiddenException;
import com.roomrental.webapi.exception.NotFoundException;
import com.roomrental.webapi.exception.UnauthorizedException;
import com.roomrental.webapi.exception.UnprocessableEntityException;

class GlobalExceptionHandlerTest {
    private static final Map<String, Object> DETAILS = Map.of("field", "name");

    @ParameterizedTest
    @MethodSource("businessExceptions")
    void handleBusinessException_ShouldReturnStatusAndDetails_WhenExceptionIsThrown(Exception exception, int statusCode)
            throws Exception {
        // Arrange
        MockMvc mvc = MockMvcBuilders.standaloneSetup(new TestController(exception))
                .setControllerAdvice(new GlobalExceptionHandler()).build();

        // Act
        MvcResult result = mvc.perform(get("/test"))
                .andExpect(status().is(statusCode)).andReturn();

        // Assert
        assertThat(result.getResponse().getContentAsString())
                .contains("\"message\":\"Business error\"", "\"field\":\"name\"")
                .doesNotContain("secret cause");
    }

    private static Stream<Arguments> businessExceptions() {
        var cause = new Exception("secret cause");
        return Stream.of(
                Arguments.of(new ConflictException("Business error", DETAILS, cause), 409),
                Arguments.of(new ForbiddenException("Business error", DETAILS, cause), 403),
                Arguments.of(new NotFoundException("Business error", DETAILS, cause), 404),
                Arguments.of(new UnauthorizedException("Business error", DETAILS, cause), 401),
                Arguments.of(new UnprocessableEntityException("Business error", DETAILS, cause), 422));
    }

    @Test
    void handleUnexpected_ShouldHideInternalDetails_WhenExceptionIsThrown() throws Exception {
        // Arrange
        MockMvc mvc = MockMvcBuilders.standaloneSetup(new TestController(new IllegalStateException("secret")))
                .setControllerAdvice(new GlobalExceptionHandler()).build();

        // Act
        MvcResult result = mvc.perform(get("/test"))
                .andExpect(status().isInternalServerError()).andReturn();

        // Assert
        assertThat(result.getResponse().getContentAsString())
                .contains("Internal server error").doesNotContain("secret", "IllegalStateException");
    }

    @Test
    void handleFrameworkException_ShouldPreserveBadRequest_WhenJsonIsMalformed() throws Exception {
        // Arrange
        MockMvc mvc = MockMvcBuilders.standaloneSetup(new TestController(null))
                .setControllerAdvice(new GlobalExceptionHandler()).build();

        // Act
        MvcResult result = mvc.perform(post("/test").contentType(MediaType.APPLICATION_JSON).content("{"))
                .andExpect(status().isBadRequest()).andReturn();

        // Assert
        assertThat(result.getResponse().getContentAsString()).contains("Bad Request", "details");
    }

    @Test
    void handleUnexpected_ShouldPropagateAccessDenied_WhenSpringSecurityRejectsRequest() {
        // Arrange
        var exception = new AccessDeniedException("denied");
        var handler = new GlobalExceptionHandler();

        // Act
        Throwable thrown = catchThrowable(() -> handler.handleUnexpected(exception));

        // Assert
        assertThat(thrown).isSameAs(exception);
    }

    @RestController
    static class TestController {
        private final Exception exception;

        TestController(Exception exception) {
            this.exception = exception;
        }

        @GetMapping("/test")
        void get() throws Exception {
            throw exception;
        }

        @PostMapping("/test")
        void post(@RequestBody Map<String, Object> body) {
        }
    }
}
