BEGIN;
INSERT INTO `roles` (`id`, `code`, `display_name`) VALUES
('01a2092e-e6e1-77ff-8c59-13d95a93d4ff', 'ADMIN', 'Admin'),
('01a2092e-e6e2-793f-bbf6-93baec4bc276', 'LANDLORD', 'Landlord'),
('01a2092e-e6e2-7479-bf72-b1d57e686c49', 'TENANT', 'Tenant');
COMMIT;
