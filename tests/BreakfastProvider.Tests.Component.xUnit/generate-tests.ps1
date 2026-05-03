<#
.SYNOPSIS
Generates plain xUnit3 test files from LightBDD source files.
Reads each .cs + .steps.cs pair and produces a merged xUnit test class.
#>

$srcRoot = "c:\git\BreakfastProvider\tests\BreakfastProvider.Tests.Component.LightBDD\Scenarios"
$dstRoot = "c:\git\BreakfastProvider\tests\BreakfastProvider.Tests.Component.xUnit\Scenarios"

# Get all scenario definition files (not .steps.cs)
$scenarioFiles = Get-ChildItem $srcRoot -Filter "*.cs" -Recurse | Where-Object { $_.Name -notmatch '\.steps\.cs$' }

foreach ($scenarioFile in $scenarioFiles) {
    $stepsFile = Join-Path $scenarioFile.DirectoryName ($scenarioFile.BaseName + ".steps.cs")
    $scenarioContent = Get-Content $scenarioFile.FullName -Raw
    $stepsContent = if (Test-Path $stepsFile) { Get-Content $stepsFile -Raw } else { "" }
    
    # Get relative path from srcRoot
    $relDir = $scenarioFile.DirectoryName.Substring($srcRoot.Length)
    $outDir = Join-Path $dstRoot $relDir
    if (!(Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }
    
    # Determine class name - strip the double underscore feature convention
    $className = $scenarioFile.BaseName
    
    # Extract namespace from scenario content
    $nsMatch = [regex]::Match($scenarioContent, 'namespace\s+([\w.]+)')
    $oldNs = $nsMatch.Groups[1].Value
    $newNs = $oldNs -replace 'BreakfastProvider\.Tests\.Component\.LightBDD', 'BreakfastProvider.Tests.Component.xUnit'
    
    # Collect all usings from both files
    $allUsings = @{}
    foreach ($content in @($scenarioContent, $stepsContent)) {
        $usingMatches = [regex]::Matches($content, '(?m)^using\s+[^;]+;')
        foreach ($u in $usingMatches) {
            $usingLine = $u.Value.Trim()
            # Skip LightBDD-specific usings
            if ($usingLine -match 'LightBDD\.' -or 
                $usingLine -match 'TestTrackingDiagrams\.LightBDD' -or
                $usingLine -match 'BreakfastProvider\.Tests\.Component\.LightBDD\.Util') {
                continue
            }
            # Remap the LightBDD infrastructure namespace
            $usingLine = $usingLine -replace 'BreakfastProvider\.Tests\.Component\.LightBDD', 'BreakfastProvider.Tests.Component.xUnit'
            $allUsings[$usingLine] = $true
        }
    }
    
    # Add TTD xUnit3 using if needed (for HappyPath attribute)
    if ($scenarioContent -match '\[HappyPath\]') {
        $allUsings['using TestTrackingDiagrams.xUnit3;'] = $true
    }
    
    # Determine feature description
    $featureDescMatch = [regex]::Match($scenarioContent, '\[FeatureDescription\(([^)]+)\)\]')
    $featureDesc = ""
    if ($featureDescMatch.Success) {
        $featureDesc = $featureDescMatch.Groups[1].Value
    }
    
    # Build the step methods from steps file
    $stepMethods = ""
    if ($stepsContent) {
        # Extract the class body from steps file (everything inside the class braces)
        $classBodyMatch = [regex]::Match($stepsContent, '(?s)public\s+partial\s+class\s+\w+\s*:\s*\w+[^{]*\{(.+)\}[\s]*\}[\s]*$')
        if ($classBodyMatch.Success) {
            $stepMethods = $classBodyMatch.Groups[1].Value
            
            # Remove CompositeStep return types and Sub.Steps patterns
            # Replace async Task<CompositeStep> with async Task
            $stepMethods = $stepMethods -replace 'async\s+Task<CompositeStep>', 'async Task'
            
            # Replace Sub.Steps(...) patterns with sequential calls
            # This is complex - we'll handle it differently
            
            # Remove [SkipStepIf] attributes and add if-guards instead
            $stepMethods = [regex]::Replace($stepMethods, 
                '\s*\[SkipStepIf\(nameof\(Settings\.(\w+)\),\s*([^)]+)\)\]\s*\n',
                "`n")
            
            # Remove InputTable<T> parameter types
            $stepMethods = $stepMethods -replace 'InputTable<(\w+)>\s+(\w+)', 'List<$1> $2'
            
            # Remove VerifiableDataTable usage
            $stepMethods = $stepMethods -replace 'VerifiableDataTable<(\w+)>\s+(\w+)', 'List<$1> $2'
            
            # Remove #pragma disable
            $stepMethods = $stepMethods -replace '#pragma\s+warning\s+disable\s+\w+\s*//[^\n]*\n', ''
            $stepMethods = $stepMethods -replace '#pragma\s+warning\s+disable\s+\w+\s*\n', ''
        }
    }
    
    Write-Host "Processing: $($scenarioFile.Name) -> $outDir\$className.cs"
}

Write-Host "`nDone - processed $($scenarioFiles.Count) feature files"
