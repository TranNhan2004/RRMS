-- Run using the MySQL client, which supports DELIMITER directives.
DELIMITER $$
CREATE TRIGGER `trg_users_user_name_set_once`
BEFORE UPDATE ON `users`
FOR EACH ROW
BEGIN
    IF OLD.`user_name` IS NOT NULL
       AND NOT (BINARY NEW.`user_name` <=> BINARY OLD.`user_name`) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Username can only be set once';
    END IF;
END$$
DELIMITER ;
