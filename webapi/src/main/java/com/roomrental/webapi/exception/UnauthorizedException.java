package com.roomrental.webapi.exception;

import java.util.Map;

import lombok.Getter;

@Getter
public class UnauthorizedException extends Exception {
    private String message;
    private Map<String, Object> details;
    private Exception innerException;

    public UnauthorizedException(String message, Map<String, Object> details, Exception innerException) {
        this.message = message;
        this.details = details;
        this.innerException = innerException;
    }

    public UnauthorizedException(String message, Map<String, Object> details) {
        this.message = message;
        this.details = details;
    }

    public UnauthorizedException(String message) {
        this.message = message;
    }
}
