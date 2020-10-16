Param([string]$version)

((Get-Content -path optano.algorithm.tuner\Build\Optano.Algorithm.Tuner.nuspec -Raw) -replace 'VERSION_PLACEHOLDER',$version) | Set-Content -Path optano.algorithm.tuner\Build\Optano.Algorithm.Tuner.nuspec
((Get-Content -path optano.algorithm.tuner\Build\Optano.Algorithm.Tuner-debug.nuspec -Raw) -replace 'VERSION_PLACEHOLDER',$version) | Set-Content -Path optano.algorithm.tuner\Build\Optano.Algorithm.Tuner-debug.nuspec