# Web Designer

The QuickApiMapper Web Designer is a Blazor Server application that provides a user-friendly interface for managing integrations. This guide covers all features and capabilities.

## Overview

The Web Designer provides:
- **Integration Management** - Create, edit, delete integrations
- **Visual Field Mapping** - Drag-and-drop field mapping
- **Test Mode** - Preview transformations
- **Message History** - View captured messages
- **Statistics** - Monitor integration performance

## Accessing the Designer

**Default URL**: `http://localhost:5173`

**With Aspire**: The Aspire dashboard shows the Web Designer URL

## Dashboard

The home page shows key metrics:

### At a Glance

- **Total Integrations** - Number of configured integrations
- **Active Today** - Integrations that processed messages today
- **Success Rate** - Percentage of successful transformations
- **Avg Duration** - Average transformation time

### Recent Activity

Table showing recent integration executions:
- Integration name
- Status (Success/Failed)
- Timestamp
- Duration
- Message count

### Quick Actions

- **Create Integration** - Start new integration wizard
- **View All Integrations** - Browse all integrations
- **View Messages** - Browse captured messages
- **Settings** - Configure application settings

## Integration List

Navigate to **Integrations** to view all configured integrations.

### List View

Each integration shows:
- **Name** - Integration display name
- **Integration ID** - URL identifier
- **Type** - Source → Destination types
- **Status** - Enabled/Disabled
- **Last Run** - When last executed
- **Actions** - Quick action buttons

### Filtering

Filter integrations by:
- **Search** - Search by name or ID
- **Source Type** - JSON, XML, gRPC
- **Destination Type** - SOAP, JSON, gRPC, RabbitMQ, Service Bus
- **Status** - Active, Inactive

### Sorting

Sort by:
- Name (A-Z, Z-A)
- Last modified
- Created date
- Success rate

### Actions

Quick actions for each integration:
- **▶ Test** - Open test dialog
- **✏ Edit** - Edit integration
- **📊 Stats** - View statistics
- **📧 Messages** - View message history
- **⋮ More** - Additional options (Clone, Export, Delete)

## Creating Integrations

Click **Create Integration** to start the wizard.

### Step 1: Basic Information

**Fields**:
- **Name** - Descriptive name (e.g., "Customer to ERP Integration")
- **Integration ID** - Auto-generated from name (can edit)
- **Description** - Optional description
- **Source Type** - Dropdown: JSON, XML, gRPC
- **Destination Type** - Dropdown: JSON, SOAP, gRPC, RabbitMQ, Service Bus

**Validation**:
- Name is required
- Integration ID must be unique
- Integration ID must be URL-safe (lowercase, hyphens, numbers)

### Step 2: Source Configuration

Configure source-specific settings.

**JSON Source**:
No additional configuration required.

**XML Source**:
- Namespace mappings (optional)

**gRPC Source**:
- Service name
- Method name
- Proto file upload

### Step 3: Destination Configuration

Configure destination-specific settings.

**JSON Destination**:
- Endpoint URL
- HTTP Method (POST, PUT, PATCH)
- Headers

**SOAP Destination**:
- Endpoint URL
- SOAP Action
- Method Name
- Target Namespace
- SOAP Version (1.1 or 1.2)

**gRPC Destination**:
- Endpoint URL
- Service Name
- Method Name
- Proto file upload

**RabbitMQ Destination**:
- Connection String
- Exchange Name
- Routing Key
- Exchange Type

**Service Bus Destination**:
- Connection String
- Queue Name OR Topic Name

### Step 4: Field Mappings

Map fields from source to destination.

**Interface**:
- **Add Mapping** button - Add new field mapping
- **Table View** - Shows all mappings
 - Source Path
 - Destination Path
 - Transformers
 - Actions (Edit, Delete)

**Adding Mapping**:
1. Click **Add Mapping**
2. Enter **Source Path** (JSONPath or XPath)
3. Enter **Destination Path** (dot notation)
4. Optionally add **Transformers**
5. Click **Save**

**Transformers**:
- **Add Transformer** - Select from dropdown
- **Parameters** - Configure transformer parameters (if any)
- **Reorder** - Drag to change execution order
- **Remove** - Delete transformer

### Step 5: Static Values

Add constant values to output.

**Interface**:
- **Add Static Value** button
- **Table View** - Shows all static values
 - Destination Path
 - Value
 - Actions (Edit, Delete)

**Adding Static Value**:
1. Click **Add Static Value**
2. Enter **Destination Path**
3. Enter **Static Value**
4. Click **Save**

### Step 6: Behaviors (Optional)

Configure integration behaviors.

**Available Behaviors**:
- **Authentication** - Add auth headers
- **Validation** - Input/output validation
- **Timing** - Performance monitoring
- **Message Capture** - Enable message capture

### Step 7: Review & Save

Review configuration and save:
- **Review Tab** - Shows all settings
- **Test** - Test with sample data
- **Save** - Create integration
- **Save & Deploy** - Create and enable immediately

## Editing Integrations

Click the **Edit** button on any integration.

**Editable**:
- Name
- Description
- Source/Destination configurations
- Field mappings
- Static values
- Behaviors

**Not Editable**:
- Integration ID (cannot change once created)
- Source Type
- Destination Type

**Actions**:
- **Save** - Save changes
- **Save & Test** - Save and open test dialog
- **Cancel** - Discard changes

## Testing Integrations

Click the **Test** button (▶) to test an integration.

### Test Dialog

**Tabs**:
1. **Input** - Enter sample data
2. **Output** - View transformed result
3. **Debug** - See field-by-field transformations

**Input Tab**:
- **Sample Payload** - Text area for JSON/XML
- **Load Sample** - Load predefined sample
- **Override Static Values** - Optionally override static values
- **Test Transform** - Execute test

