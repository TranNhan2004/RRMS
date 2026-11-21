package com.roomrental.webapi.middleware;

import java.io.IOException;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.regex.Pattern;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.slf4j.MDC;
import org.springframework.core.Ordered;
import org.springframework.core.annotation.Order;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;
import org.springframework.web.servlet.HandlerMapping;

import jakarta.servlet.AsyncEvent;
import jakarta.servlet.AsyncListener;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;

/** Logs request completion without reading request or response bodies. */
@Component
@Order(Ordered.HIGHEST_PRECEDENCE + 20)
public class RequestLoggingFilter extends OncePerRequestFilter {
    private static final Logger LOGGER = LoggerFactory.getLogger(RequestLoggingFilter.class);
    private static final Pattern SAFE_ID = Pattern.compile("[A-Za-z0-9._:-]+");
    private static final String ACTION_KEY = "clientActionId";
    private static final String CLIENT_REQUEST_KEY = "clientRequestId";
    private static final String REQUEST_KEY = "requestId";

    @Override
    protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response, FilterChain chain)
            throws ServletException, IOException {
        long started = System.nanoTime();
        String actionId = safeId(request.getHeader("X-Client-Action-Id"));
        String clientRequestId = safeId(request.getHeader("X-Client-Request-Id"));
        String requestId = safeId(request.getHeader("X-Request-Id"));
        String previousAction = MDC.get(ACTION_KEY);
        String previousClientRequest = MDC.get(CLIENT_REQUEST_KEY);
        String previousRequest = MDC.get(REQUEST_KEY);
        putOrRemove(ACTION_KEY, actionId);
        putOrRemove(CLIENT_REQUEST_KEY, clientRequestId);
        putOrRemove(REQUEST_KEY, requestId);
        var logged = new AtomicBoolean();
        boolean failed = false;
        try {
            chain.doFilter(request, response);
        } catch (IOException | ServletException | RuntimeException | Error exception) {
            failed = true;
            throw exception;
        } finally {
            try {
                boolean unhandled = failed;
                String traceId = safeId(MDC.get("traceId"));
                if (request.isAsyncStarted()) {
                    try {
                        request.getAsyncContext().addListener(new AsyncListener() {
                            @Override
                            public void onComplete(AsyncEvent event) {
                                logCompletion(request, response, started, actionId, clientRequestId, requestId,
                                        traceId, unhandled, logged);
                            }

                            @Override
                            public void onTimeout(AsyncEvent event) {
                            }

                            @Override
                            public void onError(AsyncEvent event) {
                            }

                            @Override
                            public void onStartAsync(AsyncEvent event) {
                                event.getAsyncContext().addListener(this);
                            }
                        });
                    } catch (IllegalStateException exception) {
                        // The async request may complete between the check and listener registration.
                        logCompletion(request, response, started, actionId, clientRequestId, requestId,
                                traceId, unhandled, logged);
                    }
                } else {
                    logCompletion(request, response, started, actionId, clientRequestId, requestId,
                            traceId, unhandled, logged);
                }
            } finally {
                putOrRemove(ACTION_KEY, previousAction);
                putOrRemove(CLIENT_REQUEST_KEY, previousClientRequest);
                putOrRemove(REQUEST_KEY, previousRequest);
            }
        }
    }

    private static void logCompletion(HttpServletRequest request, HttpServletResponse response, long started,
            String actionId, String clientRequestId, String requestId, String traceId, boolean failed,
            AtomicBoolean logged) {
        if (!logged.compareAndSet(false, true))
            return;
        Object pattern = request.getAttribute(HandlerMapping.BEST_MATCHING_PATTERN_ATTRIBUTE);
        String endpoint = pattern == null ? request.getRequestURI() : pattern.toString();
        String status = failed && response.getStatus() < 400 ? "unhandled" : Integer.toString(response.getStatus());
        long durationMs = TimeUnit.NANOSECONDS.toMillis(System.nanoTime() - started);
        LOGGER.info(
                "HTTP request method={} endpoint={} status={} durationMs={} clientActionId={} clientRequestId={} requestId={} traceId={}",
                safeText(request.getMethod(), 32), safeText(endpoint, 1024), status, durationMs,
                actionId, clientRequestId, requestId, traceId);
    }

    private static String safeId(String value) {
        return value != null && value.length() <= 255 && SAFE_ID.matcher(value).matches() ? value : null;
    }

    private static String safeText(String value, int limit) {
        if (value == null)
            return "-";
        var result = new StringBuilder();
        for (int i = 0; i < value.length() && result.length() < limit; i++) {
            char character = value.charAt(i);
            result.append(Character.isISOControl(character) ? '_' : character);
        }
        return result.toString();
    }

    private static void putOrRemove(String key, String value) {
        if (value == null)
            MDC.remove(key);
        else
            MDC.put(key, value);
    }
}
