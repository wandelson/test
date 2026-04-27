# Simple Load Test for Hacker News API
# Run this script to simulate concurrent requests to /api/stories/10

param(
    [int]$ConcurrentRequests = 10,
    [int]$TotalRequests = 100,
    [string]$Url = "http://localhost:5000/api/stories/10"
)

Write-Host "Starting load test with $ConcurrentRequests concurrent requests, total $TotalRequests requests to $Url"

$jobs = @()
$startTime = Get-Date

for ($i = 0; $i -lt $ConcurrentRequests; $i++) {
    $jobs += Start-Job -ScriptBlock {
        param($url, $requests)
        $results = @()
        for ($j = 0; $j -lt $requests; $j++) {
            try {
                $response = Invoke-WebRequest -Uri $url -Method GET -TimeoutSec 10
                $results += @{
                    StatusCode = $response.StatusCode
                    Time = (Get-Date) - $using:startTime
                    Success = $true
                }
            } catch {
                $results += @{
                    StatusCode = $_.Exception.Response.StatusCode
                    Time = (Get-Date) - $using:startTime
                    Success = $false
                }
            }
        }
        return $results
    } -ArgumentList $Url, ($TotalRequests / $ConcurrentRequests)
}

$allResults = @()
foreach ($job in $jobs) {
    $allResults += Receive-Job -Job $job -Wait
    Remove-Job -Job $job
}

$endTime = Get-Date
$totalTime = $endTime - $startTime

$successCount = ($allResults | Where-Object { $_.Success }).Count
$failureCount = $allResults.Count - $successCount

Write-Host "Load test completed in $($totalTime.TotalSeconds) seconds"
Write-Host "Total requests: $($allResults.Count)"
Write-Host "Successful: $successCount"
Write-Host "Failed: $failureCount"
Write-Host "Success rate: $([math]::Round($successCount / $allResults.Count * 100, 2))%"

if ($successCount -gt 0) {
    $avgResponseTime = ($allResults | Where-Object { $_.Success } | Measure-Object -Property Time.TotalMilliseconds -Average).Average
    Write-Host "Average response time: $([math]::Round($avgResponseTime, 2)) ms"
}