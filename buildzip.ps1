param (
    # You can override this path when calling the script
    [string]$ProjectPath = "C:\Game Project\Serious Projects\Oddinary-Farm"
)

# Check if the project path exists
if (-not (Test-Path $ProjectPath)) {
    Write-Error "Project path not found: $ProjectPath"
    exit 1
}

# Define paths
$versionFile = Join-Path $ProjectPath "Assets\VersionUtility.cs"
$sourceFolder = Join-Path $ProjectPath "Build"

# Validate file and folder existence
if (-not (Test-Path $versionFile)) {
    Write-Error "Version file not found: $versionFile"
    exit 1
}

if (-not (Test-Path $sourceFolder)) {
    Write-Error "Build folder not found: $sourceFolder"
    exit 1
}

# Read the entire file as a single string
try {
    $content = Get-Content $versionFile -Raw
}
catch {
    Write-Error "Failed to read: $versionFile"
    exit 1
}

# Function to extract version numbers using regex
function Get-VersionNumber {
    param (
        [string]$Content,
        [string]$VersionName
    )
    $pattern = "$VersionName\s*=\s*(\d+);"
    if ($Content -match $pattern) {
        return $matches[1]
    }
    else {
        Write-Error "$VersionName not found in $versionFile!"
        exit 1
    }
}

# Extract version numbers
$major = Get-VersionNumber -Content $content -VersionName "MAJOR_VERSION"
$minor = Get-VersionNumber -Content $content -VersionName "MINOR_VERSION"
$build = Get-VersionNumber -Content $content -VersionName "BUILD_VERSION"

# Construct the version string and output zip file name
$versionString = "$major.$minor.$build"
$zipFileName = "Build $versionString.zip"
$destinationZip = Join-Path $ProjectPath $zipFileName

# If the zip file already exists, ask for user confirmation to overwrite
if (Test-Path $destinationZip) {
    $confirmation = Read-Host "A zip file with version $versionString already exists. Do you want to overwrite it? (Y/N)"
    if ($confirmation -ne "Y" -and $confirmation -ne "y") {
        Write-Output "Operation cancelled. Exiting script."
        exit 0
    }
    else {
        Remove-Item $destinationZip -Force
    }
}

# Compress the Build folder into the zip file
try {
    Compress-Archive -Path $sourceFolder -DestinationPath $destinationZip -Force
}
catch {
    Write-Error "Compression failed: $_"
    exit 1
}

Write-Output "Created zip file: $destinationZip"

# Keep the terminal open
Read-Host "Compression success. Press Enter to exit..."
exit 0
