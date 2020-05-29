# Gurobi Tuning

[Gurobi](http://www.gurobi.com/index) is a solver for several mathematical program classes including linear programs (LP). It comes with a multitude of parameters and the goal of tuning them might be a complex tradeoff of success rate, quality and runtime.

Our example concentrates on tuning Gurobi on LPs, tuning for success rate first, then for quality and finally runtime.

We tune Gurobi via the Gurobi81.NET package, integrated in [OPTANO.Modeling.Gurobi](https://www.nuget.org/packages/OPTANO.Modeling.Gurobi/8.1.1.20).

Please consider the [technical preparations](technical_preparation.md) before using *OPTANO Algorithm Gurobi Tuner*.

## Overview
Analogue to what is done in the [SAPS example](saps.md), *OPTANO Algorithm Gurobi Tuner* contains the following files:

- `Program.cs`, responsible for reading in parameters and starting *OPTANO Algorithm Tuner*;
- `GurobiUtils.cs`, responsible for reading in instances and the parameter tree;
- `GurobiRunner.cs`, starting parameter evaluations, i.e. single Gurobi runs;
- `GurobiRunnerConfiguration.cs`, wrapping Gurobi specific run parameters that should not be tuned, but set for an entire tuning;
- `GurobiRunnerConfigurationParser.cs`, defining custom arguments for Gurobi tuning and modifying the output of `--help` accordingly; and
- `GurobiRunnerFactory.cs`, which builds `GurobiRunner` instances using a given parameter combination.

In addition, it provides

- `parameterTree.xml`, the XML definition of the Gurobi parameter tree;
- `GurobiRunEvaluator.cs`, describing the custom target function;
- `GurobiResult.cs`, storing all run properties relevant to the target function; and
- `GurobiCallback.cs`, responsible for checking for cancellations in Gurobi runs.

## Tuneable Parameters
*OPTANO Algorithm Gurobi Tuner* tunes 30 Gurobi parameters relevant for LP solving, many of them influencing each other. Explanations for each parameter are given as comments in the `parameterTree.xml` file and can also be found at the [official Gurobi website](http://www.gurobi.com/documentation/7.5/refman/parameters.html#sec:Parameters).

### Parameter Tree
Due to the variety and interdependencies of Gurobi's parameters, its [parameter tree](basic_usage.md#xml) is a good example for the expressive power you have in defining parameters to tune for *OPTANO Algorithm Tuner*. In its definition in `parameterTree.xml`, you can find OR nodes, AND nodes and value nodes; the used domains are categorial, continuous and discrete; and the interdependencies result in a tree that is both wide and has several levels.

When you read the defined parameter tree, keep in mind that AND nodes are parents of independent parameters, while child nodes have interdependencies with their parents. OR nodes turn parts of the tree on and off depending on their value.

Many Gurobi parameters can either be set automatically or be provided by the user. We realize this split in Gurobi's parameter tree by inserting additional OR nodes which represent this decision, and only provide Gurobi runs with the parameters if those OR nodes are set to true.

### Reading in the Tree
`GurobiUtils.cs` shows how to read a parameter tree from file in its method `CreateParameterTree`.

### Artificial Parameters
Some nodes in the tree do not directly correspond to Gurobi parameters. This is caused by the fact that we split some original parameters into multiple tree nodes in order to either

- turn the parameters off and on or
- better model different behavior in different domain parts.

Examples for the first case are all nodes with IDs like "`<some Gurobi parameter name>Active`", while the second case is true for the nodes with the IDs "`ObjScaleValue`" and "`ObjScaleCoefficient`".

The translation of parameters defined by the tree into ones accepted by Gurobi happens in `GurobiRunnerFactory`: In a first step, non-artificial parameters are set. Now note that most artificial OR nodes have been inserted in order to turn off and on their respective (single) children, making it possible to check whether automatically setting those child parameters is the better choice compared to selecting them. As choosing parameters depending on OR node values is already handled by *OPTANO Algorithm Tuner*, those OR nodes don't need custom parsing in `GurobiRunnerFactory`.<br>
An exception to this is the parameter with ID "`RinsActive`" which has three possible states: Set by the user, set automatically and turned off. This OR node may contain information apart from enabling its child node and is therefore parsed specifically in the factory. In addition, the parameters with the IDs "`ObjScaleValue`" and "`ObjScaleCoefficient`" model the same original parameter and hence must be checked and to set that one explicitely.

## Target Function

A Gurobi run may return in various different states and tuning can easily get more complex than just looking at a single metric. These facts are evident both in our extension of `Result`, called `GurobiResult`, and in the implementation of `IRunEvaluator`, called `GurobiRunEvaluator`.

`GurobiResult`, apart from the usual properties of `Runtime` and `IsCancelled`, also stores whether the run found a valid solution (`HasValidSolution`) and the gap that resulted (`Gap`). As those properties can still be interesting if the run was cancelled, we do not allow automatically generated `GurobiResults` in case of cancellations and throw an exception if the parameterless constructor used for those gets called.

When presented with parameter combinations and their `GurobiResults` in `GurobiRunEvaluator`, we first sort by success rate, i.e. rate of valid solutions found. Then, we sort by rate of cancellations. A run without a cancellation means that a valid solution was found and proven to be (almost) optimal. Parameter combinations with same success and cancellation rates are compared using the average remaining gap on cancelled runs which found a solution. Finally, the least important factor in tuning is average runtime.

Although the defined target function uses runtime only as the least important metric, applying [racing](parameters.md#racing) still makes sense: If racing is turned on, *OPTANO Algorithm Tuner* stops evaluating parameter combinations that couldn't possible win against their competition in terms of runtime. This check is made only with competing parameter combinations that managed to solve all current instances without any cancellations. As non-optimal success rates, cancellation rates or gaps on cancelled runs only happen when at least one run is cancelled, for those parameter combinations, runtime actually is the deciding factor.

## Running Gurobi

Single Gurobi runs are started by `GurobiRunner` instances. Each instance represents one parameter combination which is described by a `GRBEnv` object which was created in `GurobiRunnerFactory` and passed to `GurobiRunner`.

In `GurobiRunner.Run`, a single run on an instance file is executed: A log file is created, the instance file is passed to a new `GRBModel` in the existing Gurobi environment, and a start solution is read in case one exists in form of an .mst file. Our callback class `GurobiCallback` is registered to make sure that cancellations lead to abortion. Then, the optimization is started.

Once the optimization finished, regardless of the reason, we create a new `GurobiResult` with the correct properties. We then dispose of the model.

Within the Gurobi package, you shouldn't only dispose of the model, but also of the `GRBEnv` instance. We cannot handle this disposal in `GurobiRunner` itself because each environment will be used for several runs. Exactly for this use case, *OPTANO Algorithm Tuner* is implemented to automatically check if  `ITargetAlgorithm` objects implement `IDisposable` and call `Dispose` on such an object once it is not needed anymore.<br/>
We can therefore handle disposal of `GRBEnv` objects by implementing `IDisposable` and trusting *OPTANO Algorithm Tuner* to call it when required.

## Parameter

For parsing the parameters, a `GurobiRunnerConfigurationParser` is implemented which parses the specific parameters from the command line, extracts the ones that refer to the Gurobi runner configuration and passes the [remaining arguments](parameters.md) to the *OPTANO Algorithm Tuner*.

### Specific Parameters
*OPTANO Algorithm Gurobi Tuner* checks for the following additional parameters:

<dl>
 <dt>--master</dt>
 <dd>Indicates that this instance of the application should act as master.</dd>
 <dt>--grbThreadCount={VALUE}</dt>
 <dd>Sets the maximum number of threads that may be used by Gurobi. Default is 4. Needs to be greater than 0.</dd>
 <dt>--numberOfSeeds={VALUE}</dt>
 <dd>Sets the number of random seeds to use for every .mps file found in the instance folder. For each file, numberOfSeeds many independent seeds will be used, effectively increasing the instance count by a factor of numberOfSeeds. Default is 1. Needs to be greater than 0.</dd>
 <dt>--rngSeed={VALUE}</dt>
 <dd>Sets the random number generator seed, which generates #numberOfSeeds seeds for every instance of the Gurobi algorithm. Default is 42.</dd>
 <dt>--grbNodefileDirectory={PATH}</dt>
 <dd>Sets the [nodefile directory of Gurobi](https://www.gurobi.com/documentation/8.1/refman/nodefiledir.html). Default is a subfolder 'nodefiles' in the current working directory.</dd>
 <dt>--grbNodefileStartSizeGigabyte={VALUE}</dt>
 <dd>Sets the [memory threshold in gigabyte of Gurobi](https://www.gurobi.com/documentation/8.1/refman/nodefilestart.html) for writing MIP tree nodes in nodefile on disk. Default is 0.5 GB. Needs to be greater than or equal to 0.</dd>
 <dt>--grbTerminationMipGap={VALUE}</dt>
 <dd>Sets the [termination mip gap of Gurobi](https://www.gurobi.com/documentation/8.1/refman/mipgap2.html). Default is 0.01. Needs to be greater than or equal to 0.</dd>
</dl>

## Error Handling

To handle upcoming errors (e.g. crash of target algorithm) reasonable, the method `CreateGurobiResult()` is implemented in `GurobiRunner`. This method checks the [status codes of Gurobi](https://www.gurobi.com/documentation/9.0/refman/optimization_status_codes.html) and handles the result accordingly. If the status of Gurobi is `"LOADED"`, `"TIME_LIMIT"`, `"INTERRUPTED"`, `"NUMERIC"` or `"INPROGRESS"` it creates a cancelled result and sets the corresponding runtime to the given timeout.

## How to Use

You have to make sure you possess sufficient Gurobi licenses to run the number of parallel evaluations that will result from the number of your computing nodes.

The command to run *OPTANO Algorithm Gurobi Tuner* may look like

`dotnet Optano.Algorithm.Tuner.Gurobi.dll --master --maxParallelEvaluations=1 --trainingInstanceFolder=[PATH] --cpuTimeout=10`

For starting a worker you have to supply the master host name and the port:

`dotnet Optano.Algorithm.Tuner.Gurobi.dll --seedHostName=[HOSTNAME] --port=[PORT]`

The master will print the required information on startup.

### Troubleshooting:
If the worker does not connect to the master, try to explicitely set the IP adress of the master as host name with `--ownHostName=[IPADRESS]` by starting the master.

## Unittests

For details about the unittests of *OPTANO Algorithm Gurobi Tuner* please see [Unittests](unittests.md).