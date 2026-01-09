-- Delete existing data
DELETE FROM Transformers;
DELETE FROM FieldMappings;
DELETE FROM StaticValues;
DELETE FROM SoapConfigurations;
DELETE FROM IntegrationMappings;

-- Integration 1: E-Commerce Order Fulfillment
INSERT INTO IntegrationMappings (Id, Name, Endpoint, SourceType, DestinationType, DestinationUrl, DispatchFor, IsActive, Version, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
VALUES ('11111111-1111-1111-1111-111111111111', 'E-Commerce Order Fulfillment', '/api/fulfillment/orders', 'JSON', 'SOAP', 'https://warehouse-system.demo.local/soap/v2/fulfillment', NULL, 1, 1, datetime('now'), datetime('now'), NULL, NULL);

INSERT INTO FieldMappings (Id, IntegrationMappingId, Source, Destination, "Order")
VALUES
  ('11111111-1111-1111-1111-111111111001', '11111111-1111-1111-1111-111111111111', '$.orderId', '/envelope/body/orderNumber', 1),
  ('11111111-1111-1111-1111-111111111002', '11111111-1111-1111-1111-111111111111', '$.customer.email', '/envelope/body/customerEmail', 2),
  ('11111111-1111-1111-1111-111111111003', '11111111-1111-1111-1111-111111111111', '$.customer.name', '/envelope/body/customerName', 3),
  ('11111111-1111-1111-1111-111111111004', '11111111-1111-1111-1111-111111111111', '$.shippingAddress.street', '/envelope/body/address/street', 4),
  ('11111111-1111-1111-1111-111111111005', '11111111-1111-1111-1111-111111111111', '$.shippingAddress.city', '/envelope/body/address/city', 5),
  ('11111111-1111-1111-1111-111111111006', '11111111-1111-1111-1111-111111111111', '$.shippingAddress.postalCode', '/envelope/body/address/zipCode', 6),
  ('11111111-1111-1111-1111-111111111007', '11111111-1111-1111-1111-111111111111', '$.items[*].sku', '/envelope/body/lineItems/item/productCode', 7),
  ('11111111-1111-1111-1111-111111111008', '11111111-1111-1111-1111-111111111111', '$.items[*].quantity', '/envelope/body/lineItems/item/qty', 8),
  ('11111111-1111-1111-1111-111111111009', '11111111-1111-1111-1111-111111111111', '$$.fulfillmentPriority', '/envelope/body/priority', 9);

INSERT INTO StaticValues (Id, IntegrationMappingId, Key, Value, IsGlobal)
VALUES
  ('11111111-1111-1111-1111-111111111101', '11111111-1111-1111-1111-111111111111', 'fulfillmentPriority', 'STANDARD', 0),
  ('11111111-1111-1111-1111-111111111102', '11111111-1111-1111-1111-111111111111', 'warehouseId', 'WH-001', 0),
  ('11111111-1111-1111-1111-111111111103', '11111111-1111-1111-1111-111111111111', 'processingStatus', 'PENDING', 0);

-- Integration 2: Supplier Data Sync
INSERT INTO IntegrationMappings (Id, Name, Endpoint, SourceType, DestinationType, DestinationUrl, DispatchFor, IsActive, Version, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
VALUES ('22222222-2222-2222-2222-222222222222', 'Supplier Data Sync', '/api/suppliers/sync', 'JSON', 'SOAP', 'https://procurement-api.demo.local/soap/suppliers', NULL, 1, 1, datetime('now'), datetime('now'), NULL, NULL);

INSERT INTO FieldMappings (Id, IntegrationMappingId, Source, Destination, "Order")
VALUES
  ('22222222-2222-2222-2222-222222222001', '22222222-2222-2222-2222-222222222222', '$.supplierId', '/envelope/body/vendorCode', 1),
  ('22222222-2222-2222-2222-222222222002', '22222222-2222-2222-2222-222222222222', '$.companyName', '/envelope/body/businessName', 2),
  ('22222222-2222-2222-2222-222222222003', '22222222-2222-2222-2222-222222222222', '$.taxId', '/envelope/body/taxIdentifier', 3),
  ('22222222-2222-2222-2222-222222222004', '22222222-2222-2222-2222-222222222222', '$.contact.email', '/envelope/body/primaryEmail', 4),
  ('22222222-2222-2222-2222-222222222005', '22222222-2222-2222-2222-222222222222', '$.contact.phone', '/envelope/body/phoneNumber', 5),
  ('22222222-2222-2222-2222-222222222006', '22222222-2222-2222-2222-222222222222', '$.paymentTerms', '/envelope/body/terms', 6),
  ('22222222-2222-2222-2222-222222222007', '22222222-2222-2222-2222-222222222222', '$$.accountStatus', '/envelope/body/status', 7);

INSERT INTO StaticValues (Id, IntegrationMappingId, Key, Value, IsGlobal)
VALUES
  ('22222222-2222-2222-2222-222222222101', '22222222-2222-2222-2222-222222222222', 'accountStatus', 'ACTIVE', 0),
  ('22222222-2222-2222-2222-222222222102', '22222222-2222-2222-2222-222222222222', 'vendorType', 'STANDARD', 0),
  ('22222222-2222-2222-2222-222222222103', '22222222-2222-2222-2222-222222222222', 'creditRating', 'A', 0);

-- Integration 3: Product Catalog Integration
INSERT INTO IntegrationMappings (Id, Name, Endpoint, SourceType, DestinationType, DestinationUrl, DispatchFor, IsActive, Version, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
VALUES ('33333333-3333-3333-3333-333333333333', 'Product Catalog Integration', '/api/catalog/products', 'XML', 'JSON', 'https://catalog-api.demo.local/v1/products', NULL, 1, 1, datetime('now'), datetime('now'), NULL, NULL);

INSERT INTO FieldMappings (Id, IntegrationMappingId, Source, Destination, "Order")
VALUES
  ('33333333-3333-3333-3333-333333333001', '33333333-3333-3333-3333-333333333333', '/products/product/sku', '$.productCode', 1),
  ('33333333-3333-3333-3333-333333333002', '33333333-3333-3333-3333-333333333333', '/products/product/name', '$.title', 2),
  ('33333333-3333-3333-3333-333333333003', '33333333-3333-3333-3333-333333333333', '/products/product/description', '$.longDescription', 3),
  ('33333333-3333-3333-3333-333333333004', '33333333-3333-3333-3333-333333333333', '/products/product/price', '$.pricing.basePrice', 4),
  ('33333333-3333-3333-3333-333333333005', '33333333-3333-3333-3333-333333333333', '/products/product/category', '$.categoryPath', 5),
  ('33333333-3333-3333-3333-333333333006', '33333333-3333-3333-3333-333333333333', '/products/product/stock', '$.inventory.quantity', 6),
  ('33333333-3333-3333-3333-333333333007', '33333333-3333-3333-3333-333333333333', '/products/product/images/image', '$.media.images[*]', 7);

INSERT INTO StaticValues (Id, IntegrationMappingId, Key, Value, IsGlobal)
VALUES
  ('33333333-3333-3333-3333-333333333101', '33333333-3333-3333-3333-333333333333', 'currency', 'USD', 0),
  ('33333333-3333-3333-3333-333333333102', '33333333-3333-3333-3333-333333333333', 'status', 'PUBLISHED', 0),
  ('33333333-3333-3333-3333-333333333103', '33333333-3333-3333-3333-333333333333', 'visibility', 'PUBLIC', 0);

-- Integration 4: Shipping Label Generator (Inactive)
INSERT INTO IntegrationMappings (Id, Name, Endpoint, SourceType, DestinationType, DestinationUrl, DispatchFor, IsActive, Version, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
VALUES ('44444444-4444-4444-4444-444444444444', 'Shipping Label Generator', '/api/shipping/labels', 'JSON', 'JSON', 'https://logistics-api.demo.local/v2/labels', NULL, 0, 1, datetime('now'), datetime('now'), NULL, NULL);

INSERT INTO FieldMappings (Id, IntegrationMappingId, Source, Destination, "Order")
VALUES
  ('44444444-4444-4444-4444-444444444001', '44444444-4444-4444-4444-444444444444', '$.shipmentId', '$.trackingNumber', 1),
  ('44444444-4444-4444-4444-444444444002', '44444444-4444-4444-4444-444444444444', '$.fromAddress', '$.origin', 2),
  ('44444444-4444-4444-4444-444444444003', '44444444-4444-4444-4444-444444444444', '$.toAddress', '$.destination', 3),
  ('44444444-4444-4444-4444-444444444004', '44444444-4444-4444-4444-444444444444', '$.package.weight', '$.packageWeight', 4),
  ('44444444-4444-4444-4444-444444444005', '44444444-4444-4444-4444-444444444444', '$.package.dimensions', '$.packageSize', 5),
  ('44444444-4444-4444-4444-444444444006', '44444444-4444-4444-4444-444444444444', '$$.serviceLevel', '$.shippingMethod', 6);

INSERT INTO StaticValues (Id, IntegrationMappingId, Key, Value, IsGlobal)
VALUES
  ('44444444-4444-4444-4444-444444444101', '44444444-4444-4444-4444-444444444444', 'serviceLevel', 'GROUND', 0),
  ('44444444-4444-4444-4444-444444444102', '44444444-4444-4444-4444-444444444444', 'carrier', 'FEDEX', 0),
  ('44444444-4444-4444-4444-444444444103', '44444444-4444-4444-4444-444444444444', 'labelFormat', 'PDF', 0);

-- Integration 5: Invoice Processing Service
INSERT INTO IntegrationMappings (Id, Name, Endpoint, SourceType, DestinationType, DestinationUrl, DispatchFor, IsActive, Version, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
VALUES ('55555555-5555-5555-5555-555555555555', 'Invoice Processing Service', '/api/accounting/invoices', 'JSON', 'SOAP', 'https://finance-system.demo.local/soap/invoices', NULL, 1, 1, datetime('now'), datetime('now'), NULL, NULL);

INSERT INTO FieldMappings (Id, IntegrationMappingId, Source, Destination, "Order")
VALUES
  ('55555555-5555-5555-5555-555555555001', '55555555-5555-5555-5555-555555555555', '$.invoiceNumber', '/envelope/body/invoiceId', 1),
  ('55555555-5555-5555-5555-555555555002', '55555555-5555-5555-5555-555555555555', '$.billTo.customerId', '/envelope/body/accountNumber', 2),
  ('55555555-5555-5555-5555-555555555003', '55555555-5555-5555-5555-555555555555', '$.billTo.name', '/envelope/body/billingName', 3),
  ('55555555-5555-5555-5555-555555555004', '55555555-5555-5555-5555-555555555555', '$.invoiceDate', '/envelope/body/issueDate', 4),
  ('55555555-5555-5555-5555-555555555005', '55555555-5555-5555-5555-555555555555', '$.dueDate', '/envelope/body/paymentDue', 5),
  ('55555555-5555-5555-5555-555555555006', '55555555-5555-5555-5555-555555555555', '$.lineItems[*].description', '/envelope/body/items/item/desc', 6),
  ('55555555-5555-5555-5555-555555555007', '55555555-5555-5555-5555-555555555555', '$.lineItems[*].amount', '/envelope/body/items/item/price', 7),
  ('55555555-5555-5555-5555-555555555008', '55555555-5555-5555-5555-555555555555', '$.totalAmount', '/envelope/body/totalDue', 8),
  ('55555555-5555-5555-5555-555555555009', '55555555-5555-5555-5555-555555555555', '$$.currency', '/envelope/body/currencyCode', 9);

INSERT INTO StaticValues (Id, IntegrationMappingId, Key, Value, IsGlobal)
VALUES
  ('55555555-5555-5555-5555-555555555101', '55555555-5555-5555-5555-555555555555', 'currency', 'USD', 0),
  ('55555555-5555-5555-5555-555555555102', '55555555-5555-5555-5555-555555555555', 'terms', 'NET30', 0),
  ('55555555-5555-5555-5555-555555555103', '55555555-5555-5555-5555-555555555555', 'invoiceType', 'STANDARD', 0);

-- Integration 6: Customer Notification Service
INSERT INTO IntegrationMappings (Id, Name, Endpoint, SourceType, DestinationType, DestinationUrl, DispatchFor, IsActive, Version, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
VALUES ('66666666-6666-6666-6666-666666666666', 'Customer Notification Service', '/api/notifications/send', 'JSON', 'JSON', 'https://messaging-api.demo.local/v1/notifications', NULL, 1, 1, datetime('now'), datetime('now'), NULL, NULL);

INSERT INTO FieldMappings (Id, IntegrationMappingId, Source, Destination, "Order")
VALUES
  ('66666666-6666-6666-6666-666666666001', '66666666-6666-6666-6666-666666666666', '$.userId', '$.recipient.customerId', 1),
  ('66666666-6666-6666-6666-666666666002', '66666666-6666-6666-6666-666666666666', '$.email', '$.recipient.emailAddress', 2),
  ('66666666-6666-6666-6666-666666666003', '66666666-6666-6666-6666-666666666666', '$.message.subject', '$.content.title', 3),
  ('66666666-6666-6666-6666-666666666004', '66666666-6666-6666-6666-666666666666', '$.message.body', '$.content.message', 4),
  ('66666666-6666-6666-6666-666666666005', '66666666-6666-6666-6666-666666666666', '$$.notificationType', '$.type', 5),
  ('66666666-6666-6666-6666-666666666006', '66666666-6666-6666-6666-666666666666', '$$.priority', '$.deliveryPriority', 6);

INSERT INTO StaticValues (Id, IntegrationMappingId, Key, Value, IsGlobal)
VALUES
  ('66666666-6666-6666-6666-666666666101', '66666666-6666-6666-6666-666666666666', 'notificationType', 'TRANSACTIONAL', 0),
  ('66666666-6666-6666-6666-666666666102', '66666666-6666-6666-6666-666666666666', 'priority', 'NORMAL', 0),
  ('66666666-6666-6666-6666-666666666103', '66666666-6666-6666-6666-666666666666', 'channel', 'EMAIL', 0);
