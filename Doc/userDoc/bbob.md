# BBOB Tuning

Black-Box Optimization Benchmarking (BBOB) is part of the [Black Box Optimization Competition](https://www.ini.rub.de/PEOPLE/glasmtbl/projects/bbcomp/index.html) and includes different multi-dimensional objective functions, which act as a good benchmark for black-box optimization.

We use these BBOB functions via the python 2.7 adapter `bbobeval.py`, provided in `Tools`, to evaluate the behaviour of our tuner. As mentioned in [Technical Preparation](technical_preparation.md), you need to download the bbobbenchmarks python script before using *OPTANO Algorithm BBOB Tuner*.

## Overview

Analogue to what is done in the [SAPS example](saps.md), *OPTANO Algorithm BBOB Tuner* contains the following files:

- `Program.cs` is responsible for reading in parameters and starting *OPTANO Algorithm Tuner*;
- `BbobUtils.cs` is responsible for reading in instances and building the parameter tree;
- `BbobRunner.cs` starts parameter evaluations, i.e. single BBOB runs;
- `BbobRunnerConfiguration.cs` wraps the configuration and the configuration builder of the BBOB tuner;
- `BbobRunnerConfigurationParser.cs` defines custom arguments for BBOB tuning and modifies the output of `--help` accordingly;
- `BbobRunnerFactory.cs` builds `BbobRunner` instances using a given parameter combination;
- `GenericBbobEntryPoint.cs` provides a generic entry point for different BBOB runner parametrizations; and
- `GenericParameterization.cs` defines the different generic parameterization options.

## Customization
The BBOB tuning classes are a simple example on [how to customize <i>OPTANO Algorithm Tuner</i>](advanced.md) to your liking.

As the BBOB python 2.7 script takes a file, including the corresponding BBOB function ID, as input, we need to implement a function `CreateInstancesFilesAndReturnAsList` in `BbobUtils`, which takes care of creating the appropriate instance files. Moreover we have to implement `ITargetAlgorithm` and `ITargetAlgorithmFactory`, which are implemented in `BbobRunner` respectively `BbobRunnerFactory`. Additionally, we have to take care of parsing the BBOB tuner specific parameters in the `BbobRunnerConfigurationParser` and create the respective `BbobRunnerConfiguration`. Finally, note, that the target function of minimizing the BBOB function value is already implemented by `SortByValue`, and the relevant result by `ContinuousResult`.

The complete call to the tuner is built in `Program.cs`.

## Parameter
For parsing the parameters, a `BbobRunnerConfigurationParser` is implemented which parses the specific parameters from the command line, extracts the ones that refer to the BBOB runner configuration and passes the [remaining arguments](parameters.md) to the *OPTANO Algorithm Tuner*.

The `Main` method shows how to call the tuner code from your customized application.

### Specific Parameters
*OPTANO Algorithm BBOB Tuner* checks for the following additional parameters:

<dl>
 <dt>--master</dt>
 <dd>Indicates that this instance of the application should act as master.</dd>
  <dt>--pythonBinary={PATH}</dt>
 <dd>The path to the python 2.7 binary.</dd>
  <dt>--functionId={VALUE}</dt>
 <dd>The bbob function to use. Must be in the range [1,56].</dd>
   <dt>--genericParametrization=BBOBFunctions.GenericParameterization</dt>
 <dd>Specifies the generic parameterization to use for the genetic enginering model. Must be a member of the BBOBFunctions.GenericParameterization enum. Valid Values are:
<dd>Default</dd>
<dd>RandomForestReuseOldTrees</dd>
<dd>RandomForestAverageRank</dd>
<dd>StandardRandomForest (same as Default)</dd>
   <dt>--bbobScript={PATH}</dt>
 <dd>The path to the BBOB python 2.7 script. Default is "Tools/bbobeval.py".</dd>
   <dt>--dimensions={VALUE}</dt>
 <dd>The number of dimensions for the BBOB function. Must be greater than 0. Default is 10.</dd>
   <dt>--instanceSeed={VALUE}</dt>
 <dd>The random seed for the instance seed generator. Default is 42.</dd>
   </dl>

Note, that the `--functionId` represents the single Instance for BBOB. Thus, `--instanceNumbers` many seeds are generated for evaluating the target function with BBOB.

## Error Handling

To handle upcoming errors (e.g. crash of target algorithm) reasonable, the method `ExtractFunctionValue()` is implemented in `BbobRunner`. This method reads out the console output and checks for `"result="`. If not present, it sets the result to double.MaxValue as reasonable value.

## How to Use
The command to run *OPTANO Algorithm BBOB Tuner* may look like

`dotnet Optano.Algorithm.Tuner.Bbob.dll --master --trainingInstanceFolder=[PATH]--maxParallelEvaluations=4 --pythonBinary=[PATH] --functionId=6 --instanceNumbers=1:10`

For starting a worker you have to supply the master host name and the port:

`dotnet Optano.Algorithm.Tuner.Bbob.dll --seedHostName=[HOSTNAME] --port=[PORT]`

The master will print the required information on startup.

## Troubleshooting:

### Python version:
Since our BBOB python adapter `bbobeval.py` is a python 2.7 script, make sure to state the path to your python 2.7 binary in `pythonBinary`.

### Unknown host name:
If the worker does not connect to the master, try to explicitely set the IP adress of the master as host name with `--ownHostName=[IPADRESS]` by starting the master.

## Unittests

For details about the unittests of *OPTANO Algorithm BBOB Tuner* please see [Unittests](unittests.md).