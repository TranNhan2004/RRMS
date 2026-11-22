package com.roomrental.webapi.repository;

import static org.assertj.core.api.Assertions.assertThat;

import org.junit.jupiter.api.Test;
import org.springframework.core.io.support.SpringFactoriesLoader;

import com.roomrental.webapi.abstraction.repository.ProvinceRepositoryCustom;
import com.roomrental.webapi.abstraction.repository.WardRepositoryCustom;

class AddressRepositoryFragmentDiscoveryTests {
    @Test
    void load_ShouldFindBothImplementations_WhenFragmentsAreRegistered() {
        // Arrange
        var loader = SpringFactoriesLoader.forDefaultResourceLocation();

        // Act
        var provinces = loader.load(ProvinceRepositoryCustom.class);
        var wards = loader.load(WardRepositoryCustom.class);

        // Assert
        assertThat(provinces).singleElement().isInstanceOf(ProvinceRepositoryCustomImpl.class);
        assertThat(wards).singleElement().isInstanceOf(WardRepositoryCustomImpl.class);
    }
}
