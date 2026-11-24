CREATE TABLE `user_credentials` (
    `id`            CHAR(36)       NOT NULL PRIMARY KEY,
    `password_hash` VARCHAR(255)   NOT NULL,
    `user_id`       CHAR(36)       NOT NULL,
    `is_deleted`    BOOLEAN        NOT NULL,
    `created_at`    TIMESTAMP(6)   NULL,
    `created_by_id` CHAR(36)       NULL,
    `updated_at`    TIMESTAMP(6)   NULL,
    `updated_by_id` CHAR(36)       NULL
);

ALTER TABLE `user_credentials`
ADD CONSTRAINT `uq_user_credentials_user_id`
UNIQUE (`user_id`);

ALTER TABLE `user_credentials`
ADD CONSTRAINT `fk_user_credentials_user_id`
FOREIGN KEY (`user_id`) REFERENCES `users` (`id`);

ALTER TABLE `user_credentials`
ADD CONSTRAINT `fk_user_credentials_created_by_id`
FOREIGN KEY (`created_by_id`) REFERENCES `users` (`id`);

ALTER TABLE `user_credentials`
ADD CONSTRAINT `fk_user_credentials_updated_by_id`
FOREIGN KEY (`updated_by_id`) REFERENCES `users` (`id`);

CREATE TABLE `user_identities` (
    `id`               CHAR(36)       NOT NULL PRIMARY KEY,
    `provider`         VARCHAR(50)    NOT NULL,
    `provider_subject` VARCHAR(255)   CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
    `user_id`          CHAR(36)       NOT NULL,
    `is_deleted`       BOOLEAN        NOT NULL,
    `created_at`       TIMESTAMP(6)   NULL,
    `created_by_id`    CHAR(36)       NULL,
    `updated_at`       TIMESTAMP(6)   NULL,
    `updated_by_id`    CHAR(36)       NULL
);

ALTER TABLE `user_identities`
ADD CONSTRAINT `uq_user_identities_user_id`
UNIQUE (`user_id`);

ALTER TABLE `user_identities`
ADD CONSTRAINT `uq_user_identities_provider_provider_subject`
UNIQUE (`provider`, `provider_subject`);

ALTER TABLE `user_identities`
ADD CONSTRAINT `fk_user_identities_user_id`
FOREIGN KEY (`user_id`) REFERENCES `users` (`id`);

ALTER TABLE `user_identities`
ADD CONSTRAINT `fk_user_identities_created_by_id`
FOREIGN KEY (`created_by_id`) REFERENCES `users` (`id`);

ALTER TABLE `user_identities`
ADD CONSTRAINT `fk_user_identities_updated_by_id`
FOREIGN KEY (`updated_by_id`) REFERENCES `users` (`id`);

ALTER TABLE `users`
DROP COLUMN `password_hash`;