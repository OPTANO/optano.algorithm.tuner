# File Not Found Exception
## parameterTree.xsd

If you specify your target algorithm's parameters via XML, parameterTree.xsd must be available next to your application file. The _OPTANO Algorithm Tuner_ package tries to automatically copy that file to your output directory while building. In case your system does not support this, you need to handle the file manually. You can set [CopyToOutputDirectory](https://msdn.microsoft.com/en-us/library/bb629388.aspx) for parameterTree.xsd to _Always_ or _PreserveNewest_. The xsd should be included as a resource in your current project.

![Copy parameterTree.xsd](../../images/missing_file_parameter_tree.png)

## SharpLearningCustom.dll
In order to run _OPTANO Algorithm Tuner_, you need to have a reference to SharpLearningCustom.dll. The dll is distributed with the Optano.Algorithm.Tuner nuGet package (located in lib/net461) and should usually automatically get referenced when installing that package. In case this automatic addition goes wrong, you will see the following exception:

![File Not Found Exception](../../images/missing_reference_runtime.png)