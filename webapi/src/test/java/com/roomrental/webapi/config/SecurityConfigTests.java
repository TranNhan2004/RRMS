package com.roomrental.webapi.config;

import static org.springframework.security.test.web.servlet.request.SecurityMockMvcRequestPostProcessors.user;
import static org.springframework.security.test.web.servlet.request.SecurityMockMvcRequestPostProcessors.csrf;
import static org.springframework.security.test.web.servlet.setup.SecurityMockMvcConfigurers.springSecurity;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.redirectedUrl;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.ValueSource;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Import;
import org.springframework.http.MediaType;
import org.springframework.security.core.userdetails.User;
import org.springframework.security.core.userdetails.UserDetailsService;
import org.springframework.security.provisioning.InMemoryUserDetailsManager;
import org.springframework.test.context.junit.jupiter.web.SpringJUnitWebConfig;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.web.context.WebApplicationContext;
import org.springframework.web.servlet.config.annotation.EnableWebMvc;

import com.roomrental.webapi.config.security.SecurityConfig;

@SpringJUnitWebConfig(SecurityConfigTests.TestConfig.class)
class SecurityConfigTests {
    @Autowired
    private WebApplicationContext context;
    private MockMvc mvc;

    @BeforeEach
    void setUp() {
        mvc = MockMvcBuilders.webAppContextSetup(context).apply(springSecurity()).build();
    }

    @ParameterizedTest
    @ValueSource(strings = { "/swagger-ui.html", "/swagger-ui/index.html", "/v3/api-docs",
            "/v3/api-docs/swagger-config" })
    void securityFilterChain_ShouldAllowSwagger_WhenUnauthenticated(String path) throws Exception {
        // Arrange
        var request = get(path);

        // Act
        var result = mvc.perform(request);

        // Assert
        result.andExpect(status().isOk());
    }

    @Test
    void securityFilterChain_ShouldRedirectToLogin_WhenApiRequestIsUnauthenticated() throws Exception {
        // Arrange
        var request = get("/api/protected").accept(MediaType.TEXT_HTML);

        // Act
        var result = mvc.perform(request);

        // Assert
        result.andExpect(status().isFound()).andExpect(redirectedUrl("/login"));
    }

    @Test
    void securityFilterChain_ShouldReturnUnauthorized_WhenApiRequestIsUnauthenticated() throws Exception {
        // Arrange
        var request = get("/api/protected").accept(MediaType.APPLICATION_JSON);

        // Act
        var result = mvc.perform(request);

        // Assert
        result.andExpect(status().isUnauthorized());
    }

    @Test
    void securityFilterChain_ShouldAllowProtectedApi_WhenAuthenticated() throws Exception {
        // Arrange
        var request = get("/api/protected").with(user("test"));

        // Act
        var result = mvc.perform(request);

        // Assert
        result.andExpect(status().isOk());
    }

    @Test
    void securityFilterChain_ShouldRejectPost_WhenCsrfTokenIsMissing() throws Exception {
        // Arrange
        var request = post("/api/v1/addresses/provinces");

        // Act
        var result = mvc.perform(request);

        // Assert
        result.andExpect(status().isForbidden());
    }

    @Test
    void securityFilterChain_ShouldAllowPost_WhenCsrfTokenIsPresent() throws Exception {
        // Arrange
        var request = post("/api/v1/addresses/provinces").with(csrf());

        // Act
        var result = mvc.perform(request);

        // Assert
        result.andExpect(status().isOk());
    }

    @Configuration
    @EnableWebMvc
    @Import({ SecurityConfig.class, TestController.class })
    static class TestConfig {
        @Bean
        UserDetailsService userDetailsService() {
            return new InMemoryUserDetailsManager(
                    User.withUsername("test").password("{noop}test").roles("USER").build());
        }
    }

    @RestController
    static class TestController {
        @GetMapping({ "/swagger-ui.html", "/swagger-ui/index.html", "/v3/api-docs", "/v3/api-docs/swagger-config",
                "/api/protected" })
        String get() {
            return "ok";
        }

        @PostMapping({ "/api/v1/addresses/provinces", "/api/protected" })
        String post() {
            return "ok";
        }
    }
}
