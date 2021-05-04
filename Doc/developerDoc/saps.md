# SAPS Tuning

SAPS is a SAT solver with four independent parameters. Correcly choosing those parameters depending on your SAT instances can greatly reduce runtime.

The implementation we tune in this example is the one shipped with UBCSAT version 1.1.0, available at [UBCSAT homepage](http://ubcsat.dtompkins.com/). As we tune for penalized runtime, have simple parameters, and UBCSAT can be called from console, it is possible to use the [out-of-the-box](../userDoc/basic_usage.md) tuning provided by *OPTANO Algorithm Tuner*. The parameters for that would be:

`--master --maxParellelEvaluations=8 --basicCommand="[PATH]\ubcsat.exe -alg saps -i {instance} {arguments} -timeout 10 -cutoff max -seed 42" --trainingInstanceFolder=[PATH] --parameterTree=[PATH]\sapsParameterTree.xml`

However, [customizing <i>OPTANO Algorithm Tuner</i>](advanced.md) reduces overhead and comes with increased precision. For applications dealing with small SAT instances solvable in milliseconds, this can make a noticeable difference.

Please consider the [technical preparations](unittests.md) before using *OPTANO Algorithm SAPS Tuner*.

Moreover note, that you may use [FuzzSAT](http://fmv.jku.at/fuzzsat/) as a generator for your SAT instances.

## Overview
The solution implementing *OPTANO Algorithm SAPS Tuner* consists of the classes

- `Program.cs`
- `SapsUtils.cs`,
- `SapsRunner.cs`,
- `SapsRunnerConfiguration.cs`,
- `SapsRunnerConfigurationParser.cs`, 
- `SapsRunnerFactory.cs`,
- `GenericSapsEntryPoint.cs`, and
- `GenericParameterization.cs`.

`Program.cs` is responsible for reading in parameters and starting *OPTANO Algorithm Tuner*.<br/>
`SapsUtils.cs` is responsible for reading in instances and building the parameter tree.<br/>
`SapsRunner.cs` starts parameter evaluations, i.e. single SAPS runs.<br/>
`SapsRunnerConfiguration.cs` wraps the configuration and the configuration builder of the SAPS Tuner.<br/>
`SapsRunnerConfigurationParser.cs` defines custom arguments for SAPS tuning and modifies the output of `--help` accordingly.<br/>
`SapsRunnerFactory.cs` builds `SapsRunner` instances using a given parameter combination.<br/>
`GenericSapsEntryPoint.cs` provides a generic entry point for different SAPS runner parametrizations.<br/>
`GenericParameterization.cs` defines the different generic parameterization options.

## Customization
The SAPS tuning classes are a simple example on [how to customize <i>OPTANO Algorithm Tuner</i>](advanced.md) to your liking.

As UBCSAT takes a file name as input, we don't need our own `IInstance` implementation, but can use the `InstanceSeedFile` provided by *OPTANO Algorithm Tuner*. Likewise, the target function of penalized runtime tuning is already implemented by `SortByRuntime`, and the relevant result by `RuntimeResult`. This leaves us to implement `ITargetAlgorithm` and `ITargetAlgorithmFactory`, which are implemented in `SapsRunner` respectively `SapsRunnerFactory`. Additionally, we have to take care of parsing the SAPS Tuner specific parameters in the `SapsRunnerConfigurationParser` and create the respective `SapsRunnerConfiguration`.

The complete call to the tuner is built in `Program.cs`.

## Parameter
For parsing the parameters, a `SapsRunnerConfigurationParser` is implemented which parses the specific parameters from the command line, extracts the ones that refer to the SAPS runner configuration and passes the [remaining arguments](../userDoc/parameters.md) to the *OPTANO Algorithm Tuner*.

The `Main` method shows how to call the tuner code from your customized application.

### Specific Parameters
*OPTANO Algorithm SAPS Tuner* checks for the following additional parameters:

<dl>
 <dt>--master</dt>
 <dd>Indicates that this instance of the application should act as master.</dd>
 <dt>--executable={PATH}</dt>
 <dd>The path to the ubcsat executable.</dd>
 <dt>--genericParametrization=Optano.Algorithm.Tuner.Saps.CustomModel.GenericParameterization</dt>
 <dd>Specifies the generic parameterization to use for the genetic enginering model. Must be a member of the Optano.Algorithm.Tuner.Saps.CustomModel.GenericParameterization enum. Valid Values are:
<dd>Default</dd>
<dd>RandomForestReuseOldTrees</dd>
<dd>RandomForestAverageRank</dd>
<dd>StandardRandomForest (same as Default)</dd>
 </dd>
 <dt>--factorParK={VALUE} [0]</dt>
 <dd>The factor for the penalization of the average runtime. Needs to be greater or equal to 0. If 0, OAT sorts first by highest number of uncancelled runs and then by unpenalized average runtime.</dd>
 <dt>--rngSeed={VALUE} [42]</dt>
 <dd>The random number generator seed, which generates #numberOfSeeds seeds for every instance of the SAPS algorithm.</dd>
 <dt>--numberOfSeeds={VALUE} [1]</dt>
 <dd>Specifies the number of seeds, which are used for every instance of the SAPS algorithm. Needs to be greater than 0.</dd>
</dl>

### The `SapsRunnerConfigurationParser`

`SapsRunnerConfigurationParser.cs` defines the class `SapsRunnerConfigurationParser` which parses specific parameters from the command line and extracts the ones that should be handled by *OPTANO Algorithm Tuner*. Additionally, it defines a useful output if the `help` parameter is provided.

`SapsRunnerConfigurationParser` extends `HelpSupportingArgumentParser` provided by *OPTANO Algorithm Tuner* and consists of

- fields and properties for the arguments to read
- the methods `CreatePreprocessingOptionSet` and `CreateMasterOptionSet` which define the possible options and how to parse them. The descriptions of those will be printed when help is requested.
- the method `PrintHelp` which prints both the custom options and the ones defined by *OPTANO Algorithm Tuner*.
- the method `ParseArguments` which takes care of the order and dependencies between parsing of parameters, and also throws exceptions in case that some required parameters are not set.

## A Closer Look on `SapsRunner`

`SapsRunner` is the class that starts SAPS runs for certain parameter combinations. Every instance of `SapsRunner` is used for several evaluations on the same parameter combination. Therefore, the parameters are set in the constructor, while the instance for the run is specified in a method call.

*OPTANO Algorithm SAPS Tuner* handles its task by starting UBCSAT in an additional process, waiting for it and reading the process output. Note that `Run(InstanceSeedFile instance, CancellationToken cancellationToken)` has to take care of the provided cancellation token. `SapsRunner` handles this by registering a `ProcessUtils.CancelProcess(process)` method. This method is part of *OPTANO Algorithm Tuner* and can therefore also be used by your own implementations.

If you want to handle cancelled evaluations as results with an `IsCancelled` flag set to `true`, you have to make sure to call `cancellationToken.ThrowIfCancellationRequested()` in case of a cancellation. `SapsRunner` does just that.

## Error Handling

To handle upcoming errors (e.g. crash of target algorithm) reasonable, the method `CreateRuntimeResult()` is implemented in `SapsRunner`. This method reads out the console output and checks for `"CPUTime_Median ="`. If not present, it creates a cancelled result and sets the corresponding runtime to the given timeout.


## How to Use
The command to run *OPTANO Algorithm SAPS Tuner* may look like

`dotnet Optano.Algorithm.Tuner.Saps.dll --master --maxParallelEvaluations=4 --trainingInstanceFolder=[PATH] --cpuTimeout=10 --executable=[PATH]`

For starting a worker you have to supply the master host name and the port:

`dotnet Optano.Algorithm.Tuner.Saps.dll --seedHostName=[HOSTNAME] --port=[PORT]`

The master will print the required information on startup.

### Troubleshooting:
If the worker does not connect to the master, try to explicitely set the IP adress of the master as host name with `--ownHostName=[IPADRESS]` by starting the master.

## Unittests

For details about the unittests of *OPTANO Algorithm SAPS Tuner* please see [Unittests](unittests.md).