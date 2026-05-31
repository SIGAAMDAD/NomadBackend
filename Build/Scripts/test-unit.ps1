. "$PSScriptRoot/common.ps1"

Write-NomadSection "Unit Tests"

Assert-NomadPathExists $Script:TestProjectPath "Test project"

$filter = if ( $env:NOMAD_TEST_FILTER ) {
	$env:NOMAD_TEST_FILTER
} else {
	"Category=Unit&Category!=Steam"
}

Invoke-NomadCommand $Script:DotNet @(
	"test",
	$Script:TestProjectPath,
	"--configuration",
	"Debug",
	"--filter",
	$filter,
	"--logger",
	"trx",
	"--results-directory",
	$Script:TestResultsDirectory
)
