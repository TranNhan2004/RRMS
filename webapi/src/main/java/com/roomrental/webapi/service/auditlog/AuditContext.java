package com.roomrental.webapi.service.auditlog;

import java.util.regex.Pattern;

import org.slf4j.MDC;
import org.springframework.security.authentication.AnonymousAuthenticationToken;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.oauth2.server.resource.authentication.JwtAuthenticationToken;
import org.springframework.web.context.request.RequestContextHolder;
import org.springframework.web.context.request.ServletRequestAttributes;

import jakarta.servlet.http.HttpServletRequest;

/**
 * Values are captured on the business thread; forwarded IP headers are not
 * trusted.
 */
public record AuditContext(
        String traceId,
        String requestId,
        String clientActionId,
        String clientRequestId,
        String ipAddress,
        String actorId) {

    private static final Pattern SAFE_ID = Pattern.compile("[A-Za-z0-9._:-]+");

    public static AuditContext capture() {
        Authentication auth = SecurityContextHolder.getContext().getAuthentication();
        String actor = null;
        if (auth != null && auth.isAuthenticated() && !(auth instanceof AnonymousAuthenticationToken)) {
            String identity = auth instanceof JwtAuthenticationToken jwt
                    && jwt.getToken().getClaimAsString("userId") != null
                            ? jwt.getToken().getClaimAsString("userId")
                            : auth.getName();
            if (identity != null && !identity.isBlank() && identity.length() <= 255
                    && identity.chars().noneMatch(Character::isISOControl))
                actor = identity;
        }
        if (actor == null)
            actor = "system";
        HttpServletRequest request = RequestContextHolder
                .getRequestAttributes() instanceof ServletRequestAttributes attrs
                        ? attrs.getRequest()
                        : null;
        return new AuditContext(
                valid(MDC.get("traceId"), 64),
                header(request, "X-Request-Id"),
                header(request, "X-Client-Action-Id"),
                header(request, "X-Client-Request-Id"),
                request == null ? null : valid(request.getRemoteAddr(), 45),
                actor);
    }

    private static String header(HttpServletRequest request, String name) {
        return request == null ? null : valid(request.getHeader(name), 255);
    }

    private static String valid(String value, int max) {
        return value != null && value.length() <= max && SAFE_ID.matcher(value).matches() ? value : null;
    }
}
