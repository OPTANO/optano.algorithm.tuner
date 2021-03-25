# Lingeling Tuning

Lingeling is a SAT solver with a large number of different parameters. Correcly choosing those parameters depending on your SAT instances can greatly reduce runtime.

The implementation we tune in this example is the SAT competition 2016 version `lingeling-bbc-9230380-160707` available at [http://fmv.jku.at/lingeling/](http://fmv.jku.at/lingeling/).

Since Lingeling is written for execution on a Linux machine, you need to execute *OPTANO Algorithm Lingeling Tuner* on a Linux machine. For details see [Cross Platform Execution of *OPTANO Algorithm Tuner*](../userDoc/cross_platform.md).

Please consider the [technical preparations](technical_preparation.md) before using *OPTANO Algorithm Lingeling Tuner*.

Moreover note, that you may use [FuzzSAT](http://fmv.jku.at/fuzzsat/) as a generator for your SAT instances.

## Overview

Analogue to what is done in the [SAPS example](saps.md), *OPTANO Algorithm Lingeling Tuner* contains the following files:

- `Program.cs` is responsible for reading in parameters and starting *OPTANO Algorithm Tuner*;
- `LingelingUtils.cs` is responsible for reading in instances and building the parameter tree;
- `LingelingRunner.cs` starts parameter evaluations, i.e. single Lingeling runs;
- `LingelingRunnerConfiguration.cs` wraps the configuration and the configuration builder of the Lingeling tuner;
- `LingelingRunnerConfigurationParser.cs` defines custom arguments for Lingeling tuning and modifies the output of `--help` accordingly;
- `LingelingRunnerFactory.cs` builds `LingelingRunner` instances using a given parameter combination;
- `GenericLingelingEntryPoint.cs` provides a generic entry point for different Lingeling runner parametrizations; and
- `GenericParameterization.cs` defines the different generic parameterization options.

In addition, it provides

- `lingelingMemoryLimited.sh`, sets the memory limit on a Linux computer; and
- `lingelingParamTree.xml`, the XML definition of the Lingeling parameter tree.

## Customization
The Lingeling tuning classes are a simple example on [how to customize <i>OPTANO Algorithm Tuner</i>](advanced.md) to your liking.

As Lingeling takes a file name as input, we don't need our own `IInstance` implementation, but can use the `InstanceSeedFile` provided by *OPTANO Algorithm Tuner*. _Note_, that we treat each combination of random seed and SAT instance as a separate `InstanceSeedFile`, i.e. the same SAT instance might occur more than once in an instance set for a given tournament evaluation - with different seeds for Lingeling's internal RNG.<br/>
Likewise, the target function of penalized runtime tuning is already implemented by `SortByRuntime`, and the relevant result by `RuntimeResult`. This leaves us to implement `ITargetAlgorithm` and `ITargetAlgorithmFactory`, which are implemented in `LingelingRunner` respectively `LingelingRunnerFactory`. Additionally, we have to take care of parsing the Lingeling tuner specific parameters in the `LingelingRunnerConfigurationParser` and create the respective `LingelingRunnerConfiguration`.

The complete call to the tuner is built in `Program.cs`.

## Parameter
For parsing the parameters, a `LingelingRunnerConfigurationParser` is implemented which parses the specific parameters from the command line, extracts the ones that refer to the Lingeling runner configuration and passes the [remaining arguments](../userDoc/parameters.md) to the *OPTANO Algorithm Tuner*.

The `Main` method shows how to call the tuner code from your customized application.

### Specific Parameters
*OPTANO Algorithm Lingeling Tuner* checks for the following additional parameters:

<dl>
 <dt>--master</dt>
 <dd>Indicates that this instance of the application should act as master.</dd>
 <dt>--executable={PATH}</dt>
 <dd>{PATH} to the Lingeling executable.</dd>
 <dt>--genericParametrization=LingelingTuner.GenericParameterization</dt>
 <dd>Specifies the generic parameterization to use for the genetic engineering model. Must be a member of the LingelingTuner.GenericParameterization enum. Valid Values are:
<dd>Default</dd>
<dd>RandomForestReuseOldTrees</dd>
<dd>RandomForestAverageRank</dd>
<dd>StandardRandomForest (same as Default)</dd>
 </dd>
 <dt>--factorParK={VALUE}</dt>
 <dd>The factor for the penalization of the average runtime. Needs to be greater than 0. Default is 10.</dd>
 <dt>--rngSeed={VALUE}</dt>
 <dd>The random number generator seed, which generates #numberOfSeeds seeds for every instance of the Lingeling algorithm. Default is 42.</dd>
 <dt>--numberOfSeeds={VALUE}</dt>
 <dd>Specifies the number of different seeds, which are combined with each distinct SAT instance file for evaluating the Lingeling algorithm. Needs to be greater than 0. Default is 1.</dd>
 <dt>--memoryLimitMegabyte={VALUE}</dt>
 <dd>Specifies the memory limit (in megabyte), which can be used for the algorithm. Needs to be greater than 0. Default is 4000.</dd>
</dl>

## A Closer Look on `lingelingMemoryLimited.sh`

`lingelingMemoryLimited.sh` is a bash script for limiting the maximum amount of RAM that a single evaluation of Lingeling (i.e. a candidate parameterization on a specific InstanceSeedFile) can use. It is used by the `LingelingRunner.cs` as an adapter through which the call to the Lingeling executable is forwarded.

```
#!/bin/bash
ulimit -m $2
echo ulimit -m: $(ulimit -m)
ulimit -v $2
echo ulimit -v: $(ulimit -v)
echo $1
exec $1
```

Here, the command ``exec`` makes sure, that the execution of Lingeling is stopped, when the `lingelingMemoryLimited.sh` is terminated, e.g. because ``process.kill()`` is called by the `LingelingRunner`, or because the memory limit is exceeded.

## Error Handling

To handle upcoming errors (e.g. crash of target algorithm) reasonable, the method `ExtractRunStatistics()` is implemented in `LingelingRunner`. This method reads out the console output and checks for `"SATISFIABLE"`. If not present, it creates a cancelled result and sets the corresponding runtime to the given timeout.

## How to Use

The command to run *OPTANO Algorithm Lingeling Tuner* may look like

`dotnet Optano.Algorithm.Tuner.Lingeling.dll --master --maxParallelEvaluations=4 --trainingInstanceFolder=[PATH] --cpuTimeout=10 --executable=[PATH]`

For starting a worker you have to supply the master host name and the port:

`dotnet Optano.Algorithm.Tuner.Lingeling.dll --seedHostName=[HOSTNAME] --port=[PORT]`

The master will print the required information on startup.

## Troubleshooting: 

### Permission denied:
Do not forget to make ``lingeling`` and ``lingelingMemoryLimited.sh`` executable by using the command ``chmod +x`` before starting *OPTANO Algorithm Lingeling Tuner*.

### Unknown host name:
If the worker does not connect to the master, try to explicitely set the IP adress of the master as host name with `--ownHostName=[IPADRESS]` by starting the master.

## Unittests

For details about the unittests of *OPTANO Algorithm Lingeling Tuner* please see [Unittests](unittests.md).