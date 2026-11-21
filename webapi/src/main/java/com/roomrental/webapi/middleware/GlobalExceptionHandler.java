package com.roomrental.webapi.middleware;

import java.util.Map;

import org.jspecify.annotations.Nullable;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.HttpStatusCode;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.AccessDeniedException;
import org.springframework.security.core.AuthenticationException;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;
import org.springframework.web.context.request.WebRequest;
import org.springframework.web.servlet.mvc.method.annotation.ResponseEntityExceptionHandler;

import com.roomrental.webapi.dto.common.ErrorReponse;
import com.roomrental.webapi.exception.ConflictException;
import com.roomrental.webapi.exception.ForbiddenException;
import com.roomrental.webapi.exception.NotFoundException;
import com.roomrental.webapi.exception.UnauthorizedException;
import com.roomrental.webapi.exception.UnprocessableEntityException;

@RestControllerAdvice
public class GlobalExceptionHandler extends ResponseEntityExceptionHandler {
    private static final Logger LOGGER = LoggerFactory.getLogger(GlobalExceptionHandler.class);

    @ExceptionHandler(ConflictException.class)
    public ResponseEntity<ErrorReponse> handleConflict(ConflictException exception) {
        return response(HttpStatus.CONFLICT, exception.getMessage(), exception.getDetails());
    }

    @ExceptionHandler(ForbiddenException.class)
    public ResponseEntity<ErrorReponse> handleForbidden(ForbiddenException exception) {
        return response(HttpStatus.FORBIDDEN, exception.getMessage(), exception.getDetails());
    }

    @ExceptionHandler(NotFoundException.class)
    public ResponseEntity<ErrorReponse> handleNotFound(NotFoundException exception) {
        return response(HttpStatus.NOT_FOUND, exception.getMessage(), exception.getDetails());
    }

    @ExceptionHandler(UnauthorizedException.class)
    public ResponseEntity<ErrorReponse> handleUnauthorized(UnauthorizedException exception) {
        return response(HttpStatus.UNAUTHORIZED, exception.getMessage(), exception.getDetails());
    }

    @ExceptionHandler(UnprocessableEntityException.class)
    public ResponseEntity<ErrorReponse> handleUnprocessableEntity(UnprocessableEntityException exception) {
        return response(HttpStatus.UNPROCESSABLE_CONTENT, exception.getMessage(), exception.getDetails());
    }

    @Override
    protected ResponseEntity<Object> handleExceptionInternal(
            Exception exception,
            @Nullable Object body,
            HttpHeaders headers,
            HttpStatusCode status,
            WebRequest request) {

        HttpStatus knownStatus = HttpStatus.resolve(status.value());
        String message = status.is5xxServerError() ? "Internal server error"
                : knownStatus != null ? knownStatus.getReasonPhrase() : "Request failed";
        return new ResponseEntity<>(error(message, null), headers, status);
    }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<ErrorReponse> handleUnexpected(Exception exception) throws Exception {
        // Let Spring Security's filter chain handle authentication and authorization
        // failures.
        if (exception instanceof AuthenticationException || exception instanceof AccessDeniedException) {
            throw exception;
        }
        LOGGER.error("Unhandled exception of type {}", exception.getClass().getName());
        return response(HttpStatus.INTERNAL_SERVER_ERROR, "Internal server error", null);
    }

    private static ResponseEntity<ErrorReponse> response(
            HttpStatus status,
            String message,
            Map<String, Object> details) {
        return ResponseEntity.status(status).body(error(message, details));
    }

    private static ErrorReponse error(String message, Map<String, Object> details) {
        var response = new ErrorReponse();
        response.setMessage(message);
        response.setDetails(details);
        return response;
    }
}
