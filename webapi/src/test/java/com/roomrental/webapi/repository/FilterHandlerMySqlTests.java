package com.roomrental.webapi.repository;

import static org.assertj.core.api.Assertions.assertThat;

import java.util.List;
import java.util.UUID;

import org.hibernate.Session;
import org.hibernate.SessionFactory;
import org.hibernate.cfg.Configuration;
import org.junit.jupiter.api.AfterAll;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.condition.EnabledIfEnvironmentVariable;

import com.roomrental.webapi.abstraction.repository.FilterQuery;
import com.roomrental.webapi.dto.common.FilterCriterionRequest;
import com.roomrental.webapi.dto.common.FilterRequest;
import com.roomrental.webapi.dto.common.SortFieldRequest;
import com.roomrental.webapi.entity.Province;
import com.roomrental.webapi.entity.Ward;
import com.roomrental.webapi.model.common.FilterFieldMap;
import com.roomrental.webapi.model.common.FilterRequestModel;
import com.roomrental.webapi.utils.FilterRequestParser;

import jakarta.persistence.criteria.CriteriaBuilder;
import jakarta.persistence.criteria.CriteriaQuery;
import jakarta.persistence.criteria.JoinType;
import jakarta.persistence.criteria.Root;
import jakarta.persistence.criteria.Selection;

/** Uses a pre-provisioned test database; all test inserts are rolled back. */
@EnabledIfEnvironmentVariable(named = "ROOM_RENTAL_FILTER_TEST_JDBC_URL", matches = ".+")
public class FilterHandlerMySqlTests {
    public record WardRow(String name, String provinceName) {
    }

    private static SessionFactory factory;

    public static class WardFilterRequest extends FilterRequest<WardRow> {
        @Override
        public FilterFieldMap getFilterFieldMap() {
            return new FilterFieldMap().map("name", "name", String.class)
                    .map("provinceName", "province.name", String.class);
        }
    }

    public static final class WardFilterRequestModel extends FilterRequestModel<WardRow> {
        WardFilterRequestModel(FilterRequestModel<WardRow> request) {
            super(request);
        }
    }

    private static final class WardRequestMapper {
        static WardFilterRequestModel toModel(WardFilterRequest request) {
            return new WardFilterRequestModel(FilterRequestParser.parse(request));
        }
    }

    private static final class WardQueryRepository {
        FilterQuery<Ward, WardRow> findWardQuery(UUID provinceId) {
            return new FilterQuery<>() {
                @Override
                public Class<Ward> entityType() {
                    return Ward.class;
                }

                @Override
                public void configure(CriteriaBuilder cb, CriteriaQuery<?> query, Root<Ward> root) {
                    query.where(cb.equal(root.get("province").get("id"), provinceId));
                }

                @Override
                public Selection<WardRow> select(CriteriaBuilder cb, Root<Ward> root, Class<WardRow> type) {
                    var province = root.join("province", JoinType.LEFT);
                    return cb.construct(type, root.get("name"), province.get("name"));
                }
            };
        }
    }

    @BeforeAll
    static void setUpFactory() {
        factory = new Configuration()
                .addAnnotatedClass(Province.class)
                .addAnnotatedClass(Ward.class)
                .setProperty("hibernate.connection.driver_class", "com.mysql.cj.jdbc.Driver")
                .setProperty("hibernate.connection.url", System.getenv("ROOM_RENTAL_FILTER_TEST_JDBC_URL"))
                .setProperty("hibernate.connection.username",
                        System.getenv().getOrDefault("ROOM_RENTAL_FILTER_TEST_DB_USER", ""))
                .setProperty("hibernate.connection.password",
                        System.getenv().getOrDefault("ROOM_RENTAL_FILTER_TEST_DB_PASSWORD", ""))
                .setProperty("hibernate.hbm2ddl.auto", "none")
                .buildSessionFactory();
    }

    @AfterAll
    static void closeFactory() {
        if (factory != null)
            factory.close();
    }

    @Test
    void applyFilter_ShouldFilterProjectedNestedFieldsAndCountBaseRows_WhenQueryIsExecuted() {
        // Arrange
        try (Session session = factory.openSession()) {
            var transaction = session.beginTransaction();
            try {
                String suffix = UUID.randomUUID().toString().substring(0, 8);
                Province selected = province("A" + suffix);
                Province other = province("B" + suffix);
                session.persist(selected);
                session.persist(other);
                session.persist(ward("A" + suffix, selected));
                session.persist(ward("B" + suffix, selected));
                session.persist(ward("C" + suffix, other));
                session.flush();
                var request = new WardFilterRequest();
                request.setCurrentPage(1);
                request.setItemsPerPage(1);
                request.setFilterCriteria(List
                        .of(new FilterCriterionRequest("provinceName", "EQUALS", List.of(selected.getName()), null)));
                request.setSortFields(List.of(new SortFieldRequest("name", false)));
                var model = WardRequestMapper.toModel(request);
                var definition = new WardQueryRepository().findWardQuery(selected.getId());
                var handler = new FilterHandler(session);

                // Act
                var result = handler.applyFilter(model, definition, WardRow.class);

                // Assert
                assertThat(result.getItems()).containsExactly(new WardRow("B" + suffix, selected.getName()));
                assertThat(result.getTotalItems()).isEqualTo(2);
                assertThat(result.getTotalPages()).isEqualTo(2);
                assertThat(result.isHasNextPage()).isTrue();
            } finally {
                transaction.rollback();
            }
        }
    }

    @Test
    void applyFilter_ShouldCombineGroupedCriteriaAndGlobalSearch_WhenMultipleRowsMatch() {
        // Arrange
        try (Session session = factory.openSession()) {
            var transaction = session.beginTransaction();
            try {
                String suffix = UUID.randomUUID().toString().substring(0, 8);
                Province selected = province("P" + suffix);
                session.persist(selected);
                session.persist(ward("A" + suffix, selected));
                session.persist(ward("B" + suffix, selected));
                session.persist(ward("C" + suffix, selected));
                session.flush();
                var request = new WardFilterRequest();
                request.setCurrentPage(1);
                request.setItemsPerPage(10);
                request.setSearchGlobalText(selected.getName());
                request.setGroupPattern("{0} OR {1}");
                request.setFilterCriteria(List.of(
                        new FilterCriterionRequest("name", "STARTS_WITH", List.of("A"), null),
                        new FilterCriterionRequest("name", "STARTS_WITH", List.of("B"), null)));
                request.setSortFields(List.of(new SortFieldRequest("name", true)));
                var model = WardRequestMapper.toModel(request);
                var definition = new WardQueryRepository().findWardQuery(selected.getId());
                var handler = new FilterHandler(session);

                // Act
                var result = handler.applyFilter(model, definition, WardRow.class);

                // Assert
                assertThat(result.getItems()).containsExactly(
                        new WardRow("A" + suffix, selected.getName()),
                        new WardRow("B" + suffix, selected.getName()));
                assertThat(result.getTotalItems()).isEqualTo(2);
                assertThat(result.getTotalPages()).isEqualTo(1);
            } finally {
                transaction.rollback();
            }
        }
    }

    private static Province province(String suffix) {
        var province = new Province();
        province.setProvinceCode(suffix);
        province.setCode(suffix);
        province.setName("Province " + suffix);
        province.setShortName("P " + suffix);
        province.setPlaceType("province");
        province.setCountry("VN");
        return province;
    }

    private static Ward ward(String suffix, Province province) {
        var ward = new Ward();
        ward.setWardCode(suffix);
        ward.setName(suffix);
        ward.setProvince(province);
        return ward;
    }
}
