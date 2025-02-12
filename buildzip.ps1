# BuildZip.ps1
# Path to the VersionUtility.cs file
$versionFile = "C:\Unity Projects\Oddinary-Farm\Assets\VersionUtility.cs"

# Read the entire file as a single string
$content = Get-Content $versionFile -Raw

# Use regex to extract version numbers
if ($content -match "MAJOR_VERSION\s*=\s*(\d+);") {
    $major = $matches[1]
}
else {
    Write-Error "Major version not found!"
    exit 1
}

if ($content -match "MINOR_VERSION\s*=\s*(\d+);") {
    $minor = $matches[1]
}
else {
    Write-Error "Minor version not found!"
    exit 1
}

if ($content -match "BUILD_VERSION\s*=\s*(\d+);") {
    $build = $matches[1]
}
else {
    Write-Error "Build version not found!"
    exit 1
}

# Construct the version string and the output zip file name
$versionString = "$major.$minor.$build"
$zipFileName = "Build $versionString.zip"

# Define source and destination paths
$sourceFolder = "C:\Unity Projects\Oddinary-Farm\Build"
$destinationZip = Join-Path "C:\Unity Projects\Oddinary-Farm" $zipFileName

# If a zip with the same name exists, remove it
if (Test-Path $destinationZip) {
    Remove-Item $destinationZip -Force
}

# Compress the Build folder into the zip file
Compress-Archive -Path $sourceFolder -DestinationPath $destinationZip

Write-Output "Created zip file: $destinationZip"
