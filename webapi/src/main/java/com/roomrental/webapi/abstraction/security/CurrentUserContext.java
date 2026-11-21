package com.roomrental.webapi.abstraction.security;

import java.util.List;
import java.util.UUID;

public interface CurrentUserContext {
    UUID getUserId();

    String getUsername();

    List<String> getRoles();

    boolean isAuthenticated();
}
