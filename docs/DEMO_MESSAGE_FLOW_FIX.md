# Message Flow & Statistics Pages - Fix Applied

## Issue

The Live Message Flow (`/demo/flow`) and Demo Statistics (`/demo/statistics`) pages were not working due to a mismatch between the API response types and the client's expected types.

## Root Cause

The Management API's `MessagesController` was returning domain models (`CapturedMessage`, `PagedResult<CapturedMessage>`) but the Designer Web client was expecting DTOs (`CapturedMessageDto`, `MessagePagedResult`).

## Fix Applied

Updated `src/QuickApiMapper.Management.Api/Controllers/MessagesController.cs` to:

1. **Import the DTO models**:
   ```csharp
   using QuickApiMapper.Management.Contracts.Models;
   ```

2. **Change return types** to use DTOs:
   - `PagedResult<CapturedMessage>` → `MessagePagedResult`
   - `CapturedMessage` → `CapturedMessageDto`

3. **Add mapping method** to convert domain models to DTOs:
   ```csharp
   private static CapturedMessageDto MapToDto(CapturedMessage message)
   {
       return new CapturedMessageDto
       {
           Id = message.Id,
           IntegrationId = message.IntegrationId,
           IntegrationName = message.IntegrationName,
           Direction = message.Direction.ToString(),
           Payload = message.Payload,
           IsTruncated = message.IsTruncated,
           Status = message.Status.ToString(),
           ErrorMessage = message.ErrorMessage,
           Duration = message.Duration,
           CorrelationId = message.CorrelationId,
           Timestamp = message.Timestamp,
           Metadata = message.Metadata
       };
   }
   ```

4. **Updated the Query endpoint** to map results:
   ```csharp
   var result = await _messageCaptureProvider.QueryAsync(filter, cancellationToken);

   // Map to DTO
   var dtoResult = new MessagePagedResult
   {
       Items = result.Items.Select(MapToDto).ToList(),
       TotalCount = result.TotalCount,
       PageNumber = result.PageNumber,
       PageSize = result.PageSize
   };

   return Ok(dtoResult);
   ```

5. **Updated GetById endpoint** to return mapped DTO:
   ```csharp
   return Ok(MapToDto(message));
   ```

## Testing Instructions

### 1. Stop Running Services

If you have the demo running:
```bash
# Stop the Aspire host or Management API
# Press Ctrl+C in the terminal where it's running
```

### 2. Rebuild the Solution

```bash
cd C:\git\IFS\QuickApiMapper
dotnet build
```

### 3. Start the Demo

```bash
cd src/QuickApiMapper.Host.AppHost
dotnet run
```

### 4. Test the Pages

1. **Open Designer Web** (check Aspire Dashboard for the URL)

2. **Navigate to Demo pages**:
   - `/demo/flow` - Live Message Flow
   - `/demo/statistics` - Demo Statistics

3. **Verify functionality**:
   - Message Flow should display captured messages
   - Statistics should show metrics and charts
   - Auto-refresh should work
   - Filters should be functional

### 5. Generate Test Data (if needed)

If there are no messages to display:

#### Option A: Use Demo Runner
1. Go to `/demo/runner`
2. Select an integration (e.g., "Demo: JSON to SOAP Order Processing")
3. Click "Execute Request"
4. Go back to Message Flow to see the results

#### Option B: Use cURL

```bash
# Submit a test order to the demo integration
curl -X POST "http://localhost:PORT/api/demo/fulfillment/submit" \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "TEST-001",
    "customerName": "John Doe",
    "customerEmail": "john@example.com",
    "orderDate": "2026-01-11T10:00:00Z",
    "totalAmount": 99.99,
    "currency": "USD",
    "items": [{
      "sku": "PROD-001",
      "productName": "Test Product",
      "quantity": 1,
      "unitPrice": 99.99
    }],
    "shippingAddress": {
      "street": "123 Main St",
      "city": "Seattle",
      "state": "WA",
      "postalCode": "98101",
      "country": "USA"
    },
    "priority": "STANDARD"
  }'
```

Replace `PORT` with the actual port from Aspire Dashboard.

## Expected Results

### Message Flow Page

- **Timeline View**: Shows messages in chronological order with status indicators
- **Pipeline View**: Displays Input → Transform → Output pipeline stages
- **Color coding**:
  - Green = Success
  - Red = Failed
  - Yellow = Pending
- **Interactive buttons**: "View Details", "Compare Input/Output"
- **Auto-refresh**: Updates every 5 seconds when enabled

### Statistics Page

- **Metrics Cards**:
  - Total Transformations
  - Success Rate (percentage)
  - Average Processing Time (ms)
  - Messages in Last Hour

- **Charts**:
  - **Line Chart**: Message volume over time
  - **Donut Chart**: Success vs Failed distribution
  - **Bar Chart**: Processing time distribution

- **Integration Performance Table**: Shows stats per integration

## Troubleshooting

### No Messages Displayed

**Cause**: No demo data has been generated yet.

**Solution**:
1. Check if demo mode is enabled (should see banner at top)
2. Run the Demo Runner to generate test data
3. Check Management API logs for errors

### "Error loading messages" Notification

**Cause**: API endpoint not responding or returning errors.

**Solutions**:
1. Check Aspire Dashboard - ensure Management API is healthy
2. Check browser console for detailed error messages
3. Verify Management API URL in appsettings.json
4. Ensure Management API has started before Designer Web

### Charts Not Rendering

**Cause**: MudBlazor Charts library issue or no data.

**Solutions**:
1. Ensure there's data in the time range selected
2. Try selecting a different time range (24h instead of 1h)
3. Check browser console for JavaScript errors

### Auto-refresh Not Working

**Cause**: Timer disposed or not started.

**Solutions**:
1. Toggle auto-refresh off and on again
2. Manually click Refresh button
3. Reload the page

## Verification Checklist

- [ ] Build succeeds with 0 errors
- [ ] Management API starts successfully
- [ ] Designer Web starts successfully
- [ ] `/demo/flow` page loads without errors
- [ ] `/demo/statistics` page loads without errors
- [ ] Can filter by integration
- [ ] Can switch between Timeline/Pipeline views
- [ ] Charts render with sample data
- [ ] Auto-refresh toggles work
- [ ] Click "View Details" opens detail dialog
- [ ] Integration performance table displays correctly

## Files Modified

- `src/QuickApiMapper.Management.Api/Controllers/MessagesController.cs`

## Additional Notes

- The fix is backward compatible - existing API consumers will still work
- DTOs provide a stable contract between API and client
- The mapping layer allows domain models to evolve independently
- Consider adding AutoMapper for more complex mapping scenarios in the future

---

**Status**: ✅ Fix Applied - Ready for Testing
**Date**: 2026-01-11
**Impact**: Critical - Enables key demo features
