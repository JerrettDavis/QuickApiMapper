# Demo Runner Message Capture Integration - COMPLETE

## Issue

Messages executed via the Demo Runner (`/demo/runner`) were not appearing in Message Flow (`/demo/flow`) or Statistics (`/demo/statistics`). Users could execute transformations but wouldn't see them in the monitoring dashboards.

## Root Cause

The `TestingService` was designed purely for testing transformations - it would execute the mapping and return the result, but it **did not capture messages** to the message capture system. This meant:
- No audit trail was created
- No correlation IDs were generated
- Messages didn't appear in Message Flow
- Statistics weren't updated

## Solution Implemented

### 1. Updated TestingService to Capture Messages

**File**: `src/QuickApiMapper.Management.Api/Services/TestingService.cs`

**Changes**:
1. **Added dependency injection** for `IMessageCaptureProvider`
2. **Generate correlation ID** at the start of each test execution
3. **Capture input message** before transformation with:
   - Correlation ID for tracking
   - Pending status
   - Metadata marking it as "TestMode" from "DemoRunner"
4. **Capture output message** after successful transformation with:
   - Same correlation ID
   - Success/Failed status
   - Duration metrics
   - Field mapping count
5. **Capture error messages** on exceptions
6. **Return correlation ID** in response metadata

### 2. Updated DemoRunner to Use Correlation ID

**File**: `src/QuickApiMapper.Designer.Web/Components/Pages/DemoRunner.razor`

**Changes**:
1. **Extract correlation ID** from API response metadata
2. **Extract actual duration** from API instead of client-side timing
3. **Display correlation ID** in success notification
4. **Link to Message Flow** using the actual correlation ID

## How It Works Now

### Execution Flow

```
User clicks "Execute Request" in Demo Runner
          ↓
POST /api/integrations/{id}/test
          ↓
TestingService.ExecuteTestAsync()
          ↓
1. Generate correlation ID (e.g., "96c750a2-8869-49d8-aacb-ea9e0aec6010")
2. Capture input message (Status: Pending)
3. Execute transformation
4. Capture output message (Status: Success/Failed)
5. Return result with correlation ID in metadata
          ↓
DemoRunner extracts correlation ID
          ↓
User can click "View in Message Flow" → /demo/flow?correlation={id}
          ↓
Message Flow shows both input and output messages
Statistics are updated with the new transformation
```

### Message Metadata

Each captured message includes:
- **Source**: "DemoRunner"
- **TestMode**: "true"
- **CorrelationId**: Shared between input and output
- **Duration**: Processing time in milliseconds
- **FieldMappingCount**: Number of field mappings applied

This metadata allows filtering demo messages separately from production traffic if needed.

## Testing Instructions

### 1. Stop and Rebuild

```bash
# Stop any running services (Ctrl+C)
cd C:\git\IFS\QuickApiMapper
dotnet build
```

### 2. Start the Demo

```bash
cd src/QuickApiMapper.Host.AppHost
dotnet run
```

### 3. Test the Integration

1. **Open Designer Web** from Aspire Dashboard

2. **Navigate to Demo Runner**: `/demo/runner`

3. **Execute a test**:
   - Select "Demo: JSON to SOAP Order Processing"
   - Choose "Sample E-Commerce Order"
   - Click "Execute Request"

4. **Verify the result**:
   - ✅ Should see success notification with correlation ID
   - ✅ Should see transformed SOAP output
   - ✅ Should see execution time
   - ✅ Should see correlation ID in the alert

5. **View in Message Flow**:
   - Click "View in Message Flow" button
   - Should navigate to `/demo/flow?correlation={id}`
   - Should see TWO messages with the same correlation ID:
     - **Input message** (Direction: Input, Status: Pending)
     - **Output message** (Direction: Output, Status: Success)

6. **Check Statistics**:
   - Navigate to `/demo/statistics`
   - Should see metrics updated:
     - Total Transformations count increased
     - Success rate updated
     - Average processing time updated
     - New entry in Integration Performance table

### 4. Test Error Handling

1. **Navigate to Demo Runner**

2. **Modify sample request** to cause an error:
   - Edit the JSON to be invalid (e.g., remove a closing brace)
   - Click "Execute Request"

3. **Verify error capture**:
   - Should see error notification
   - Should still get a correlation ID
   - View in Message Flow should show:
     - Input message (captured before error)
     - Output message with Status: Failed and error details

## Expected Behavior

### Demo Runner Page

**Before execution**:
- Integration selection
- Sample payload preview
- "Execute Request" button

**After successful execution**:
- ✅ Green "Success" badge
- ✅ Execution time displayed (e.g., "125ms")
- ✅ Status code: 200
- ✅ Correlation ID shown in info alert
- ✅ Transformed payload with syntax highlighting
- ✅ "View in Message Flow" button (clickable)

**After failed execution**:
- ❌ Red "Failed" badge
- ✅ Error message displayed
- ✅ Correlation ID still provided
- ✅ "View in Message Flow" still works

### Message Flow Page

**When viewing by correlation ID**:
- Should display exactly 2 messages
- Both messages share the same correlation ID
- Pipeline view shows: Input → Transform → Output
- Timeline shows chronological order
- Can click "Compare Input/Output" to see diff

### Statistics Page

