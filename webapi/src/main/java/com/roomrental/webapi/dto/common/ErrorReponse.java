package com.roomrental.webapi.dto.common;

import java.util.Map;

import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

@Getter
@Setter
@NoArgsConstructor
public class ErrorReponse {
    private String message;
    private Map<String, Object> details;
}
