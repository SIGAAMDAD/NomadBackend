. "$PSScriptRoot/common.ps1"

Write-NomadSection "Format Check"

Assert-NomadPathExists $Script:SolutionPath "Solution"

Invoke-NomadCommand $Script:DotNet @(
	"format",
	$Script:SolutionPath,
	"whitespace",
	"--verify-no-changes"
)

#Invoke-NomadCommand $Script:DotNet @(
#	"format",
#	$Script:SolutionPath,
#	"style",
#	"--verify-no-changes"
#)

#Invoke-NomadCommand $Script:DotNet @(
#	"format",
#	$Script:SolutionPath,
#	"analyzers",
#	"--verify-no-changes"
#)