package com.roomrental.webapi.controller;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import java.util.List;
import java.util.UUID;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.MediaType;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;

import com.roomrental.webapi.abstraction.service.address.AddressQueryService;
import com.roomrental.webapi.controller.v1.AddressController;
import com.roomrental.webapi.middleware.GlobalExceptionHandler;
import com.roomrental.webapi.model.address.GetFilteredProvincesRequestModel;
import com.roomrental.webapi.model.address.GetFilteredWardsRequestModel;
import com.roomrental.webapi.model.address.GetFilteredProvinceResponseItemModel;
import com.roomrental.webapi.model.address.GetFilteredWardResponseItemModel;
import com.roomrental.webapi.model.common.FilterResponseModel;

@ExtendWith(MockitoExtension.class)
class AddressControllerTests {
    @Mock
    private AddressQueryService service;

    @Test
    void getFilteredProvinces_ShouldMapRequestAndResponse_WhenValid() throws Exception {
        // Arrange
        var id = UUID.randomUUID();
        var province = new GetFilteredProvinceResponseItemModel(id, "01", "Hà Nội", "Hà Nội", "HNI", "City", "VN");
        when(service.getFilteredProvinces(any()))
                .thenReturn(new FilterResponseModel<>(List.of(province), 2, 1, 3, 3, true, true));
        var mvc = MockMvcBuilders.standaloneSetup(new AddressController(service))
                .setControllerAdvice(new GlobalExceptionHandler()).build();
        var captor = ArgumentCaptor.forClass(GetFilteredProvincesRequestModel.class);

        // Act
        var response = mvc.perform(post("/api/v1/addresses/provinces").contentType(MediaType.APPLICATION_JSON)
                .content("{\"searchGlobalText\":\"Hà Nội\",\"currentPage\":2,\"itemsPerPage\":1}"))
                .andExpect(status().isOk()).andReturn().getResponse().getContentAsString();

        // Assert
        verify(service).getFilteredProvinces(captor.capture());
        assertThat(captor.getValue().getSearchGlobalText()).isEqualTo("Hà Nội");
        assertThat(captor.getValue().getCurrentPage()).isEqualTo(2);
        assertThat(response).contains(id.toString(), "Hà Nội", "\"totalItems\":3", "\"hasNextPage\":true");
    }

    @Test
    void getFilteredWards_ShouldMapProvinceId_WhenValid() throws Exception {
        // Arrange
        var provinceId = UUID.randomUUID();
        var id = UUID.randomUUID();
        var ward = new GetFilteredWardResponseItemModel(id, "00004", "Ba Đình", provinceId);
        when(service.getFilteredWards(any()))
                .thenReturn(new FilterResponseModel<>(List.of(ward), 1, 10, 1, 1, false, false));
        var mvc = MockMvcBuilders.standaloneSetup(new AddressController(service))
                .setControllerAdvice(new GlobalExceptionHandler()).build();
        var captor = ArgumentCaptor.forClass(GetFilteredWardsRequestModel.class);

        // Act
        var response = mvc.perform(post("/api/v1/addresses/wards").contentType(MediaType.APPLICATION_JSON)
                .content("{\"provinceId\":\"" + provinceId + "\",\"searchGlobalText\":\"Ba\"}"))
                .andExpect(status().isOk()).andReturn().getResponse().getContentAsString();

        // Assert
        verify(service).getFilteredWards(captor.capture());
        assertThat(captor.getValue().getProvinceId()).isEqualTo(provinceId);
        assertThat(captor.getValue().getSearchGlobalText()).isEqualTo("Ba");
        assertThat(response).contains(id.toString(), provinceId.toString(), "Ba Đình");
    }

    @Test
    void getFilteredWards_ShouldRejectRequest_WhenProvinceIdIsMissing() throws Exception {
        // Arrange
        var mvc = MockMvcBuilders.standaloneSetup(new AddressController(service))
                .setControllerAdvice(new GlobalExceptionHandler()).build();

        // Act
        var response = mvc
                .perform(post("/api/v1/addresses/wards").contentType(MediaType.APPLICATION_JSON).content("{}"))
                .andReturn().getResponse();

        // Assert
        assertThat(response.getStatus()).isEqualTo(400);
        verify(service, never()).getFilteredWards(any());
    }
}
