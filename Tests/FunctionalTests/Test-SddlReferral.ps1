$global:ApiPassed = $true

function InvokeWebRequest
{
    param (
        [string]$RequestUrl,
        [string]$Method,
        [string]$Body,
        $Headers,
        [switch]$HandleRedirection
    )
    
    if ($global:ApiPassed -eq $false)
    {
        return $null
    }

    Write-Host "Requesting URL: "$RequestUrl

    try
    {
        if ($handleRedirection)
        {
            $r = Invoke-WebRequest -Uri $RequestUrl -Method $method -Headers $headers -MaximumRedirection 0  -ErrorAction SilentlyContinue
        }
        elseif ($body -ne "")
        {
            $r = Invoke-WebRequest -Uri $RequestUrl -Method $method -Body $body -Headers $headers   
        }
        else
        {
            $r = Invoke-WebRequest -Uri $RequestUrl -Method $method -Headers $headers   
        }
        $r
    }
    catch
    {
        $global:ApiPassed = $false

        if ($_.Exception.Response -is [System.Net.HttpWebResponse])
        {
            $stream = $_.Exception.Response.GetResponseStream()
            $stream.Seek(0, 0)
            $reader = New-Object System.IO.StreamReader($stream)
            $responseBody = $reader.ReadToEnd()
            
            Write-Host $_.Exception.Message
            Write-Host "Response Body: $($responseBody)"
            
            $responseBody
        }
        else
        {
            Write-Host "Error without a specific HTTP response body: $($_.Exception.Message)"
        }
    }

}

$token_referrer = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySWQiOiJ0ZXN0dXNlcklkIiwic3ViIjoiMTIzIiwibmFtZSI6InRlc3R1c2VyIiwiZW1haWwiOiJ0ZXN0QGV4YW1wbGUuY29tIiwicm9sZSI6IkFkbWluIn0.Kof-ftiTIyK44VZdwKCh5IpVaIfhP3M79huv5OTSdYU"
$token_referee = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySWQiOiJ0ZXN0dXNlcklkIiwic3ViIjoicmVmZXJlZWlkIiwibmFtZSI6InRlc3R1c2VyIiwiZW1haWwiOiJ0ZXN0QGV4YW1wbGUuY29tIiwicm9sZSI6IkFkbWluIn0.npL6LgnyEDzjaAbJw0pRn9tw2vfbv3mDRDlZbvLLrAM"
$token_unauth = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYWRtaW4iOnRydWUsImlhdCI6MTUxNjIzOTAyMn0.3a11HOLS81EpF1X16MkbhU_MxM1hgZ6YIGTjpJD6o80"

$headers_referrer = @{"Authorization" = "Bearer "+$token_referrer; "Content-Type" = "application/json"; "user-agent" = "Android"}  
$headers_referee = @{"Authorization" = "Bearer "+$token_referee; "Content-Type" = "application/json"; "user-agent" = "Android"}  
$headers_unauth = @{"Authorization" = "Bearer "+$token_unauth; "Content-Type" = "application/json"; "user-agent" = "Android"}  
$headers_noauth = @{"Content-Type" = "application/json"; "user-agent" = "Android"}  

$referralCode = "REFCODE"

$referral = @{ "ReferralCode" = $referralCode; }
$body = ConvertTo-Json $referral

$hostSegment = "http://localhost:5259"
#$hostSegment = "https://localhost:44381"

$androidAppLocation = "https://play.google.com/store/apps/details?id=com.cartoncaps.package"
$iOsAppLocation = "https://apps.apple.com/app/id123456789"

function New-Referral
{
    param ($requestBody = $null, $requestHeaders = $null)

    if ($requestBody -eq $null)
    {
        $requestBody = $body
    }

    if ($requestHeaders -eq $null)
    {
        $requestHeaders = $headers_referrer;
    }

    $referralurl = $hostSegment+"/newreferral";

    $newReferralResult = InvokeWebRequest -RequestUrl $referralurl -Method 'POST' -Body $requestBody -Headers $requestHeaders
    
    if ($newReferralResult -is [Microsoft.PowerShell.Commands.WebResponseObject])
    {
        $newReferralResult.Content | ConvertFrom-Json
    }
    else
    {
        $newReferralResult
    }
}

function Download-App 
{
    param ($referralId)

    $downloadAppUurl = $hostSegment + "/download/" + $referralId;

    $r = InvokeWebRequest -RequestUrl $downloadAppUurl -Method 'GET' -HandleRedirection -Headers $headers_noauth
    
    if ($r -ne $null)
    {
        Write-Host $r.StatusCode
        Write-Host $r.StatusDescription
        Write-Host $r.Headers["Set-Cookie"]
        Write-Host $r.Headers["Location"]

        $r
    }
}

function Validate-Referral
{
    param ($fpId)

    $validateReferralUrl = $hostSegment + "/ValidateReferral/" + $fpId

    $r = InvokeWebRequest -RequestUrl $validateReferralUrl -Method 'GET' -Headers $headers_noauth

    if ($r -ne $null)
    {
        $r.Content | ConvertFrom-Json 
    }
}

function Complete-Referral
{
    param ($referralId)

    $validateReferralUrl = $hostSegment + "/CompleteReferral/" + $referralId

    $r = InvokeWebRequest -RequestUrl $validateReferralUrl -Method 'PUT' -Headers $headers_referee
    
    if ($r -is [Microsoft.PowerShell.Commands.WebResponseObject])
    {
        $r.Content | ConvertFrom-Json
    }
    else
    {
        $r
    }
}

