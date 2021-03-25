# Tuning in multiple sessions

In the case of insufficient computing resources or sudden failure of your executing machine, it is useful to be able to continue a specific execution of *OPTANO Algorithm Tuner* (*OAT*) that was cancelled at an earlier time.

To make this possible, *OAT* regurlarly stores intermediate results to file. Usually, these `.oatstat` files will be stored in a folder `status` placed in the calling directory, but you can change the path via a [parameter](parameters.md) to prevent new runs from overwriting old ones.

The intermediate results include

- execution results obtained on the tuneable algorithm,
- current internal state, and
- all [parameters](parameters.md) inherent to the tuning, e.g. the total number of iterations to run or the mutation rate.

Therefore, to continue with a tuning, all you have to do is to add the parameter `--continue` to your usual command. Make sure to leave all parameters that are tuning-specific unaltered. You can still set the following, technical parameters:

- --statusFileDir={PATH}
- --maxParallelEvaluations={NUMBER}
- --maxParallelThreads={NUMBER}
- --verbose={0, 1, 2, 3} 
- --ownHostName={FQDN/IP}
- --port={NUMBER} 
- --faultTolerance={NUMBER} 
- --maxRepair={NUMBER} 
- --logFile={PATH}
- --instanceFolder={PATH}
- --trackConvergenceBehavior={BOOL}
- --zipOldStatus={BOOL}

In addition, you might change `trainModel` from `true` to `false`.

Check the [page about parameters](parameters.md) for more information about each of these.

If you do not specify `ownHostName`, `port`, `trainingInstanceFolder` or `testInstanceFolder`, default values will be used. If you do not specify one of the other parameters, the stored values from the provided status file will be used.

A call to continue with a basic application of *OAT* may look like this:

	dotnet Optano.Algorithm.Tuner.Application.dll --master --continue --maxParallelEvaluations=4 --basicCommand=<commandToExecuteYourAlgorithmWith> --parameterTree=<pathToXML> --trainingInstanceFolder=<instanceFolder> <optionalTechnicalParametersOverwrites>

## Configuration changes

In the overwhelming majority of use cases, the functionality described above should suit your needs. Very rarely, you might find yourself in a situation where you want to change even more parameters between different runs. Be aware that in this case, tuning in multiple sessions is not equivalent to a single longer session of *OAT*.

If this is what you intend, you can set `--strictCompatibilityCheck=false` and overwrite almost all parameters in your continued run.
