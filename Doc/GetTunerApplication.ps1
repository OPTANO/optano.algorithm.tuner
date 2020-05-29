$basepath =  "./optano.algorithm.tuner.examples/Source/Tuner.Application/Optano.Algorithm.Tuner.Application/bin/Release/netcoreapp3.1/*"
$targetpath = "./optano.algorithm.tuner/Doc/_site/"
$targetfilename = "OPTANO.Tuner.Application.zip"

If(!(test-path $targetpath))
{
      New-Item -ItemType Directory -Path $targetpath
}


Remove-Item $targetpath$targetfilename -ErrorAction Ignore
Get-Childitem $basepath -Exclude *.pdb | Compress-Archive -DestinationPath $targetpath$targetfilename -CompressionLevel Optimal