function Get-CompletedReferrals
{
    $getCompletedReferralUrl = $hostSegment+"/referrals?`$filter=status eq 'Completed'";

    $r = InvokeWebRequest -RequestUrl $getCompletedReferralUrl -Method 'GET' -Headers $headers_referrer

    if ($r -ne $null)
    {
        ($r.Content | ConvertFrom-Json).Value
    }
}

function Get-AllUserScopedReferrals
{
    $getCompletedReferralUrl = $hostSegment+"/referrals";

    $r = InvokeWebRequest -RequestUrl $getCompletedReferralUrl -Method 'GET' -Headers $headers_referrer

    if ($r -ne $null)
    {
        ($r.Content | ConvertFrom-Json).Value
    }
}

function Test-DataFlow
{
    Write-Host "Test-DataFlow" -ForegroundColor Green
    
    $global:ApiPassed = $true
    
    "1. Creating new referral"
    $newrelerral = New-Referral
    $newrelerral

    if ($global:ApiPassed -eq $false -or $newrelerral.ReferralCode -ne $referralCode -or $newrelerral.ReferrerUserId -ne "123" -or ($newrelerral.Status -ne 2 -and $newrelerral.Status -ne 'Pending'))
    {
        Write-Host "Test-DataFlow FAILED" -ForegroundColor Red
        return
    }

    "2. Downloading app with referral id = " + $newrelerral.ReferralId
    $redirection = Download-App $newrelerral.ReferralId
    
    if ($redirection.Headers.ContainsKey("Set-Cookie"))
    {
        $t = $redirection.Headers["Set-Cookie"] -split ";"
    
        if ($t.Count -gt 0 -and $t[0] -match 'fpId=([0-9A-Fa-f-]{36})')
        {
            $fpid = [Guid]::Parse($matches[1])
        }
    }

    if ($global:ApiPassed -eq $false -or $redirection.StatusCode -ne 302 -or $redirection.Headers["Location"] -ne $androidAppLocation)
    {
        Write-Host "Test-DataFlow FAILED" -ForegroundColor Red
        return
    }

    "3. Validating fpId = " + $fpId
    $appDownloadEntry = Validate-Referral -fpId $fpId.Guid
    $appDownloadEntry

    if ($global:ApiPassed -eq $false -or $appDownloadEntry.fpId -ne $fpId.Guid -or $appDownloadEntry.userAgent -ne "Android" -or $appDownloadEntry.referralId -ne $newrelerral.ReferralId -or $appDownloadEntry.referralCode -ne $newrelerral.ReferralCode)
    {
        Write-Host "Test-DataFlow FAILED" -ForegroundColor Red
        return
    }


    "4. Completing referral id = " + $appDownloadEntry.ReferralId
    $completedReferral = Complete-Referral $appDownloadEntry.ReferralId
    $completedReferral

    if ($global:ApiPassed -eq $false -or $completedReferral.ReferralCode -ne $referralCode -or $completedReferral.referralId -ne $appDownloadEntry.ReferralId -or $completedReferral.ReferrerUserId -ne "123" -or $completedReferral.refereeUserId -ne "refereeid" -or ($completedReferral.Status -ne 1 -and $completedReferral.Status -ne 'Completed'))
    {
        Write-Host "Test-DataFlow FAILED" -ForegroundColor Red
        return
    }

    "5. Getting completed referrals"
    $completedReferralsList = Get-CompletedReferrals
    $completedReferralsList

    $completedOne = $completedReferralsList | ? { $_.Id -eq $completedReferral.Id  }

    if ($global:ApiPassed -eq $false -or $completedOne -eq $null)
    {
        Write-Host "Test-DataFlow FAILED" -ForegroundColor Red
        return
    }

    Write-Host "Test-DataFlow PASSED" -ForegroundColor Green
}

function Test-InvalidRequestBodyInReferral
{
    Write-Host "Test-InvalidRequestBodyInReferral" -ForegroundColor Green

    $emptybody = '{}'
    $newrelerral = New-Referral -requestBody $emptybody

    Write-Host "Test-InvalidRequestBodyInReferral" -ForegroundColor Green
}

function Test-NewReferralWithUnauthToken
{
    Write-Host "Test-NewReferralWithUnauthToken" -ForegroundColor Green

    $newrelerral = New-Referral -requestHeaders $headers_unauth

    Write-Host "Test-NewReferralWithUnauthToken" -ForegroundColor Green
}

function Test-GetAllUserScopedReferrals
{
    Write-Host "Test-GetAllUserScopedReferrals" -ForegroundColor Green

    Get-AllUserScopedReferrals

    Write-Host "Test-GetAllUserScopedReferrals PASSED" -ForegroundColor Green
}

function Test-CompleteReferralsWithInvalidId
{
    Write-Host "Test-CompleteReferralsWithInvalidId" -ForegroundColor Green

    Complete-Referral -referralId "123"

    Write-Host "Test-CompleteReferralsWithInvalidId PASSED" -ForegroundColor Green
}

Test-DataFlow
#Test-InvalidRequestBodyInReferral
#Test-NewReferralWithUnauthToken
#Test-GetAllUserScopedReferrals
#Test-CompleteReferralsWithInvalidId