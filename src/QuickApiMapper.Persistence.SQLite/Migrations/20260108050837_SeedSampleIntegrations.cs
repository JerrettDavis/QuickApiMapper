using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickApiMapper.Persistence.SQLite.Migrations
{
    /// <inheritdoc />
    public partial class SeedSampleIntegrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = DateTime.UtcNow;

            // Integration 1: E-Commerce Order Fulfillment
            var integration1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
            migrationBuilder.InsertData(
                table: "IntegrationMappings",
                columns: new[] { "Id", "Name", "Endpoint", "SourceType", "DestinationType", "DestinationUrl", "IsActive", "EnableInput", "EnableOutput", "EnableMessageCapture", "Version", "CreatedAt", "UpdatedAt" },
                values: new object[] { integration1Id, "E-Commerce Order Fulfillment", "/api/fulfillment/orders", "JSON", "SOAP", "https://warehouse-system.demo.local/soap/v2/fulfillment", true, true, true, true, 1, now, now });

            // Field mappings for Integration 1
            migrationBuilder.InsertData(
                table: "FieldMappings",
                columns: new[] { "Id", "IntegrationMappingId", "Source", "Destination", "Order" },
                values: new object[,]
                {
                    { Guid.Parse("11111111-1111-1111-1111-111111111101"), integration1Id, "$.orderId", "/envelope/body/orderNumber", 1 },
                    { Guid.Parse("11111111-1111-1111-1111-111111111102"), integration1Id, "$.customer.email", "/envelope/body/customerEmail", 2 },
                    { Guid.Parse("11111111-1111-1111-1111-111111111103"), integration1Id, "$.customer.name", "/envelope/body/customerName", 3 },
                    { Guid.Parse("11111111-1111-1111-1111-111111111104"), integration1Id, "$.shippingAddress.street", "/envelope/body/address/street", 4 },
                    { Guid.Parse("11111111-1111-1111-1111-111111111105"), integration1Id, "$.shippingAddress.city", "/envelope/body/address/city", 5 },
                    { Guid.Parse("11111111-1111-1111-1111-111111111106"), integration1Id, "$.shippingAddress.postalCode", "/envelope/body/address/zipCode", 6 },
                    { Guid.Parse("11111111-1111-1111-1111-111111111107"), integration1Id, "$.items[*].sku", "/envelope/body/lineItems/item/productCode", 7 },
                    { Guid.Parse("11111111-1111-1111-1111-111111111108"), integration1Id, "$.items[*].quantity", "/envelope/body/lineItems/item/qty", 8 },
                    { Guid.Parse("11111111-1111-1111-1111-111111111109"), integration1Id, "$$.fulfillmentPriority", "/envelope/body/priority", 9 }
                });

            // Static values for Integration 1
            migrationBuilder.InsertData(
                table: "StaticValues",
                columns: new[] { "Id", "IntegrationMappingId", "Key", "Value", "IsGlobal" },
                values: new object[,]
                {
                    { Guid.Parse("11111111-1111-1111-1111-111111111201"), integration1Id, "fulfillmentPriority", "STANDARD", false },
                    { Guid.Parse("11111111-1111-1111-1111-111111111202"), integration1Id, "warehouseId", "WH-001", false },
                    { Guid.Parse("11111111-1111-1111-1111-111111111203"), integration1Id, "processingStatus", "PENDING", false }
                });

            // Integration 2: Supplier Data Sync
            var integration2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
            migrationBuilder.InsertData(
                table: "integrationmappings",
                columns: new[] { "Id", "Name", "Endpoint", "SourceType", "DestinationType", "DestinationUrl", "IsActive", "EnableInput", "EnableOutput", "EnableMessageCapture", "Version", "CreatedAt", "UpdatedAt" },
                values: new object[] { integration2Id, "Supplier Data Sync", "/api/suppliers/sync", "JSON", "SOAP", "https://procurement-api.demo.local/soap/suppliers", true, true, true, true, 1, now, now });

            // Field mappings for Integration 2
            migrationBuilder.InsertData(
                table: "FieldMappings",
                columns: new[] { "Id", "IntegrationMappingId", "Source", "Destination", "Order" },
                values: new object[,]
                {
                    { Guid.Parse("22222222-2222-2222-2222-222222222201"), integration2Id, "$.supplierId", "/envelope/body/vendorCode", 1 },
                    { Guid.Parse("22222222-2222-2222-2222-222222222202"), integration2Id, "$.companyName", "/envelope/body/businessName", 2 },
                    { Guid.Parse("22222222-2222-2222-2222-222222222203"), integration2Id, "$.taxId", "/envelope/body/taxIdentifier", 3 },
                    { Guid.Parse("22222222-2222-2222-2222-222222222204"), integration2Id, "$.contact.email", "/envelope/body/primaryEmail", 4 },
                    { Guid.Parse("22222222-2222-2222-2222-222222222205"), integration2Id, "$.contact.phone", "/envelope/body/phoneNumber", 5 },
                    { Guid.Parse("22222222-2222-2222-2222-222222222206"), integration2Id, "$.paymentTerms", "/envelope/body/terms", 6 },
                    { Guid.Parse("22222222-2222-2222-2222-222222222207"), integration2Id, "$$.accountStatus", "/envelope/body/status", 7 }
                });

            // Static values for Integration 2
            migrationBuilder.InsertData(
                table: "StaticValues",
                columns: new[] { "Id", "IntegrationMappingId", "Key", "Value", "IsGlobal" },
                values: new object[,]
                {
                    { Guid.Parse("22222222-2222-2222-2222-222222222301"), integration2Id, "accountStatus", "ACTIVE", false },
                    { Guid.Parse("22222222-2222-2222-2222-222222222302"), integration2Id, "vendorType", "STANDARD", false },
                    { Guid.Parse("22222222-2222-2222-2222-222222222303"), integration2Id, "creditRating", "A", false }
                });

            // Integration 3: Product Catalog Integration
            var integration3Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
            migrationBuilder.InsertData(
                table: "integrationmappings",
                columns: new[] { "Id", "Name", "Endpoint", "SourceType", "DestinationType", "DestinationUrl", "IsActive", "EnableInput", "EnableOutput", "EnableMessageCapture", "Version", "CreatedAt", "UpdatedAt" },
                values: new object[] { integration3Id, "Product Catalog Integration", "/api/catalog/products", "XML", "JSON", "https://catalog-api.demo.local/v1/products", true, true, true, true, 1, now, now });

            // Field mappings for Integration 3
            migrationBuilder.InsertData(
                table: "FieldMappings",
                columns: new[] { "Id", "IntegrationMappingId", "Source", "Destination", "Order" },
                values: new object[,]
                {
                    { Guid.Parse("33333333-3333-3333-3333-333333333301"), integration3Id, "/products/product/sku", "$.productCode", 1 },
                    { Guid.Parse("33333333-3333-3333-3333-333333333302"), integration3Id, "/products/product/name", "$.title", 2 },
                    { Guid.Parse("33333333-3333-3333-3333-333333333303"), integration3Id, "/products/product/description", "$.longDescription", 3 },
                    { Guid.Parse("33333333-3333-3333-3333-333333333304"), integration3Id, "/products/product/price", "$.pricing.basePrice", 4 },
                    { Guid.Parse("33333333-3333-3333-3333-333333333305"), integration3Id, "/products/product/category", "$.categoryPath", 5 },
                    { Guid.Parse("33333333-3333-3333-3333-333333333306"), integration3Id, "/products/product/stock", "$.inventory.quantity", 6 },
                    { Guid.Parse("33333333-3333-3333-3333-333333333307"), integration3Id, "/products/product/images/image", "$.media.images[*]", 7 }
                });

            // Static values for Integration 3
            migrationBuilder.InsertData(
                table: "StaticValues",
                columns: new[] { "Id", "IntegrationMappingId", "Key", "Value", "IsGlobal" },
                values: new object[,]
                {
                    { Guid.Parse("33333333-3333-3333-3333-333333333401"), integration3Id, "currency", "USD", false },
                    { Guid.Parse("33333333-3333-3333-3333-333333333402"), integration3Id, "status", "PUBLISHED", false },
                    { Guid.Parse("33333333-3333-3333-3333-333333333403"), integration3Id, "visibility", "PUBLIC", false }
                });

            // Integration 4: Shipping Label Generator (Inactive)
            var integration4Id = Guid.Parse("44444444-4444-4444-4444-444444444444");
            migrationBuilder.InsertData(
                table: "integrationmappings",
                columns: new[] { "Id", "Name", "Endpoint", "SourceType", "DestinationType", "DestinationUrl", "IsActive", "EnableInput", "EnableOutput", "EnableMessageCapture", "Version", "CreatedAt", "UpdatedAt" },
                values: new object[] { integration4Id, "Shipping Label Generator", "/api/shipping/labels", "JSON", "JSON", "https://logistics-api.demo.local/v2/labels", false, true, true, true, 1, now, now });

            // Field mappings for Integration 4
            migrationBuilder.InsertData(
                table: "FieldMappings",
                columns: new[] { "Id", "IntegrationMappingId", "Source", "Destination", "Order" },
                values: new object[,]
                {
                    { Guid.Parse("44444444-4444-4444-4444-444444444401"), integration4Id, "$.shipmentId", "$.trackingNumber", 1 },
                    { Guid.Parse("44444444-4444-4444-4444-444444444402"), integration4Id, "$.fromAddress", "$.origin", 2 },
                    { Guid.Parse("44444444-4444-4444-4444-444444444403"), integration4Id, "$.toAddress", "$.destination", 3 },
                    { Guid.Parse("44444444-4444-4444-4444-444444444404"), integration4Id, "$.package.weight", "$.packageWeight", 4 },
                    { Guid.Parse("44444444-4444-4444-4444-444444444405"), integration4Id, "$.package.dimensions", "$.packageSize", 5 },
                    { Guid.Parse("44444444-4444-4444-4444-444444444406"), integration4Id, "$$.serviceLevel", "$.shippingMethod", 6 }
                });

            // Static values for Integration 4
            migrationBuilder.InsertData(
                table: "StaticValues",
                columns: new[] { "Id", "IntegrationMappingId", "Key", "Value", "IsGlobal" },
                values: new object[,]
                {
                    { Guid.Parse("44444444-4444-4444-4444-444444444501"), integration4Id, "serviceLevel", "GROUND", false },
                    { Guid.Parse("44444444-4444-4444-4444-444444444502"), integration4Id, "carrier", "FEDEX", false },
                    { Guid.Parse("44444444-4444-4444-4444-444444444503"), integration4Id, "labelFormat", "PDF", false }
                });

            // Integration 5: Invoice Processing Service
            var integration5Id = Guid.Parse("55555555-5555-5555-5555-555555555555");
            migrationBuilder.InsertData(
                table: "integrationmappings",
                columns: new[] { "Id", "Name", "Endpoint", "SourceType", "DestinationType", "DestinationUrl", "IsActive", "EnableInput", "EnableOutput", "EnableMessageCapture", "Version", "CreatedAt", "UpdatedAt" },
                values: new object[] { integration5Id, "Invoice Processing Service", "/api/accounting/invoices", "JSON", "SOAP", "https://finance-system.demo.local/soap/invoices", true, true, true, true, 1, now, now });

            // Field mappings for Integration 5
            migrationBuilder.InsertData(
                table: "FieldMappings",
                columns: new[] { "Id", "IntegrationMappingId", "Source", "Destination", "Order" },
                values: new object[,]
                {
                    { Guid.Parse("55555555-5555-5555-5555-555555555501"), integration5Id, "$.invoiceNumber", "/envelope/body/invoiceId", 1 },
                    { Guid.Parse("55555555-5555-5555-5555-555555555502"), integration5Id, "$.billTo.customerId", "/envelope/body/accountNumber", 2 },
                    { Guid.Parse("55555555-5555-5555-5555-555555555503"), integration5Id, "$.billTo.name", "/envelope/body/billingName", 3 },
                    { Guid.Parse("55555555-5555-5555-5555-555555555504"), integration5Id, "$.invoiceDate", "/envelope/body/issueDate", 4 },
                    { Guid.Parse("55555555-5555-5555-5555-555555555505"), integration5Id, "$.dueDate", "/envelope/body/paymentDue", 5 },
                    { Guid.Parse("55555555-5555-5555-5555-555555555506"), integration5Id, "$.lineItems[*].description", "/envelope/body/items/item/desc", 6 },
                    { Guid.Parse("55555555-5555-5555-5555-555555555507"), integration5Id, "$.lineItems[*].amount", "/envelope/body/items/item/price", 7 },
                    { Guid.Parse("55555555-5555-5555-5555-555555555508"), integration5Id, "$.totalAmount", "/envelope/body/totalDue", 8 },
                    { Guid.Parse("55555555-5555-5555-5555-555555555509"), integration5Id, "$$.currency", "/envelope/body/currencyCode", 9 }
                });

            // Static values for Integration 5
            migrationBuilder.InsertData(
                table: "StaticValues",
                columns: new[] { "Id", "IntegrationMappingId", "Key", "Value", "IsGlobal" },
                values: new object[,]
                {
                    { Guid.Parse("55555555-5555-5555-5555-555555555601"), integration5Id, "currency", "USD", false },
                    { Guid.Parse("55555555-5555-5555-5555-555555555602"), integration5Id, "terms", "NET30", false },
                    { Guid.Parse("55555555-5555-5555-5555-555555555603"), integration5Id, "invoiceType", "STANDARD", false }
                });

            // Integration 6: Customer Notification Service
            var integration6Id = Guid.Parse("66666666-6666-6666-6666-666666666666");
            migrationBuilder.InsertData(
                table: "integrationmappings",
                columns: new[] { "Id", "Name", "Endpoint", "SourceType", "DestinationType", "DestinationUrl", "IsActive", "EnableInput", "EnableOutput", "EnableMessageCapture", "Version", "CreatedAt", "UpdatedAt" },
                values: new object[] { integration6Id, "Customer Notification Service", "/api/notifications/send", "JSON", "JSON", "https://messaging-api.demo.local/v1/notifications", true, true, true, true, 1, now, now });

            // Field mappings for Integration 6
            migrationBuilder.InsertData(
                table: "FieldMappings",
                columns: new[] { "Id", "IntegrationMappingId", "Source", "Destination", "Order" },
                values: new object[,]
                {
                    { Guid.Parse("66666666-6666-6666-6666-666666666601"), integration6Id, "$.userId", "$.recipient.customerId", 1 },
                    { Guid.Parse("66666666-6666-6666-6666-666666666602"), integration6Id, "$.email", "$.recipient.emailAddress", 2 },
                    { Guid.Parse("66666666-6666-6666-6666-666666666603"), integration6Id, "$.message.subject", "$.content.title", 3 },
                    { Guid.Parse("66666666-6666-6666-6666-666666666604"), integration6Id, "$.message.body", "$.content.message", 4 },
                    { Guid.Parse("66666666-6666-6666-6666-666666666605"), integration6Id, "$$.notificationType", "$.type", 5 },
                    { Guid.Parse("66666666-6666-6666-6666-666666666606"), integration6Id, "$$.priority", "$.deliveryPriority", 6 }
                });

            // Static values for Integration 6
            migrationBuilder.InsertData(
                table: "StaticValues",
                columns: new[] { "Id", "IntegrationMappingId", "Key", "Value", "IsGlobal" },
                values: new object[,]
                {
                    { Guid.Parse("66666666-6666-6666-6666-666666666701"), integration6Id, "notificationType", "TRANSACTIONAL", false },
                    { Guid.Parse("66666666-6666-6666-6666-666666666702"), integration6Id, "priority", "NORMAL", false },
                    { Guid.Parse("66666666-6666-6666-6666-666666666703"), integration6Id, "channel", "EMAIL", false }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Delete in reverse order due to foreign key constraints
            migrationBuilder.DeleteData(table: "StaticValues", keyColumn: "Id", keyValue: Guid.Parse("11111111-1111-1111-1111-111111111201"));
            migrationBuilder.DeleteData(table: "StaticValues", keyColumn: "Id", keyValue: Guid.Parse("11111111-1111-1111-1111-111111111202"));
            migrationBuilder.DeleteData(table: "StaticValues", keyColumn: "Id", keyValue: Guid.Parse("11111111-1111-1111-1111-111111111203"));
            migrationBuilder.DeleteData(table: "StaticValues", keyColumn: "Id", keyValue: Guid.Parse("22222222-2222-2222-2222-222222222301"));
            migrationBuilder.DeleteData(table: "StaticValues", keyColumn: "Id", keyValue: Guid.Parse("22222222-2222-2222-2222-222222222302"));
            migrationBuilder.DeleteData(table: "StaticValues", keyColumn: "Id", keyValue: Guid.Parse("22222222-2222-2222-2222-222222222303"));
            migrationBuilder.DeleteData(table: "StaticValues", keyColumn: "Id", keyValue: Guid.Parse("33333333-3333-3333-3333-333333333401"));
            migrationBuilder.DeleteData(table: "StaticValues", keyColumn: "Id", keyValue: Guid.Parse("33333333-3333-3333-3333-333333333402"));
            migrationBuilder.DeleteData(table: "StaticValues", keyColumn: "Id", keyValue: Guid.Parse("33333333-3333-3333-3333-333333333403"));
            migrationBuilder.DeleteData(table: "StaticValues", keyColumn: "Id", keyValue: Guid.Parse("44444444-4444-4444-4444-444444444501"));
            migrationBuilder.DeleteData(table: "StaticValues", keyColumn: "Id", keyValue: Guid.Parse("44444444-4444-4444-4444-444444444502"));
            migrationBuilder.DeleteData(table: "StaticValues", keyColumn: "Id", keyValue: Guid.Parse("44444444-4444-4444-4444-444444444503"));
            migrationBuilder.DeleteData(table: "StaticValues", keyColumn: "Id", keyValue: Guid.Parse("55555555-5555-5555-5555-555555555601"));
            migrationBuilder.DeleteData(table: "StaticValues", keyColumn: "Id", keyValue: Guid.Parse("55555555-5555-5555-5555-555555555602"));
            migrationBuilder.DeleteData(table: "StaticValues", keyColumn: "Id", keyValue: Guid.Parse("55555555-5555-5555-5555-555555555603"));
            migrationBuilder.DeleteData(table: "StaticValues", keyColumn: "Id", keyValue: Guid.Parse("66666666-6666-6666-6666-666666666701"));
            migrationBuilder.DeleteData(table: "StaticValues", keyColumn: "Id", keyValue: Guid.Parse("66666666-6666-6666-6666-666666666702"));
            migrationBuilder.DeleteData(table: "StaticValues", keyColumn: "Id", keyValue: Guid.Parse("66666666-6666-6666-6666-666666666703"));

            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("11111111-1111-1111-1111-111111111101"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("11111111-1111-1111-1111-111111111102"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("11111111-1111-1111-1111-111111111103"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("11111111-1111-1111-1111-111111111104"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("11111111-1111-1111-1111-111111111105"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("11111111-1111-1111-1111-111111111106"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("11111111-1111-1111-1111-111111111107"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("11111111-1111-1111-1111-111111111108"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("11111111-1111-1111-1111-111111111109"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("22222222-2222-2222-2222-222222222201"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("22222222-2222-2222-2222-222222222202"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("22222222-2222-2222-2222-222222222203"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("22222222-2222-2222-2222-222222222204"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("22222222-2222-2222-2222-222222222205"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("22222222-2222-2222-2222-222222222206"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("22222222-2222-2222-2222-222222222207"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("33333333-3333-3333-3333-333333333301"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("33333333-3333-3333-3333-333333333302"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("33333333-3333-3333-3333-333333333303"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("33333333-3333-3333-3333-333333333304"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("33333333-3333-3333-3333-333333333305"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("33333333-3333-3333-3333-333333333306"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("33333333-3333-3333-3333-333333333307"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("44444444-4444-4444-4444-444444444401"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("44444444-4444-4444-4444-444444444402"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("44444444-4444-4444-4444-444444444403"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("44444444-4444-4444-4444-444444444404"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("44444444-4444-4444-4444-444444444405"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("44444444-4444-4444-4444-444444444406"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("55555555-5555-5555-5555-555555555501"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("55555555-5555-5555-5555-555555555502"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("55555555-5555-5555-5555-555555555503"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("55555555-5555-5555-5555-555555555504"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("55555555-5555-5555-5555-555555555505"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("55555555-5555-5555-5555-555555555506"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("55555555-5555-5555-5555-555555555507"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("55555555-5555-5555-5555-555555555508"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("55555555-5555-5555-5555-555555555509"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("66666666-6666-6666-6666-666666666601"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("66666666-6666-6666-6666-666666666602"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("66666666-6666-6666-6666-666666666603"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("66666666-6666-6666-6666-666666666604"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("66666666-6666-6666-6666-666666666605"));
            migrationBuilder.DeleteData(table: "FieldMappings", keyColumn: "Id", keyValue: Guid.Parse("66666666-6666-6666-6666-666666666606"));

            migrationBuilder.DeleteData(table: "IntegrationMappings", keyColumn: "Id", keyValue: Guid.Parse("11111111-1111-1111-1111-111111111111"));
            migrationBuilder.DeleteData(table: "IntegrationMappings", keyColumn: "Id", keyValue: Guid.Parse("22222222-2222-2222-2222-222222222222"));
            migrationBuilder.DeleteData(table: "IntegrationMappings", keyColumn: "Id", keyValue: Guid.Parse("33333333-3333-3333-3333-333333333333"));
            migrationBuilder.DeleteData(table: "IntegrationMappings", keyColumn: "Id", keyValue: Guid.Parse("44444444-4444-4444-4444-444444444444"));
            migrationBuilder.DeleteData(table: "IntegrationMappings", keyColumn: "Id", keyValue: Guid.Parse("55555555-5555-5555-5555-555555555555"));
            migrationBuilder.DeleteData(table: "IntegrationMappings", keyColumn: "Id", keyValue: Guid.Parse("66666666-6666-6666-6666-666666666666"));
        }
    }
}