**Output Tab**:
- **Transformed Output** - Read-only result
- **Copy** - Copy to clipboard
- **Download** - Save as file
- **Format** - Pretty-print JSON/XML

**Debug Tab**:
- **Transformation Steps** - Table showing each field
 - Source Path
 - Source Value
 - Transformers Applied
 - Destination Value
- **Duration** - Time taken
- **Errors** - Any errors encountered

### Sample Data Library

Pre-populated samples for common scenarios:
- Simple customer
- Customer with address
- Order with line items
- Nested objects
- Arrays

## Message History

View captured messages for an integration.

### Message List

**Columns**:
- **Timestamp** - When captured
- **Direction** - Input or Output
- **Status** - Success or Failed
- **Duration** - Processing time
- **Actions** - View, Replay (future)

### Filters

Filter messages by:
- **Date Range** - Start and end dates
- **Direction** - Input, Output, or Both
- **Status** - Success, Failed, or All
- **Search** - Search in payload

### Message Details

Click on a message to view details:

**Tabs**:
1. **Payload** - Full message content
2. **Metadata** - Correlation ID, timestamps, etc.
3. **Linked Message** - Related input/output message

**Payload Tab**:
- Syntax-highlighted JSON/XML
- **Copy** button
- **Download** button
- Truncation indicator if payload was truncated

**Metadata Tab**:
- Correlation ID
- Integration Name
- Direction
- Status
- Error Message (if failed)
- Duration (for output)
- Timestamp
- Custom metadata

**Linked Message Tab**:
- Link to corresponding input/output message
- Click to navigate to linked message

## Statistics

View integration performance metrics.

### Overview

**Metrics**:
- **Total Executions** - All-time count
- **Success Rate** - Percentage successful
- **Average Duration** - Mean processing time
- **Error Rate** - Percentage failed

### Charts

**Execution Trend**:
- Line chart showing executions over time
- Grouped by day/hour
- Success vs Failed

**Duration Distribution**:
- Histogram of processing times
- Percentiles (P50, P90, P95, P99)

**Error Breakdown**:
- Pie chart of error types
- Top errors by frequency

### Time Periods

Select time period:
- Last 24 hours
- Last 7 days
- Last 30 days
- Custom range

## Settings

Global application settings.

### General

- **Application Name** - Display name
- **Theme** - Light or Dark mode
- **Language** - UI language

### Message Capture

- **Enable Globally** - Enable/disable message capture
- **Retention Period** - How long to keep messages
- **Max Payload Size** - Maximum captured payload size
- **Redact Fields** - Fields to redact (comma-separated)

### Performance

- **Cache Duration** - Configuration cache TTL
- **Max Concurrent Requests** - Limit concurrent processing

### Security

- **Require Authentication** - Enable auth (future)
- **API Key** - API key for Management API access

## Keyboard Shortcuts

**Global**:
- `Ctrl+K` - Search integrations
- `Ctrl+N` - Create new integration
- `Ctrl+/` - Open help

**Integration List**:
- `↑` / `↓` - Navigate rows
- `Enter` - Open selected integration
- `E` - Edit selected integration
- `T` - Test selected integration
- `Delete` - Delete selected integration (with confirmation)

**Test Dialog**:
- `Ctrl+Enter` - Execute test
- `Ctrl+C` - Copy output
- `Esc` - Close dialog

## Mobile Support

The Web Designer is responsive and works on mobile devices.

**Mobile Features**:
- Touch-friendly buttons
- Swipe gestures for navigation
- Optimized layouts for small screens

**Limitations**:
- Field mapping is easier on desktop
- Test dialog better on larger screens
- Message payload viewing better on desktop

## Accessibility

**Features**:
- Keyboard navigation
- Screen reader support
- ARIA labels
- High contrast mode
- Focus indicators

## Themes

### Light Theme

Default theme with:
- Light backgrounds
- Dark text
- Blue accents

### Dark Theme

Toggle in **Settings**:
- Dark backgrounds
- Light text
- Blue accents
- Reduced eye strain

### Custom Themes

Create custom themes (future):
- Brand colors
- Custom fonts
- Logo

## Best Practices

### 1. Use Descriptive Names

Give integrations clear, descriptive names:

 **Good**:
- "Salesforce Customer to ERP ERP"
- "Shopify Orders to Fulfillment System"

 **Bad**:
- "Integration 1"
- "Test"

### 2. Test Before Deploying

Always test integrations before enabling:
1. Create integration
2. Test with sample data
3. Review output
4. Fix any issues
5. Save and enable

### 3. Monitor Message History

Regularly review message history:
- Check for errors
- Verify transformations
- Identify patterns

### 4. Use Statistics

Use statistics to:
- Identify slow integrations
- Find error patterns
- Optimize performance

### 5. Keep Integrations Simple

- One integration per use case
- Clear field mappings
- Minimal transformers
- Well-documented

## Troubleshooting

### Integration Not Saving

**Issue**: Save button doesn't work

**Solutions**:
1. Check all required fields are filled
2. Verify Integration ID is unique
3. Check browser console for errors
4. Try refreshing the page

### Test Returns Empty Output

**Issue**: Test mode returns empty output

**Solutions**:
1. Verify source paths are correct
2. Check sample payload structure
3. Review Debug tab for details
4. Test with simpler payload

### Messages Not Appearing

**Issue**: No messages in Message History

**Solutions**:
1. Verify message capture is enabled
2. Check integration has been executed
3. Verify date range filter
4. Check Management API is accessible

## Next Steps

- [Creating Integrations](creating-integrations.md) - Learn integration creation
- [Test Mode](test-mode.md) - Master testing
- [Message Capture](message-capture.md) - Understand message capture
