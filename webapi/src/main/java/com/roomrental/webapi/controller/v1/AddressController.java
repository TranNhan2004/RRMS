package com.roomrental.webapi.controller.v1;

import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import com.roomrental.webapi.abstraction.service.address.AddressQueryService;
import com.roomrental.webapi.dto.v1.address.GetFilteredProvincesRequest;
import com.roomrental.webapi.dto.v1.address.GetFilteredProvincesResponse;
import com.roomrental.webapi.dto.v1.address.GetFilteredWardsRequest;
import com.roomrental.webapi.dto.v1.address.GetFilteredWardsResponse;
import com.roomrental.webapi.mapper.AddressMapper;

import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;

@RestController
@RequiredArgsConstructor
@RequestMapping("/api/v1/addresses")
public class AddressController {
    private final AddressQueryService addressQueryService;

    @PostMapping("/provinces")
    @ApiResponse(responseCode = "200", content = @Content(schema = @Schema(implementation = GetFilteredProvincesResponse.class)))
    public ResponseEntity<?> getFilteredProvinces(@Valid @RequestBody GetFilteredProvincesRequest request) {
        var model = AddressMapper.toModel(request);
        var result = addressQueryService.getFilteredProvinces(model);
        return ResponseEntity.ok(AddressMapper.toProvincesResponse(result));
    }

    @PostMapping("/wards")
    @ApiResponse(responseCode = "200", content = @Content(schema = @Schema(implementation = GetFilteredWardsResponse.class)))
    public ResponseEntity<?> getFilteredWards(@Valid @RequestBody GetFilteredWardsRequest request) {
        var model = AddressMapper.toModel(request);
        var result = addressQueryService.getFilteredWards(model);
        return ResponseEntity.ok(AddressMapper.toWardsResponse(result));
    }
}
