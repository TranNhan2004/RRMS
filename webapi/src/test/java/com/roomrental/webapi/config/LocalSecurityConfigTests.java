package com.roomrental.webapi.config;

import static org.springframework.security.test.web.servlet.setup.SecurityMockMvcConfigurers.springSecurity;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.MediaType;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.context.junit.jupiter.web.SpringJUnitWebConfig;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;
import org.springframework.web.context.WebApplicationContext;

@ActiveProfiles("local")
@SpringJUnitWebConfig(SecurityConfigTests.TestConfig.class)
class LocalSecurityConfigTests {
    @Autowired
    private WebApplicationContext context;
    private MockMvc mvc;

    @BeforeEach
    void setUp() {
        mvc = MockMvcBuilders.webAppContextSetup(context).apply(springSecurity()).build();
    }

    @Test
    void securityFilterChain_ShouldAllowPublicPost_WhenLocalAndCsrfTokenIsMissing() throws Exception {
        // Arrange
        var request = post("/api/v1/addresses/provinces");

        // Act
        var result = mvc.perform(request);

        // Assert
        result.andExpect(status().isOk());
    }

    @Test
    void securityFilterChain_ShouldRequireAuthentication_WhenLocalAndEndpointIsProtected() throws Exception {
        // Arrange
        var request = post("/api/protected").accept(MediaType.APPLICATION_JSON);

        // Act
        var result = mvc.perform(request);

        // Assert
        result.andExpect(status().isUnauthorized());
    }
}
