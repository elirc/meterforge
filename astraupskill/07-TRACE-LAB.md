# 07 — Create, read, replay and reject a conflict

[Course home](README.md) · [Concepts](02-CONCEPTS.md) · [Verification](VERIFICATION.md)

Use a disposable local API process started with the command in the course README. These commands are a manual learning exercise; the automated API test covers the same behavior through ASP.NET's test host. The app uses synthetic data and in-memory storage by default.

## Prepare one logical action

In a second PowerShell terminal:

```powershell
$baseUri = 'http://127.0.0.1:5281'
$tenant = 'learning-' + [guid]::NewGuid().ToString('N')
$usageUri = "$baseUri/api/v1/tenants/$tenant/usage-events"
$usageRequest = @{
    subscriptionId = [guid]::NewGuid().ToString()
    meter = 'api_calls'
    quantity = 10
    occurredAt = '2026-09-08T12:00:00Z'
    externalEventId = 'lab-event-1'
}
$requestJson = $usageRequest | ConvertTo-Json
$created = Invoke-WebRequest -UseBasicParsing -Method Post -Uri $usageUri `
    -ContentType 'application/json' -Body $requestJson
$firstEvent = $created.Content | ConvertFrom-Json
$created.StatusCode
$firstEvent
```

Expected: 201, a nonempty `id`, quantity 10 and `isReplay: false`. The subscription GUID is synthetic. The current use case validates its shape but does not load a persisted subscription; adding that relationship check is an explicit practice task.

Pause and draw the path from the route to the dictionary. The candidate GUID is generated inside the application, while `externalEventId` came from the caller. Do not confuse the two when explaining the next step.

## Follow the server's Location

```powershell
$location = [string]$created.Headers['Location']
$resourceUri = $baseUri + $location
$readBack = Invoke-RestMethod -Method Get -Uri $resourceUri
$readBack.id -eq $firstEvent.id
$readBack.quantity
```

Expected: `True`, then 10. The returned Location is relative in this endpoint. For a general-purpose client, resolve relative and absolute locations against the request URI rather than concatenating blindly. This lab uses the known local endpoint contract.

If GET returns 404, first confirm you followed the returned path, used the same running host, and did not restart its memory store. Then inspect the route's tenant predicate. Do not begin by changing the repository until you know which request actually arrived.

## Replay without changing the business fact

```powershell
$replayed = Invoke-WebRequest -UseBasicParsing -Method Post -Uri $usageUri `
    -ContentType 'application/json' -Body $requestJson
$replayEvent = $replayed.Content | ConvertFrom-Json
$replayed.StatusCode
$replayEvent.id -eq $firstEvent.id
$replayEvent.isReplay
```

Expected: 200, `True`, `True`. The response status changed because no new resource was created. The business fact and its stored identity did not change.

Repeat after adding spaces around `externalEventId` and `meter` in a fresh request object. Predict whether trimming changes the outcome. Keep quantity, subscription and occurrence instant equal. Then try a different timezone representation of the same instant and explain why it is still the same business fact.

## Reuse the key with changed intent

```powershell
$usageRequest.quantity = 11
try {
    Invoke-WebRequest -UseBasicParsing -Method Post -Uri $usageUri `
        -ContentType 'application/json' -Body ($usageRequest | ConvertTo-Json)
} catch {
    $_.Exception.Response.StatusCode
    $_.ErrorDetails.Message
}
(Invoke-RestMethod -Method Get -Uri $resourceUri).quantity
```

Expected: a 409 conflict and the stable code `usage.idempotency_conflict`; the final quantity remains 10. PowerShell versions display HTTP errors differently. The integration test is the authoritative executable assertion for status/body/header values. Use a debugger or HTTP client to inspect the matching `X-Trace-Id` and Problem Details `traceId` if your terminal hides headers on an error.

The request correlation value comes from `HttpContext.TraceIdentifier`. This improvement makes the header and problem body agree; it does not install a distributed tracing backend.

## Two more predictions

1. Change the tenant in the URL and GET the original internal ID. Expect 404 because the lookup includes tenant. This demonstrates data scoping, not authenticated permission checking.
2. Restart the local API, then GET the original Location. Expect 404 because the default store was in memory. Retrying the POST after restart can create a new event even with the same external key, which is why this adapter cannot promise durable deduplication.

## Your evidence note

Write down the exact commands you ran, expected versus observed outcomes, and one limit exposed by the experiment. Explain a network-loss scenario in which the client sees an error but the server has already stored the event. Then explain how the same-key retry resolves that uncertainty while the process still has its state.

Finish by running the automated lifecycle test. A manual transcript helps you understand the feature; a repeatable regression helps the next engineer preserve it.
