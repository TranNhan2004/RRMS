package com.roomrental.webapi.service.address;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

import java.util.List;
import java.util.UUID;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import com.roomrental.webapi.abstraction.repository.FilterQuery;
import com.roomrental.webapi.abstraction.repository.ProvinceRepository;
import com.roomrental.webapi.abstraction.repository.WardRepository;
import com.roomrental.webapi.dto.v1.address.GetFilteredProvincesRequest;
import com.roomrental.webapi.dto.v1.address.GetFilteredWardsRequest;
import com.roomrental.webapi.entity.Province;
import com.roomrental.webapi.entity.Ward;
import com.roomrental.webapi.mapper.AddressMapper;
import com.roomrental.webapi.model.address.GetFilteredProvinceResponseItemModel;
import com.roomrental.webapi.model.address.GetFilteredWardResponseItemModel;
import com.roomrental.webapi.model.common.FilterResponseModel;
import com.roomrental.webapi.repository.FilterHandler;

@ExtendWith(MockitoExtension.class)
class AddressQueryServiceTests {
    @Mock private ProvinceRepository provinceRepository;
    @Mock private WardRepository wardRepository;
    @Mock private FilterHandler filterHandler;
    @Mock private FilterQuery<Province, GetFilteredProvinceResponseItemModel> provinceQuery;
    @Mock private FilterQuery<Ward, GetFilteredWardResponseItemModel> wardQuery;
    @InjectMocks private AddressQueryServiceImpl service;

    @Test
    void getFilteredProvinces_ShouldApplyFilter_WhenRequested() {
        // Arrange
        var dto = new GetFilteredProvincesRequest();
        dto.setSearchGlobalText("Hà Nội");
        var request = AddressMapper.toModel(dto);
        var expected = new FilterResponseModel<GetFilteredProvinceResponseItemModel>(List.of(), 1, 10, 0, 0, false, false);
        when(provinceRepository.findFilteredProvincesQuery()).thenReturn(provinceQuery);
        when(filterHandler.applyFilter(request, provinceQuery, GetFilteredProvinceResponseItemModel.class)).thenReturn(expected);

        // Act
        var result = service.getFilteredProvinces(request);

        // Assert
        assertThat(result).isSameAs(expected);
        verify(provinceRepository).findFilteredProvincesQuery();
        verify(filterHandler).applyFilter(request, provinceQuery, GetFilteredProvinceResponseItemModel.class);
    }

    @Test
    void getFilteredWards_ShouldScopeQueryToProvince_WhenRequested() {
        // Arrange
        var provinceId = UUID.randomUUID();
        var dto = new GetFilteredWardsRequest();
        dto.setProvinceId(provinceId);
        var request = AddressMapper.toModel(dto);
        var expected = new FilterResponseModel<GetFilteredWardResponseItemModel>(List.of(), 1, 10, 0, 0, false, false);
        when(wardRepository.findFilteredWardsQuery(provinceId)).thenReturn(wardQuery);
        when(filterHandler.applyFilter(request, wardQuery, GetFilteredWardResponseItemModel.class)).thenReturn(expected);

        // Act
        var result = service.getFilteredWards(request);

        // Assert
        assertThat(result).isSameAs(expected);
        verify(wardRepository).findFilteredWardsQuery(provinceId);
        verify(filterHandler).applyFilter(request, wardQuery, GetFilteredWardResponseItemModel.class);
    }
}
