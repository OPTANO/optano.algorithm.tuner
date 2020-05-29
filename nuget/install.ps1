param($installPath, $toolsPath, $package, $project)
$project.Object.References | Where-Object { $_.Name -eq 'Optimization.Framework.Contracts' } | ForEach-Object { $_.Remove() }

# Always copy the parameterTree.xsd to the output dir
$paramTreeXsd = $project.ProjectItems.Item("parameterTree.xsd")
if ($paramTreeXsd)
{
    $copyToOutput = $paramTreeXsd.Properties.Item("CopyToOutputDirectory")
    $copyToOutput.Value = 2
    $buildAction = $paramTreeXsd.Properties.Item("BuildAction")
    $buildAction.Value = 2
}