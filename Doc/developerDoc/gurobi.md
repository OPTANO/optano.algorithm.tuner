# Gurobi Tuning

[Gurobi](http://www.gurobi.com/index) is a solver for several mathematical program classes including linear programs (LP). It comes with a multitude of parameters and the goal of tuning them might be a complex tradeoff of success rate, quality and runtime.

Our example concentrates on tuning Gurobi on LPs, tuning for success rate first, then for quality and finally runtime.

We tune Gurobi via the `gurobi91.netstandard20` package, integrated in [OPTANO.Modeling.Gurobi](https://www.nuget.org/packages/OPTANO.Modeling.Gurobi/9.1.2.26).

Please consider the [technical preparations](unittests.md) before using *OPTANO Algorithm Gurobi Tuner*.

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
- `GurobiRunEvaluator.cs`, describing the custom tuning metric and racing strategy;
- `GurobiResult.cs`, storing all run properties relevant to the tuning metric; and
- `GurobiCallback.cs`, responsible for checking for cancellations in Gurobi runs; and
- `GurobiGrayBoxMethods.cs`, implementing `ICustomGrayBoxMethods` to support [gray box tuning](gray_box_tuning.md); and
- `GurobiRuntimeFeatures.cs`, implementing `AdapterFeaturesBase` and containing the runtime features of Gurobi; and
- `GurobiInstanceFeatures.cs`, implementing `AdapterFeaturesBase` and containing some rudimentary instance features.

## Tuneable Parameters
*OPTANO Algorithm Gurobi Tuner* tunes 55 Gurobi parameters relevant for LP solving, many of them influencing each other. Explanations for each parameter are given as comments in the `parameterTree.xml` file and can also be found at the [official Gurobi website](http://www.gurobi.com/documentation/9.1/refman/parameters.html#sec:Parameters).

### Parameter Tree
Due to the variety and interdependencies of Gurobi's parameters, its [parameter tree](../userDoc/basic_usage.md#xml) is a good example for the expressive power you have in defining parameters to tune for *OPTANO Algorithm Tuner*. In its definition in `parameterTree.xml`, you can find OR nodes, AND nodes and value nodes; the used domains are categorical, continuous and discrete; and the interdependencies result in a tree that is both wide and has several levels.

When you read the defined parameter tree, keep in mind that AND nodes are parents of independent parameters, while child nodes have interdependencies with their parents. OR nodes turn parts of the tree on and off depending on their value.

Many Gurobi parameters can either be set automatically or be provided by the user. We realize this split in Gurobi's parameter tree by inserting additional OR nodes which represent this decision, and only provide Gurobi runs with the parameters if those OR nodes are set to true.

### Reading in the Tree
`GurobiUtils.cs` shows how to read a parameter tree from file in its method `CreateParameterTree`.

### Artificial Parameters
Some nodes in the tree do not directly correspond to Gurobi parameters. This is caused by the fact that we split some original parameters into multiple tree nodes in order to distinguish their possible states: default, off and on. We distinguish these three states, because their default values imply automatical behaviour, deviating from parameter values set by the user. These nodes are all nodes with IDs like `<GurobiParameterName>Indicator` and get handled in the `CreateParameterTree` method of `GurobiUtils.cs`.

The translation of non-artificial parameters defined by the tree into ones accepted by Gurobi happens in the `GurobiRunnerFactory`: There all non-artificial parameters are set.

## Custom tuning metric and racing strategy

A Gurobi run may return in various different states and tuning can easily get more complex than just looking at a single metric. These facts are evident both in our extension of `Result`, called `GurobiResult`, and in the implementation of `IRunEvaluator`, called `GurobiRunEvaluator`.

`GurobiResult`, apart from the usual properties of `Runtime` and `IsCancelled`, also stores whether the run found a valid solution (`HasValidSolution`) and the gap that resulted (`Gap`). As those properties can still be interesting if the run was cancelled, we do not allow automatically generated `GurobiResults` in case of cancellations and throw an exception if the parameterless constructor used for those gets called.

Of course the choice of your tuning metric strongly depends on your desired tuning goal. For example tuning for robustness in the sense of runtime or tuning for runtime speed-up both imply totally different tuning metrics. One big advantage of *OAT* is its flexibility concerning the tuning goal due to the possibility of implementing your own tuning metric and our own [racing strategy](../userDoc/parameters.md#racing). In this example we focus on tuning for runtime speed-up and therefore implemented the following version of `IRunEvaluator`.

When presented with parameter combinations and their `GurobiResults` in `GurobiRunEvaluator`, we first sort by success rate, i.e. number of valid solutions found. Then, we sort by the number of cancellations. A run without a cancellation means that a valid solution was found and proven to be (almost) optimal. Parameter combinations with same success and cancellation rates are compared using the average remaining gap on cancelled runs which found a solution. Finally, the least important factor in tuning is average runtime.

Since we implemented a custom tuning metric, we also want to customize our racing strategy. We can customize it by specifying the method `GetGenomesThatCanBeCancelledByRacing` of `IRunEvaluator` in the following way. In our implementation a genome should be cancelled by racing once it becomes clear that it can not beat the current numberOfTournamentWinner-best genome and therefore won't get to be a mini tournament winner. Thereby we ensure to not cancel any target algorithm evaluations of possible mini tournament winners by racing such that all mini tournament winners will see all instances. Moreover we implement the method `ComputeEvaluationPriorityOfGenome` of `IRunEvaluator` such that genomes that might become a racing threshold candidate get evaluated first. By enabling this custom racing strategy (i.e. `--enableRacing=true`) you can speed-up the whole tuning drastically.

## Running Gurobi

Single Gurobi runs are started by `GurobiRunner` instances. Each instance represents one parameter combination which is described by a `GRBEnv` object which was created in `GurobiRunnerFactory` and passed to `GurobiRunner`.

In `GurobiRunner.Run`, a single run on an instance file is executed: A log file is created, the instance file is passed to a new `GRBModel` in the existing Gurobi environment, and a start solution is read in case one exists in form of an .mst file. Our callback class `GurobiCallback` is registered to make sure that cancellations lead to abortion. Then, the optimization is started.

Once the optimization finished, regardless of the reason, we create a new `GurobiResult` with the correct properties. We then dispose of the model.

Within the Gurobi package, you shouldn't only dispose of the model, but also of the `GRBEnv` instance. We cannot handle this disposal in `GurobiRunner` itself because each environment will be used for several runs. Exactly for this use case, *OPTANO Algorithm Tuner* is implemented to automatically check if  `ITargetAlgorithm` objects implement `IDisposable` and call `Dispose` on such an object once it is not needed anymore.<br/>
We can therefore handle disposal of `GRBEnv` objects by implementing `IDisposable` and trusting *OPTANO Algorithm Tuner* to call it when required.

## Parameter

For parsing the parameters, a `GurobiRunnerConfigurationParser` is implemented which parses the specific parameters from the command line, extracts the ones that refer to the Gurobi runner configuration and passes the [remaining arguments](../userDoc/parameters.md) to the *OPTANO Algorithm Tuner*.

### Specific Parameters
*OPTANO Algorithm Gurobi Tuner* checks for the following additional parameters:

<dl>
 <dt>--master</dt>
 <dd>Indicates that this instance of the application should act as master.</dd>
 <dt>--grbThreadCount={VALUE}</dt>
 <dd>Sets the maximum number of threads that may be used by Gurobi. Default is 4. Needs to be greater than 0.</dd>
 <dt>--numberOfSeeds={VALUE} [1]</dt>
 <dd>Sets the number of random seeds to use for every .mps file found in the instance folder. For each file, numberOfSeeds many independent seeds will be used, effectively increasing the instance count by a factor of numberOfSeeds. Needs to be greater than 0.</dd>
 <dt>--rngSeed={VALUE} [42]</dt>
 <dd>Sets the random number generator seed, which generates #numberOfSeeds seeds for every instance of the Gurobi algorithm.</dd>
 <dt>--grbNodefileDirectory={PATH} [<i>current directory</i>/nodefiles]</dt>
 <dd>Sets the [nodefile directory of Gurobi](https://www.gurobi.com/documentation/8.1/refman/nodefiledir.html).</dd>
 <dt>--grbNodefileStartSizeGigabyte={VALUE} [0.5]</dt>
 <dd>Sets the [memory threshold in gigabyte of Gurobi](https://www.gurobi.com/documentation/8.1/refman/nodefilestart.html) for writing MIP tree nodes in nodefile on disk. Needs to be greater than or equal to 0.</dd>
 <dt>--grbTerminationMipGap={VALUE} [0.01]</dt>
 <dd>Sets the [termination mip gap of Gurobi](https://www.gurobi.com/documentation/8.1/refman/mipgap2.html). Needs to be greater than or equal to 0.</dd>
</dl>

## Error Handling

To handle upcoming errors (e.g. crash of target algorithm) reasonable, the method `CreateGurobiResult()` is implemented in `GurobiRunner`. This method checks the [status codes of Gurobi](https://www.gurobi.com/documentation/9.1/refman/optimization_status_codes.html) and handles the result accordingly. If the status of Gurobi is `"LOADED"`, `"TIME_LIMIT"`, `"INTERRUPTED"`, `"NUMERIC"` or `"INPROGRESS"` it creates a cancelled result and sets the corresponding runtime to the given timeout.

## Gray Box Tuning

As mentioned in the [gray box tuning](gray_box_tuning.md) section, *OPTANO Algorithm Gurobi Tuner* gives an example on how to implement the `IGrayBoxTargetAlgorithm` and `ICustomGrayBoxMethods` interfaces to support gray box tuning.

In this example we use the [callback codes of Gurobi](https://www.gurobi.com/documentation/9.1/refman/cb_codes.html) in `GurobiCallback` to receive the desired runtime features and pass them to the `GurobiRunner`, which implements the `IGrayBoxTargetAlgorithm` interface. Moreover, the `GurobiGrayBoxMethods` implement the `ICustomGrayBoxMethods` interface and returns the desired gray box features and feature names from a given [data record entry](gray_box_tuning.md#DataRecordEntry).

More precisecly, *OPTANO Algorithm Gurobi Tuner* makes use of the following runtime and instance features to detect timeouts at runtime.
* Current expended wall clock time
* Current and last cutting planes count
* Current and last explored node count
* Current and last feasible solutions count
* Current and last MIP gap
* Current and last simplex iterations count
* Current and last unexplored node count
* Current number of variables
* Current number of integer variables
* Current number of linear constraints
* Current number of non-zero coefficients

### Gray Box Simulation

As mentioned in the [gray box simulation](gray_box_simulation.md) section, the *OPTANO Algorithm Gurobi Tuner* can easily form the basis for gray box simulations to estimate the impact of the presented [gray box parameters](gray_box_tuning.md). Please refer to referenced section for detailed information on how to use and evaluate this simulation.

### Post Tuning Runner

As mentioned in the [post tuning runner](post_tuning_runner.md) section, *OPTANO Algorithm Gurobi Tuner* gives an example on how to implement the `ParallelPostTuningRunner` in your custom target algorithm adapter to extend *OAT* by an additional run mode, responsible for post tuning data recording.

In particular, it makes use of the `PostTuningAdapterArgumentParser` base class to support the following additional post tuning parameters.

<dl>
 <dt>--postTuning</dt>
 <dd>Indicates that this instance of the application should act as post tuning runner.</dd>
 <dt>--pathToPostTuningFile={ABSOLUTE_PATH} [<i>current directory</i>/postTuningRuns.csv]</dt>
 <dd>Sets the path to the post tuning file, containing the desired genome instance pairs.</dd>
  <dt>--indexOfFirstPostTuningRun={VALUE} [0]</dt>
 <dd>Sets the index of the first post tuning genome instance pair to evaluate.</dd>
  <dt>--numberOfPostTuningRuns={VALUE} [1]</dt>
 <dd>Sets the number of post tuning runs to start in total.</dd>
</dl>

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