**After executing tests**:
- Total message count increases
- Success rate recalculates
- Average processing time updates
- Charts update with new data points
- Integration performance table shows test executions

## Verification Checklist

Execute multiple tests and verify:

- [ ] Each test execution creates 2 captured messages (input + output)
- [ ] Correlation IDs are unique per execution
- [ ] Input and output messages share the same correlation ID
- [ ] Message Flow displays both messages correctly
- [ ] "View in Message Flow" navigates to correct correlation
- [ ] Statistics page updates with new metrics
- [ ] Charts render with accumulated data
- [ ] Failed executions are captured with error details
- [ ] Metadata includes "TestMode": "true"
- [ ] Metadata includes "Source": "DemoRunner"
- [ ] Duration is accurately calculated and displayed
- [ ] Success/Failure status is correctly set

## API Contract Updates

### TestMappingResponse Metadata

The response now includes additional metadata:

```json
{
  "success": true,
  "transformedPayload": "<soap>...</soap>",
  "metadata": {
    "SourceType": "JSON",
    "DestinationType": "SOAP",
    "IntegrationName": "Demo: JSON to SOAP Order Processing",
    "CorrelationId": "96c750a2-8869-49d8-aacb-ea9e0aec6010",
    "Duration": "125"
  }
}
```

### Captured Message Structure

```csharp
{
  Id: "96c750a2-8869-49d8-aacb-ea9e0aec6010-input",
  IntegrationId: <guid>,
  IntegrationName: "Demo: JSON to SOAP Order Processing",
  Direction: "Input",
  Payload: "{\"orderId\": \"...\"}",
  Status: "Pending",
  CorrelationId: "96c750a2-8869-49d8-aacb-ea9e0aec6010",
  Timestamp: "2026-01-11T10:30:00Z",
  Metadata: {
    "Source": "DemoRunner",
    "TestMode": "true"
  }
}
```

## Files Modified

1. `src/QuickApiMapper.Management.Api/Services/TestingService.cs`
   - Added IMessageCaptureProvider dependency
   - Added correlation ID generation
   - Added input message capture
   - Added output message capture
   - Added error message capture
   - Updated response metadata

2. `src/QuickApiMapper.Designer.Web/Components/Pages/DemoRunner.razor`
   - Extract correlation ID from response
   - Use API-provided duration
   - Display correlation ID in notifications
   - Link to Message Flow with actual correlation ID

3. `src/QuickApiMapper.Management.Api/Controllers/MessagesController.cs` *(from previous fix)*
   - Map domain models to DTOs
   - Return MessagePagedResult

## Benefits

### For Users
- ✅ **Complete visibility** into all demo executions
- ✅ **End-to-end tracking** with correlation IDs
- ✅ **Accurate metrics** in statistics dashboard
- ✅ **Compare input/output** to understand transformations
- ✅ **Troubleshoot errors** with captured error messages

### For Demonstrations
- ✅ **Show real-time flow** as transformations execute
- ✅ **Prove message capture** works correctly
- ✅ **Demonstrate monitoring** capabilities
- ✅ **Visualize data pipeline** in action
- ✅ **Track performance** metrics live

### For Development
- ✅ **Test audit trail** without hitting external systems
- ✅ **Debug transformations** with captured payloads
- ✅ **Verify field mappings** work correctly
- ✅ **Measure performance** accurately
- ✅ **Simulate production** message flow

## Known Limitations

### Current Implementation

1. **No actual destination call**: Test executions don't call the real destination URL (by design for testing)
2. **Test mode flag**: Messages are marked with `"TestMode": "true"` to distinguish from production
3. **In-memory storage**: Uses the configured message capture provider (typically in-memory for demo)

### Future Enhancements

- [ ] Option to call real destination in "Live Test" mode
- [ ] Separate retention policy for test messages
- [ ] Bulk delete test messages
- [ ] Export test results
- [ ] Test history/comparison

## Troubleshooting

### Messages Still Not Appearing

**Symptom**: Executed test but messages don't show in Message Flow.

**Solutions**:
1. Check Management API logs for errors during message capture
2. Verify `IMessageCaptureProvider` is registered in DI
3. Check if message capture provider has storage limits reached
4. Refresh Message Flow page (click Refresh button)
5. Check filter - ensure "All Integrations" is selected

### Correlation ID Not Showing

**Symptom**: No correlation ID in the alert or metadata.

**Solutions**:
1. Check browser console for JavaScript errors
2. Verify API response includes `Metadata` property
3. Check if response was successful (200 OK)
4. Reload the Designer Web page

### "View in Message Flow" Does Nothing

**Symptom**: Button click doesn't navigate to Message Flow.

**Solutions**:
1. Check browser console for navigation errors
2. Verify correlation ID is not null/empty
3. Try manually navigating to `/demo/flow`
4. Check if Message Flow page loads independently

### Statistics Not Updating

**Symptom**: Executed tests but statistics unchanged.

**Solutions**:
1. Click "Refresh" button on Statistics page
2. Check selected time range (default 24h)
3. Verify messages were actually captured (check Message Flow)
4. Try switching integration filter
5. Check if date range includes the test execution time

---

**Status**: ✅ **COMPLETE** - Demo Runner now fully integrated with Message Capture
**Date**: 2026-01-11
**Impact**: Critical - Enables complete demo workflow
**Testing**: Ready for immediate testing after rebuild
