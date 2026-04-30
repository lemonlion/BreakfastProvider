param
(
    [Parameter(Mandatory = $true)][string] $jobStatus,
    [Parameter(Mandatory = $true)][string] $webhookUrl,
    [Parameter(Mandatory = $false)][string] $gitRepo = $env:GITHUB_REPOSITORY,
    [Parameter(Mandatory = $false)][string] $gitBaseUrl = $env:GITHUB_SERVER_URL,
    [Parameter(Mandatory = $false)][string] $gitWorkflow = $env:GITHUB_WORKFLOW,
    [Parameter(Mandatory = $false)][string] $gitRunId = $env:GITHUB_RUN_ID,
    [Parameter(Mandatory = $false)][string] $gitJob = $env:GITHUB_JOB,
    [Parameter(Mandatory = $false)][string] $gitUser = $env:GITHUB_ACTOR,
    [Parameter(Mandatory = $false)][string] $gitBranch = $env:GITHUB_REF_NAME,
    [Parameter(Mandatory = $false)][string] $gitRunAttempt = $env:GITHUB_RUN_ATTEMPT
)

$ErrorActionPreference = "Stop"

$statusData = @{
    failure = @{ text = 'failed'; colour = 'FF0000'; icon = '&#x274C;' }
    success = @{ text = 'passed'; colour = '00FF00'; icon = '&#x2705;' }
    warning = @{ text = 'passed with warnings'; colour = 'FFA500'; icon = '&#x1F536;' }
}

function GetStatusData {
    $status = $statusData[$jobStatus]

    return $( if ($null -ne $status) { $status } else { $statusData.warning }  )
}

function GetPayload {
    $status = GetStatusData
    $gitRepoUrl = "${gitBaseUrl}/${gitRepo}"
    $workflowUrl = "${gitRepoUrl}/actions/runs/${gitRunId}"
    $attemptSuffix = if ($gitRunAttempt -gt 1) { " (#${gitRunAttempt})" } else { "" }

    $title = "$($status.icon) **${gitWorkflow}** > **${gitJob}** $($status.text) in [${gitRepo}${attemptSuffix} &#x2197;](${workflowUrl})"
    $messageBody = "Branch: ``${gitBranch}`` | User: ``${gitUser}``"

    return @{
        type        = "MessageCard"
        attachments = @(
            @{
                contentType = "application/vnd.microsoft.card.adaptive"
                contentUrl  = "https://google.com"
                content     = @{
                    type            = "AdaptiveCard"
                    '$schema'       = "http://adaptivecards.io/schemas/adaptive-card.json"
                    version         = "1.5"
                    msteams         = @{
                        width = "Full"
                    }
                    backgroundImage = @{
                        url      = "https://singlecolorimage.com/get/$($status.colour)/10x6"
                        fillMode = "RepeatHorizontally"
                    }
                    body            = @(
                        @{
                            type   = "TextBlock"
                            size   = "medium"
                            weight = "bolder"
                            text   = $title
                            style  = "heading"
                            wrap   = $true
                        }
                        @{
                            type = "TextBlock"
                            text = $messageBody
                            wrap = $true
                        }
                    )
                }
            }
        )
    }
}

function PostToTeams {
    param($payload)

    $body = $payload | ConvertTo-Json -Depth 100

    Write-Host "Invoking Teams webhook with body:"
    Write-Host $body

    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-RestMethod `
        -Method POST `
        -Uri $webhookUrl `
        -Body $body `
        -ContentType 'application/json; charset=utf-8' `
        -TimeoutSec 30 | Out-Null
}

function Main {
    $payload = GetPayload
    PostToTeams -payload $payload
}

Main
