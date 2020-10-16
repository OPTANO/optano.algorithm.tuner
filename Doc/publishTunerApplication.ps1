Param(
  [string]$versionnumber =""
)

$basepath =  "./optano.algorithm.tuner.examples/Source/Tuner.Application/Optano.Algorithm.Tuner.Application/bin/Publish/"
$targetpath = "./optano.algorithm.tuner/Doc/_site/"
$targetfilename = "OPTANO.Algorithm.Tuner.Application"
$osversions = @("win-x64","linux-x64","osx-x64")

# Remove old base folder, if existing.
If(test-path $basepath)
{
      Remove-Item -Recurse -Force -ErrorAction Ignore $basepath
}

# Create new target folder, if not existing. 
If(!(test-path $targetpath))
{
      New-Item -ItemType Directory -Path $targetpath
}

# Publish and zip OPTANO.Algorithm.Tuner.Application.dll for every OS.
foreach ($currentosversion in $osversions)
{
	dotnet publish "./optano.algorithm.tuner.examples/Source/Tuner.Application/Optano.Algorithm.Tuner.Application/Optano.Algorithm.Tuner.Application.csproj" --self-contained true -r $currentosversion -c Release -o $basepath$currentosversion
	$targetfile = $targetfilename + "." + $versionnumber + "_" + $currentosversion + ".zip"
	Remove-Item $targetpath$targetfile -ErrorAction Ignore
	Get-Childitem $basepath$currentosversion -Exclude *.pdb | Compress-Archive -DestinationPath $targetpath$targetfile -CompressionLevel Optimal
}

# Replace corresponding links in documentation.
(Get-Content "./optano.algorithm.tuner/Doc/download.md").replace("{VERSIONNUMBER}", $versionnumber) | Set-Content "./optano.algorithm.tuner/Doc/download.md"