package com.roomrental.webapi.dto.v1.address;

import com.roomrental.webapi.dto.common.FilterRequest;
import com.roomrental.webapi.model.address.GetFilteredWardResponseItemModel;
import com.roomrental.webapi.model.common.FilterFieldMap;
import jakarta.validation.constraints.NotNull;
import java.util.UUID;
import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class GetFilteredWardsRequest extends FilterRequest<GetFilteredWardResponseItemModel> {
    @NotNull
    private UUID provinceId;

    @Override
    public FilterFieldMap getFilterFieldMap() {
        return new FilterFieldMap().map("name", "name", String.class);
    }
}
