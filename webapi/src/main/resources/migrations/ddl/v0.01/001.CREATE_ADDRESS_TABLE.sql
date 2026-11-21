CREATE TABLE `provinces` (
    `id`            CHAR(36)        NOT NULL PRIMARY KEY,
    `province_code` VARCHAR(10)     NOT NULL,
    `name`          VARCHAR(255)    NOT NULL,
    `short_name`    VARCHAR(255)    NOT NULL,
    `code`          VARCHAR(10)     NOT NULL,
    `place_type`    VARCHAR(255)    NOT NULL,
    `country`       VARCHAR(10)     NOT NULL
);

ALTER TABLE `provinces` 
ADD CONSTRAINT `uq_provinces_name`
UNIQUE (`name`);

CREATE TABLE `wards` (
    `id`            CHAR(36)        NOT NULL PRIMARY KEY,
    `ward_code`     VARCHAR(10)     NOT NULL,
    `name`          VARCHAR(255)    NOT NULL,
    `province_id`   CHAR(36)        NOT NULL
);

ALTER TABLE `wards` 
ADD CONSTRAINT `fk_wards_province_id`
FOREIGN KEY (`province_id`) REFERENCES `provinces` (`id`);
