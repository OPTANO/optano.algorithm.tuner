# Post Tuning Runner

By implementing the `ParallelPostTuningRunner` in your custom target algorithm adapter, the *OPTANO Algorithm Tuner* (*OAT*) can be extended by an additional run mode, responsible for post tuning data recording.

This post tuning runner allows you to record the runtime features of arbitrary genome instance pairs after the tuning (e.g. for [gray box](gray_box_tuning.md) experiments). It takes a csv file, containing a list of genome instance pairs, the current index and the number of post tuning runs as input. Each line of this csv file consists of the corresponding serialized `GenomeDoubleRepresentation` and the `InstanceID`.

An exemplary post tuning csv file for a target algorithm with two parameters may contain the following lines.

```
Genome;Instance
[5.0,12.0];Instance_1.mps
[15.0,22.0];Instance_2.mps
```

## Parameters

The post tuning runner of *OAT* checks for the following parameters:

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

In addition, it makes use of several [*OAT* master arguments](../userDoc/parameters.md). In particular, you might want to set the following parameters:

* `--maxParallelEvaluations`
* `--cpuTimeout`
* `--enableDataRecording`
* `--dataRecordDirectory`
* `--dataRecordUpdateInterval`

Please see the [parameter section](../userDoc/parameters.md) for details on these parameters.

## Exemplary Implementation

The provided [Gurobi adapter](gurobi.md) gives an example on how to implement the `ParallelPostTuningRunner` in your custom target algorithm adapter. In particular, it makes use of the `PostTuningAdapterArgumentParser` base class to support the presented parameters.