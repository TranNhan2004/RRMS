CREATE TABLE `users` (
    `id`                CHAR(36)        NOT NULL PRIMARY KEY,
    `user_name`         VARCHAR(64)     NOT NULL,
    `email`             VARCHAR(255)    NOT NULL,
    `password_hash`     VARCHAR(255)    NOT NULL,
    `status`            VARCHAR(50)     NOT NULL,
    `is_email_verified` BOOLEAN         NOT NULL,
    `is_deleted`    BOOLEAN         NOT NULL,
    `created_at`    TIMESTAMP(6)    NULL,
    `created_by_id` CHAR(36)        NULL,
    `updated_at`    TIMESTAMP(6)    NULL,
    `updated_by_id` CHAR(36)        NULL
);

ALTER TABLE `users`
ADD CONSTRAINT `uq_users_user_name` 
UNIQUE (`user_name`);

ALTER TABLE `users`
ADD CONSTRAINT `uq_users_email` 
UNIQUE (`email`);

ALTER TABLE `users`
ADD CONSTRAINT `fk_users_created_by_id` 
FOREIGN KEY (`created_by_id`) REFERENCES `users` (`id`);

ALTER TABLE `users`
ADD CONSTRAINT `fk_users_updated_by_id` 
FOREIGN KEY (`updated_by_id`) REFERENCES `users` (`id`);


CREATE TABLE `roles` (
    `id`           CHAR(36)        NOT NULL PRIMARY KEY,
    `code`         VARCHAR(64)     NOT NULL,
    `display_name` VARCHAR(100)    NOT NULL
);

ALTER TABLE `roles`
ADD CONSTRAINT `uq_roles_code` 
UNIQUE (`code`);

ALTER TABLE `roles`
ADD CONSTRAINT `uq_roles_display_name` 
UNIQUE (`display_name`);


CREATE TABLE `user_roles` (
    `id`            CHAR(36)        NOT NULL PRIMARY KEY,
    `user_id`       CHAR(36)        NOT NULL,
    `role_id`       CHAR(36)        NOT NULL,
    `is_deleted`    BOOLEAN         NOT NULL,
    `created_at`    TIMESTAMP(6)    NULL,
    `created_by_id` CHAR(36)        NULL,
    `updated_at`    TIMESTAMP(6)    NULL,
    `updated_by_id` CHAR(36)        NULL
);

ALTER TABLE `user_roles`
ADD CONSTRAINT `fk_user_roles_user_id` 
FOREIGN KEY (`user_id`) REFERENCES `users` (`id`);

ALTER TABLE `user_roles`
ADD CONSTRAINT `fk_user_roles_role_id` 
FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`);

ALTER TABLE `user_roles`
ADD CONSTRAINT `fk_user_roles_created_by_id` 
FOREIGN KEY (`created_by_id`) REFERENCES `users` (`id`);

ALTER TABLE `user_roles`
ADD CONSTRAINT `fk_user_roles_updated_by_id` 
FOREIGN KEY (`updated_by_id`) REFERENCES `users` (`id`);
