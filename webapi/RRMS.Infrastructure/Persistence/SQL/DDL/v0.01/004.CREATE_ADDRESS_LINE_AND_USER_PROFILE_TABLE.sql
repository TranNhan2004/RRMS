CREATE TABLE `address_lines` (
    `id`            CHAR(36)        NOT NULL PRIMARY KEY,
    `details`       VARCHAR(1024)   NOT NULL,
    `ward_id`       CHAR(36)        NOT NULL,
    `is_deleted`    BOOLEAN         NOT NULL,
    `created_at`    TIMESTAMP(6)    NULL,
    `created_by_id` CHAR(36)        NULL,
    `updated_at`    TIMESTAMP(6)    NULL,
    `updated_by_id` CHAR(36)        NULL
);

ALTER TABLE `address_lines`
ADD CONSTRAINT `fk_address_lines_ward_id` 
FOREIGN KEY (`ward_id`) REFERENCES `wards` (`id`);

ALTER TABLE `address_lines`
ADD CONSTRAINT `fk_address_lines_created_by_id` 
FOREIGN KEY (`created_by_id`) REFERENCES `users` (`id`);

ALTER TABLE `address_lines`
ADD CONSTRAINT `fk_address_lines_updated_by_id` 
FOREIGN KEY (`updated_by_id`) REFERENCES `users` (`id`);

CREATE TABLE `user_profiles` (
    `id`                 CHAR(36)        NOT NULL PRIMARY KEY,
    `national_id_number` VARCHAR(20)     NULL,
    `first_name`         VARCHAR(255)    NULL,
    `last_name`          VARCHAR(255)    NULL,
    `phone_number`       VARCHAR(20)     NULL,
    `birth_date`         DATE            NULL,
    `sex`                VARCHAR(50)     NULL,
    `address_updated_at` TIMESTAMP(6)    NULL,
    `address_line_id`    CHAR(36)        NULL,
    `user_id`            CHAR(36)        NOT NULL,
    `is_deleted`         BOOLEAN         NOT NULL,
    `created_at`         TIMESTAMP(6)    NULL,
    `created_by_id`      CHAR(36)        NULL,
    `updated_at`         TIMESTAMP(6)    NULL,
    `updated_by_id`      CHAR(36)        NULL
);

ALTER TABLE `user_profiles`
ADD CONSTRAINT `uq_user_profiles_user_id` 
UNIQUE (`user_id`);

ALTER TABLE `user_profiles`
ADD CONSTRAINT `fk_user_profiles_address_line_id` 
FOREIGN KEY (`address_line_id`) REFERENCES `address_lines` (`id`);

ALTER TABLE `user_profiles`
ADD CONSTRAINT `fk_user_profiles_user_id` 
FOREIGN KEY (`user_id`) REFERENCES `users` (`id`);

ALTER TABLE `user_profiles`
ADD CONSTRAINT `fk_user_profiles_created_by_id` 
FOREIGN KEY (`created_by_id`) REFERENCES `users` (`id`);

ALTER TABLE `user_profiles`
ADD CONSTRAINT `fk_user_profiles_updated_by_id` 
FOREIGN KEY (`updated_by_id`) REFERENCES `users` (`id`);
