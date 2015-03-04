param(
    [Parameter(Mandatory=$true)]
    [string] $projectFile,
    [Parameter(Mandatory=$true)]
    [string] $server,
    [string] $serverFolder="dev",
    [string] $userName,
    [string] $password
)

function Get-ScriptDirectory 
{ 
    $Invocation = (Get-Variable MyInvocation -Scope 1).Value 
    Split-Path $Invocation.MyCommand.Path 
} 

function UpdateHostInProjectJson($projectFile, $newhost)
{
    (Get-Content $projectFile) | 
    Foreach-Object {
        $_ -replace "http://localhost", "http://$newhost"
    } |
    Set-Content $projectFile 
}
 
if (-not (Test-Path $projectFile)) {
    Write-Error "Couldn't find $projectFile"
    exit 1
}

$projectName = (get-item $projectFile).Directory.Name
$workDir = (get-item $projectFile).Directory.FullName
$remoteRoot="\\" + $server
$remoteDir= Join-Path $remoteRoot -ChildPath $serverFolder

try
{
    if ($userName) {
        net use $remoteRoot $password /USER:$userName
        if ($lastexitcode -ne 0) {
            exit 1
        }
    }

    if (-not (Test-Path $remoteDir)) {
        Write-Error "Remote directory $remoteDir does not exist or it is not accessible"
        exit 1
    }

    $packDir = Join-Path $workDir -ChildPath "bin\output"
    if (Test-Path $packDir) {
        Write-Host "$packDir already exists. Removing it..."
        rmdir -Recurse "$packDir"
    }

    Write-Host "Bundling the application..."
    cd "$workDir"
    kvm use -r CoreCLR -arch x64
    kpm bundle --runtime active
    if ($lastexitcode -ne 0) {
        Write-Error "Failed to bundle the application"
        exit 1
    }

    $packedProjectJsonFile = Join-Path $packDir -ChildPath "approot\src\$projectName\project.json"
    Write-Host "Setting host to $server in $packedProjectJsonFile"
    if (-not (Test-Path $packedProjectJsonFile)) {
        Write-Error "Couldn't find $packedProjectJsonFile"
        exit 1
    }
    UpdateHostInProjectJson $packedProjectJsonFile $server

    $destDir = Join-Path $remoteDir -ChildPath $projectName
    if (Test-Path $destDir) {
      Write-Host "$destDir already exists. Removing it..."
      rmdir -Recurse "$destDir"
    }

    Write-Host "Copying bundled application to $destDir ..."
    Copy-Item "$packDir" -Destination "$destDir" -Recurse
    if ($lastexitcode -ne 0) {
        Write-Error "Failed to copy the bundled application"
        exit 1
    }
}
finally
{
    if ($userName -and $remoteRoot) {
        net use $remoteRoot /delete
    }
}