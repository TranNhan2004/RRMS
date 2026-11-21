package com.roomrental.webapi.middleware;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

import java.util.concurrent.atomic.AtomicReference;

import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.slf4j.LoggerFactory;
import org.slf4j.MDC;
import org.springframework.mock.web.MockAsyncContext;
import org.springframework.mock.web.MockHttpServletRequest;
import org.springframework.mock.web.MockHttpServletResponse;
import org.springframework.web.servlet.HandlerMapping;

import ch.qos.logback.classic.Level;
import ch.qos.logback.classic.Logger;
import ch.qos.logback.classic.spi.ILoggingEvent;
import ch.qos.logback.core.read.ListAppender;
import jakarta.servlet.ServletException;

class RequestLoggingFilterTests {
    private final RequestLoggingFilter filter = new RequestLoggingFilter();
    private final Logger logger = (Logger) LoggerFactory.getLogger(RequestLoggingFilter.class);
    private final ListAppender<ILoggingEvent> appender = new ListAppender<>();
    private Level previousLevel;

    @BeforeEach
    void setUp() {
        previousLevel = logger.getLevel();
        logger.setLevel(Level.INFO);
        appender.start();
        logger.addAppender(appender);
    }

    @AfterEach
    void tearDown() {
        logger.detachAppender(appender);
        logger.setLevel(previousLevel);
        appender.stop();
        MDC.clear();
    }

    @Test
    void doFilter_ShouldLogRequestAndRestoreCorrelationContext_WhenRequestCompletes() throws Exception {
        // Arrange
        var request = new MockHttpServletRequest("GET", "/wards/123");
        request.setQueryString("token=secret");
        request.addHeader("X-Client-Action-Id", "action-1");
        request.addHeader("X-Client-Request-Id", "client-2");
        request.addHeader("X-Request-Id", "request-3");
        request.setAttribute(HandlerMapping.BEST_MATCHING_PATTERN_ATTRIBUTE, "/wards/{id}");
        var response = new MockHttpServletResponse();
        var seenAction = new AtomicReference<String>();
        var seenClientRequest = new AtomicReference<String>();
        var seenRequest = new AtomicReference<String>();
        MDC.put("clientActionId", "outer-action");

        // Act
        filter.doFilter(request, response, (incoming, outgoing) -> {
            seenAction.set(MDC.get("clientActionId"));
            seenClientRequest.set(MDC.get("clientRequestId"));
            seenRequest.set(MDC.get("requestId"));
            ((MockHttpServletResponse) outgoing).setStatus(201);
        });

        // Assert
        assertThat(seenAction.get()).isEqualTo("action-1");
        assertThat(seenClientRequest.get()).isEqualTo("client-2");
        assertThat(seenRequest.get()).isEqualTo("request-3");
        assertThat(appender.list).hasSize(1);
        assertThat(appender.list.getFirst().getFormattedMessage())
                .contains("method=GET", "endpoint=/wards/{id}", "status=201", "durationMs=",
                        "clientActionId=action-1", "clientRequestId=client-2", "requestId=request-3")
                .doesNotContain("secret", "token=");
        assertThat(MDC.get("clientActionId")).isEqualTo("outer-action");
        assertThat(MDC.get("clientRequestId")).isNull();
        assertThat(MDC.get("requestId")).isNull();
    }

    @Test
    void doFilter_ShouldRejectUnsafeIdentifiersAndLogException_WhenChainFails() {
        // Arrange
        var request = new MockHttpServletRequest("POST", "/wards");
        request.addHeader("X-Client-Action-Id", "action\nforged");
        request.addHeader("X-Client-Request-Id", "x".repeat(256));
        var response = new MockHttpServletResponse();
        var exception = new ServletException("secret details");

        // Act
        org.assertj.core.api.ThrowableAssert.ThrowingCallable action = () -> filter.doFilter(request, response,
                (incoming, outgoing) -> { throw exception; });

        // Assert
        assertThatThrownBy(action).isSameAs(exception);
        assertThat(appender.list).hasSize(1);
        assertThat(appender.list.getFirst().getFormattedMessage())
                .contains("status=unhandled", "clientActionId=null", "clientRequestId=null")
                .doesNotContain("forged", "secret details", "x".repeat(256));
    }

    @Test
    void doFilter_ShouldLogOnceAtCompletion_WhenRequestIsAsynchronous() throws Exception {
        // Arrange
        var request = new MockHttpServletRequest("GET", "/async");
        request.setAsyncSupported(true);
        var response = new MockHttpServletResponse();

        // Act
        filter.doFilter(request, response, (incoming, outgoing) -> request.startAsync(incoming, outgoing));
        int beforeCompletion = appender.list.size();
        response.setStatus(202);
        ((MockAsyncContext) request.getAsyncContext()).complete();

        // Assert
        assertThat(beforeCompletion).isZero();
        assertThat(appender.list).hasSize(1);
        assertThat(appender.list.getFirst().getFormattedMessage()).contains("endpoint=/async", "status=202");
    }
}
