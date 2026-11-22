package com.roomrental.webapi.controller;

import java.util.Map;

import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/v1/health-check")
public class HealthCheckController {

    @GetMapping("server")
    public String healthy() {
        return "Healthy";
    }

    @GetMapping("/thread-info")
    public Map<String, Object> getThreadInfo() {
        Thread thread = Thread.currentThread();

        return Map.of(
                "name", thread.getName(),
                "isVirtual", thread.isVirtual());
    }
}
