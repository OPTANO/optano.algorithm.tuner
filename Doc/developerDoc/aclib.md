# ACLib Scenario Tuning

The [Algorithm Configuration Library 2.0](https://bitbucket.org/mlindauer/aclib2) (ACLib) contains a large number of benchmark scenarios for algorithm tuning and defines a popular standard for parameter definitions, scenario descriptions and target algorithm interfaces.

Our example is able to tune non-deterministic scenarios where

- target algorithms meet the ACLib contract, i.e.
 - can be called via
`
command instance_name 0 cutoff_time_in_s 0 unsigned_seed -parameter_id_1 value_1 -parameter_id_2 value_2 ... -parameter_id_n value_n
` ,
 - are responsible for limiting their CPU time according to the call,
 and
 - print a line of format `Result for ParamILS: status, runtime, runlength, quality, seed, additional data` as output. 
- parameters are defined in [Parameter Configuration Space (PCS) format](http://www.cs.ubc.ca/labs/beta/Projects/SMAC/v2.08.00/manual.pdf#page=12) as specified by SMAC v2.08 and
- scenario definitions are provided in the ACLib scenario format.

## Overview

*OPTANO Algorithm ACLib Tuner* consists of the following namespaces:

- `Configuration` contains classes and methods to read in specific parameters and the scenario file,
- `ParameterConfigurationSpace` contains types to read and encode the parameter structure defined by [PCS format](http://www.cs.ubc.ca/labs/beta/Projects/SMAC/v2.08.00/manual.pdf#page=12), as well as a custom `GenomeBuilder` which can handle forbidden parameter combinations,
- `TargetAlgorithm`  contains implementations of the `ITargetAlgorithm` and `ITargetAlgorithmFactory` interfaces for quality and runtime tuning, and
- `Result` provides type to store algorithm output.

Additionally, the project contains

- `AcLibUtils.cs`, responsible for reading in instances and specifying the parameter tree, and
- `Program.cs`, responsible for reading in parameters and starting *OPTANO Algorithm Tuner*.

The following sections concentrate on parts of the code which differ considerably from the [SAPS](saps.md) and [Gurobi](gurobi.md) examples.

## Parameter Configuration Space

The [PCS format](http://www.cs.ubc.ca/labs/beta/Projects/SMAC/v2.08.00/manual.pdf#page=12) makes it possible to define so-called [conditional parameters](#conditional-parameters), which are activated depending on the value of other parameters; as well as [forbidden parameter combinations](#forbidden-parameter-combinations), which specify that certain value combinations should never be provided to the algorithm.

_OPTANO Algorithm ACLib Tuner_ reads all parameter information using a `ParameterConfigurationSpaceConverter` instance, which creates an object of type `ParameterConfigurationSpaceSpecification`. Conditional parameters are encoded with the aid of the new type `EqualsCondition`, while forbidden parameter combinations are specified by `ForbiddenParameterCombination` objects.

Note that _OPTANO Algorithm ACLib Tuner_ currently does not support reading default parameter values from a PCS file, but will ignore given defaults.

### Conditional Parameters

Conditional parameters are activated depending on the value of other parameters, and deactivated parameters should not be handed to the target algorithm. In *OPTANO Algorithm ACLib Tuner*, these parameters are filtered out in the `ITargetAlgorithmFactory` implementation `RunnerFactoryBase`.

### Forbidden Parameter Combinations

If a parameter value configuration is invalid, this should be detected by the `GenomeBuilder` used in the tuning.
_OPTANO Algorithm ACLib Tuner_ thus defines a custom `GenomeBuilder` called `ParameterConfigurationSpaceGenomeBuilder`.
The class overrides both `IsGenomeValid(Genome genome)` and `MakeGenomeValid(Genome genome)`.

`IsGenomeValid` checks whether any of the forbidden parameter combinations is met. `MakeGenomeValid` also finds those combinations and then randomly mutates relevant parameter values until the configuration is valid again. The number of mutations per configuration is limited by a [the `--maxRepair` parameter](../userDoc/parameters.md#fault-tolerance).

In order to use the custom `GenomeBuilder`, it is provided in the `Master.Run` call of `Program.cs`.

## Specific Parameters
*OPTANO Algorithm ACLib Tuner* checks for one additional parameter:

<dl>
 <dt>--scenario={PATH}</dt>
 <dd>Sets the path to a text file specifying the scenario. The format of this file is the one by ACLib.</dd>
</dl>

Note that limits will not be read from the scenario file, but need to be specified by the usual [parameters](../userDoc/parameters.md#scale-of-the-tuning).

## How to Use

In addition to a target algorithm meeting the ACLib contract, a scenario file as well as a parameter specification in [PCS format](http://www.cs.ubc.ca/labs/beta/Projects/SMAC/v2.08.00/manual.pdf#page=12) are required.

### Exemplary Scenario File (scenario.txt)

```
run_obj = quality
cutoff_time = 300
paramfile = [path to params.pcs]
instance_file = [path to training.txt]
algo = [command to start your target algorithm]
```

### Exemplary PCS File (params.pcs)

```
[parameter_id_1] [min,max][default]
[parameter_id_2] [min,max][default]
```

Note that _OPTANO Algorithm ACLib Tuner_ currently does not support reading default parameter values from a PCS file, but will ignore given defaults.

### Exemplary Instance File (training.txt)

```
[seed] [path_to_instance]
[seed] [path_to_instance]
```

Note that _OPTANO Algorithm ACLib Tuner_ supports the specification of test instances for evaluation. The corresponding instance file has the same format as the one for training instances and its path can be specified in the scenario file with the `test_instance_file` key word. In order to use it, `--scoreGenerationHistory` must be provided as an argument.

### Exemplary Command to run _OPTANO Algorithm ACLib Tuner_

The command to run _OPTANO Algorithm ACLib Tuner_ may look like

`dotnet Optano.Algorithm.Tuner.AcLib.dll --master --maxParallelEvaluations=4 --scenario=[path to scenario.txt] --instanceNumbers=1:2`

For starting a worker you have to supply the master host name and the port:

`dotnet Optano.Algorithm.Tuner.AcLib.dll --seedHostName=[HOSTNAME] --port=[PORT]`

The master will print the required information on startup.

### Troubleshooting

If the worker does not connect to the master, try to explicitely set the IP adress of the master as host name with `--ownHostName=[IPADRESS]` by starting the master.

## Unittests

For details about the unittests of *OPTANO Algorithm ACLib Tuner* please see [Unittests](unittests.md).