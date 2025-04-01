
$global:InstanceId = ""
$global:CoreAPIBaseUrl = ""
$global:ManagementAPIBaseUrl = ""

$global:CoreAPIAccessToken = ""
$global:ManagementAPIAccessToken = ""
$global:ACADynamicSessionsAccessToken = ""

function Test-JWTExpired {
    param(
        [string]$jwt
    )
    try {
        if ($jwt -eq "") {
            return $true
        }
        $parts = $jwt.split(".")
        if ($parts.Length -ne 3) {
            Write-Warning "Invalid JWT format."
            return $true
        }
        $body = $parts[1]
        $decoded = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($body.Replace('-', '+').Replace('_', '/').PadRight(($body.Length / 4 * 4) + ($body.Length % 4 -eq 0 ? 0 : 4), '=')))
        $json = ConvertFrom-Json $decoded
        $expirationTime = [datetime]::UnixEpoch.AddSeconds($json.exp)
        $currentTime = [datetime]::UtcNow
        Write-Host "Current time: $currentTime | JWT expiration time: $expirationTime" -ForegroundColor Green
        return $expirationTime -lt $currentTime
    } catch {
        Write-Warning "Error decoding or parsing JWT: $($_.Exception.Message)"
        return $true
    }
}

function Get-ManagementAPIAccessToken {
    $accessToken = az account get-access-token `
        --scope "api://FoundationaLLM-Management/Data.Manage" `
        --output  tsv `
        --query accessToken
    return $accessToken
}

function Test-ManagementAPIAccessToken {
    if (Test-JWTExpired $global:ManagementAPIAccessToken) {
        $global:ManagementAPIAccessToken = Get-ManagementAPIAccessToken
    }
}

function Get-CoreAPIAccessToken {
    $accessToken = az account get-access-token `
        --scope "api://FoundationaLLM-Core/Data.Read" `
        --output  tsv `
        --query accessToken
    return $accessToken
}

function Test-CoreAPIAccessToken {
    if (Test-JWTExpired $global:CoreAPIAccessToken) {
        $global:CoreAPIAccessToken = Get-CoreAPIAccessToken
    }
}

function Get-ACADynamicSessionsAccessToken {
    $accessToken = az account get-access-token `
        --resource "https://dynamicsessions.io" `
        --output  tsv `
        --query accessToken
    return $accessToken
}

function Test-ACADynamicSessionsAccessToken {
    if (Test-JWTExpired $global:ACADynamicSessionsAccessToken) {
        $global:ACADynamicSessionsAccessToken = Get-ACADynamicSessionsAccessToken
    }
}

function Get-ManagementAPIBaseUri {
    return [System.Uri]::new([System.Uri]::new($ManagementAPIBaseUrl), "/instances/$($InstanceId)")
}

function Get-CoreAPIBaseUri {
    return [System.Uri]::new([System.Uri]::new($CoreAPIBaseUrl), "/instances/$($InstanceId)")
}

function Get-ObjectId {
    param (
        [string]$Name,
        [string]$Type
    )

    return "/instances/$($global:InstanceId)/providers/$($Type)/$($Name)"
}

function Get-ResourceObjectIds {
    param (
        [array]$Resources
    )

    $resourceObjectIds = [ordered]@{}
    $Resources | ForEach-Object {
        $resource = $_
        $resourceObjectId = (Get-ObjectId -Name $resource.name -Type $resource.type)
        $resourceObjectIds[$resourceObjectId] = [ordered]@{}
        $resourceObjectIds[$resourceObjectId]["object_id"] = $resourceObjectId
        $resourceObjectIds[$resourceObjectId]["properties"] = [ordered]@{}
        if ($resource.Contains("role") ){
            $resourceObjectIds[$resourceObjectId]["properties"]["object_role"] = $resource.role
        }
        if ($resource.Contains("properties") ){
            $resource["properties"] | ForEach-Object {
                $propertyList = $_
                $currentDict = $resourceObjectIds[$resourceObjectId]["properties"]
                for ($i = 0; $i -lt $propertyList.Length - 2; $i++) {
                    if ($currentDict.Contains($propertyList[$i])) {
                        $currentDict = $currentDict[$propertyList[$i]]
                    } else {
                        $currentDict[$propertyList[$i].ToString()] = [ordered]@{}
                        $currentDict = $currentDict[$propertyList[$i]]
                    }
                }
                $currentDict[$propertyList[$i].ToString()] = $propertyList[$i + 1]
            }
        }
    }

    return $resourceObjectIds
}

function Invoke-ManagementAPI {
    param (
        [string]$Method,
        [string]$RelativeUri,
        [hashtable]$Body = $null,
        [hashtable]$Headers = $null
    )

    Write-Host "Calling Management API:" -ForegroundColor Green

    Test-ManagementAPIAccessToken

    if ($Headers -eq $null) {
        $Headers = @{
            "Authorization" = "Bearer $($global:ManagementAPIAccessToken)"
            "Content-Type"  = "application/json"
        }
    } else {
        $Headers["Authorization"] = "Bearer $accessToken"
    }

    $baseUri = Get-ManagementAPIBaseUri

    $uri = "$($baseUri.AbsoluteUri)/$($RelativeUri)"

    Write-Host "$($Method) $($uri)" -ForegroundColor Green

    return Invoke-RestMethod `
        -Method $Method `
        -Uri  $uri `
        -Body ($Body | ConvertTo-Json -Depth 20) `
        -Headers $Headers
}