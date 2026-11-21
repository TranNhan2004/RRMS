package com.roomrental.webapi.model.common;

import java.util.List;
import com.roomrental.webapi.enums.common.FilterGroupLogic;
import lombok.AllArgsConstructor;
import lombok.Getter;

@Getter
@AllArgsConstructor
public class FilterGroup {
    private final FilterGroupLogic logic;
    private final List<Node> children;

    @Getter
    @AllArgsConstructor
    public static class Node {
        private final Integer criterionIndex;
        private final FilterGroup group;

        public static Node criterion(int index) { return new Node(index, null); }
        public static Node group(FilterGroup group) { return new Node(null, group); }
    }
}
