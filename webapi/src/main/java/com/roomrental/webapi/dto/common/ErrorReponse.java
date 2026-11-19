package com.roomrental.webapi.dto.common;

import java.util.Map;

import lombok.Data;

@Data
public class ErrorReponse {
    private String message;
    private Map<String, Object> details;
}
