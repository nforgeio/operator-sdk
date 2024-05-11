#Requires -Version 7.1.3 -RunAsAdministrator
#------------------------------------------------------------------------------
# FILE:         publish-dev.ps1
# CONTRIBUTOR:  NeonFORGE Team
# COPYRIGHT:    Copyright © 2005-2024 by NEONFORGE LLC.  All rights reserved.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

#
# USAGE: pwsh -f neonsdk-nuget-dev.ps1 [OPTIONS]
#
# OPTIONS:
#
#       -dirty      - Use GitHub sources for SourceLink even if local repo is dirty
#       -restore    - Just restore the CSPROJ files after cancelling publish
#
# REMARKS:
#
# NOTE: The script writes the package publication version to:
#
#           $/build/nuget/version.txt
#

param 
(
    [switch]$dirty   = $false,
    [switch]$restore = $false
)

# Import the global solution include file.

. $env:NK_ROOT/Powershell/includes.ps1

# Abort if Visual Studio is running because that can lead to 
# build configuration conflicts because this script builds the
# RELEASE configuration and we normally have VS in DEBUG mode.


# Verify that the user has the required environment variables.  These will
# be available only for maintainers and are intialized by the NeonCLOUD
# [buildenv.cmd] script.

if (!(Test-Path env:NC_ROOT))
{
    "*** ERROR: This script is intended for maintainers only:"
    "           [NC_ROOT] environment variable is not defined."
    ""
    "           Maintainers should re-run the NeonCLOUD [buildenv.cmd] script."

    return 1
}

# Retrieve any necessary credentials.

$nugetApiKey = Get-SecretPassword "GITHUB_PAT"

#------------------------------------------------------------------------------
# Builds and publishes the project packages.

function Publish
{
    [CmdletBinding()]
    param (
        [Parameter(Position=0, Mandatory=$true)]
        [string]$project,
        [Parameter(Position=1, Mandatory=$true)]
        [string]$version
    )

    $projectPath = [io.path]::combine($env:NO_ROOT, "src", "$project", "$project" + ".csproj")

    # Disabling symbol packages now that we're embedding PDB files.
    #
    # dotnet pack $projectPath -c Release -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg -o "$env:NO_BUILD\nuget"

    dotnet pack $projectPath -c Release -o "$env:NO_BUILD\nuget" -p:SolutionName=$env:SolutionName -p:PackageVersion=$version -p:NeonBuildUseNugets=true
    ThrowOnExitCode

    dotnet nuget push "$env:NO_BUILD\nuget\$project.$version.nupkg" --api-key $nugetApiKey --source https://nuget.pkg.github.com/nforgeio/index.json --skip-duplicate
    ThrowOnExitCode
}

try
{
    if ([System.String]::IsNullOrEmpty($env:SolutionName))
    {
        $env:SolutionName = "neonKUBE"
    }

    $msbuild             = $env:MSBUILDPATH
    $neonBuild           = "$env:NF_ROOT\ToolBin\neon-build\neon-build.exe"
    $config              = "Release"
    $nfRoot              = "$env:NF_ROOT"
    $noRoot              = "$env:NO_ROOT"
    $noBuild             = "$env:NO_BUILD"
    $nfLib               = "$nfRoot\Lib"
    $neonSdkVersion      = $(& "neon-build" read-version "$nfLib/Neon.Common/Build.cs" NeonSdkVersion)
    $neonOperatorVersion = "$(Get-Date -format yyyy.%M.%d)-$([int](Get-Date -UFormat %s -Second 0))"

    #------------------------------------------------------------------------------
    # Save the publish version to [$/build/nuget/version.text] so release tools can
    # determine the current release.

    [System.IO.Directory]::CreateDirectory("$nkRoot\build\nuget") | Out-Null
    [System.IO.File]::WriteAllText("$nkRoot\build\nuget\version.txt", $neonkubeVersion)

    #--------------------------------------------------------------------------
    # SourceLink configuration:
	#
	# We're going to fail this when the current git branch is dirty 
	# and [-dirty] wasn't passed.

    $gitDirty = IsGitDirty

    if ($gitDirty -and -not $dirty)
    {
        throw "Cannot publish nugets because the git branch is dirty.  Use the [-dirty] option to override."
    }

    $env:NEON_PUBLIC_SOURCELINK = "true"

    #--------------------------------------------------------------------------
    # We need to do a release solution build to ensure that any tools or other
    # dependencies are built before we build and publish the individual packages.

    if (-not $restore)
    {
        #------------------------------------------------------------------------------
        # Build and publish the projects.

        Write-Info "OperatorSdkVersion: $neonOperatorVersion"
        Write-Info "NeonSdkVersion: $neonSdkVersion"

        Publish -project Neon.Kubernetes                        -version $neonOperatorVersion
        Publish -project Neon.Kubernetes.Core                   -version $neonOperatorVersion
        Publish -project Neon.Kubernetes.Resources              -version $neonOperatorVersion
        Publish -project Neon.Operator                          -version $neonOperatorVersion
        Publish -project Neon.Operator.Analyzers                -version $neonOperatorVersion
        Publish -project Neon.Operator.Core                     -version $neonOperatorVersion
        Publish -project Neon.Operator.OperatorLifecycleManager -version $neonOperatorVersion
        Publish -project Neon.Operator.Templates                -version $neonOperatorVersion
        Publish -project Neon.Operator.Xunit                    -version $neonOperatorVersion
    }

    #------------------------------------------------------------------------------
    # Remove all of the generated nuget files so these don't accumulate.

    Remove-Item "$env:NO_BUILD\nuget\*.nupkg"

    ""
    "** Package publication completed"
    ""
}
catch
{
    Write-Exception $_
    exit 1
}
