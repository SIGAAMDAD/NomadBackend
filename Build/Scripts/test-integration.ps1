. "$PSScriptRoot/common.ps1"

Write-NomadSection "Integration Tests"

Assert-NomadPathExists $Script:TestProjectPath "Test project"

$filter = if ( $env:NOMAD_INTEGRATION_TEST_FILTER ) {
	$env:NOMAD_INTEGRATION_TEST_FILTER
} else {
	"Category=Integration&Category!=Steam"
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
