package com.roomrental.webapi.security;

import java.util.List;
import java.util.UUID;

import org.springframework.security.authentication.AnonymousAuthenticationToken;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.oauth2.jwt.Jwt;
import org.springframework.security.oauth2.server.resource.authentication.JwtAuthenticationToken;
import org.springframework.stereotype.Component;

import com.roomrental.webapi.abstraction.security.CurrentUserContext;

@Component
public class SpringSecurityCurrentUserContext implements CurrentUserContext {

    @Override
    public UUID getUserId() {
        Jwt jwt = getJwt();
        return UUID.fromString(jwt.getClaimAsString("userId"));
    }

    @Override
    public String getUsername() {
        return getJwt().getClaimAsString("username");
    }

    @Override
    public List<String> getRoles() {
        return getJwt().getClaimAsStringList("roles");
    }

    @Override
    public boolean isAuthenticated() {
        Authentication authentication = SecurityContextHolder.getContext().getAuthentication();

        return authentication != null
                && authentication.isAuthenticated()
                && !(authentication instanceof AnonymousAuthenticationToken);
    }

    private Jwt getJwt() {
        Authentication authentication = SecurityContextHolder.getContext().getAuthentication();

        if (!(authentication instanceof JwtAuthenticationToken token)) {
            throw new IllegalStateException("No authenticated JWT user found");
        }

        return token.getToken();
    }
}
