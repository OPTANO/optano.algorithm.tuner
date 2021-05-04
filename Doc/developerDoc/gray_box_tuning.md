# Gray Box Tuning

The overall tuning runtime is mainly driven by the runtime of the underlying target algorithm. In general, you can limit this runtime by providing a cpu timeout per evaluation via `--cpuTimeout`. To reduce the runtime, wasted on out-timing target algorithm runs, the _OPTANO Algorithm Tuner_ (*OAT*) provides an innovative gray box extension, which aims for detecting and cancelling these runs at runtime, before the specified time limit is actually reached.

## Background

Technically, the gray box extension of *OAT* tries to classify timeouts and non-timeouts at runtime. For this purpose, it makes use of a [balanced random forest classifier](https://statistics.berkeley.edu/tech-reports/666), trained on the target algorithm's runtime features during the tuning. For example, a runtime feature of a MIP solver might be the current MIP gap, (un-)explored node count or the count of feasible solutions. This classification is repeated regularly over time until the target algorithm finishes or reaches the given timeout. Whenever the random forest predicts a timeout, the evaluation is stopped and its current result (e.g. a MIP Gap) is reported/treated as a timeout result.

### Training of Gray Box Classifier

The gray box random forest is trained with data that is gathered during the first _k_ generations of the tuning. Afterwards, the timeout detection is started. To benefit from the additional information that is collected during the tuning, it is re-trained after every following generation.

## Parameters

You can adapt the behaviour of the gray box extension with the following parameters:

<dl>
  <dt>--enableDataRecording={BOOL} [false]</dt>
 <dd>If this option is enabled, this OPTANO Algorithm Tuner instance will record the target algorithm's runtime features and write data log files, potentially used for gray box tuning. To enable gray box tuning, this option and the gray box tuning option need to be enabled.</dd>
  <dt>--dataRecordUpdateInterval={NUMBER} [5% of CPU timeout]</dt>
 <dd>Sets the update interval of the data recorder in seconds. Every <i>dataRecordUpdateInterval</i> seconds a data point is recorded and the gray box classifier is applied, if gray box tuning is enabled.</dd>
     <dt>--enableGrayBox={BOOL} [false]</dt>
 <dd>If this option is enabled, this OPTANO Algorithm Tuner instance will use gray box tuning in order to minimize the overall tuning time.</dd>
    <dt>--grayBoxConfidenceThreshold={NUMBER} [0.75]</dt>
 <dd>Sets the confidence threshold of the gray box random forest. The current target algorithm run is cancelled by the gray box, if the confidence of the random forest exceeds this threshold.</dd>
    <dt>--grayBoxStartGeneration={NUMBER} [5]</dt>
 <dd>Sets the 0-indexed gray box start generation. Before this generation, no target algorithm run is cancelled by the gray box.</dd>
   <dt>--grayBoxStartTimePoint={NUMBER} [5% of CPU timeout]</dt>
 <dd>Sets the gray box start time point during a target algorithm run in seconds. Before this time point, no target algorithm run is cancelled by the gray box.</dd>
</dl>

Note that you can also record the target algorithm's runtime features without making use of the presented gray box extension by enabling the data recording option (i.e. `--enableDataRecording=true`), but not the gray box tuning option (i.e. `enableGrayBox=false`).

In addition, you might want to set the following technical parameters:

<dl>
  <dt>--dataRecordDirectory={ABSOLUTE_PATH} [<i>current directory</i>/DataLogFiles]</dt>
 <dd>Sets the path to the directory where the data log files should be written to.</dd>
   <dt>--removeDataRecordsFromMemoryAfterTraining={BOOL} [false]</dt>
 <dd>If this option is enabled, this OPTANO Algorithm Tuner instance will remove the list of data records from memory after training the gray box random forest and read in all data log files again in every generation. This option will decrease the memory usage, but increase the time, needed to read in the data log files in every generation.</dd>
</dl>

## How to support gray box tuning?

In order to support gray box tuning you need to implement the following two interfaces in your custom target algorithm adapter.

### `IGrayBoxTargetAlgorithm`

The interface `IGrayBoxTargetAlgorithm` comes with the following two events / methods.
 * The event `OnNewDataRecord` should be invoked by the target algorithm adapter every `DataRecordUpdateInterval` seconds to record a data point and apply the gray box classifier. The `GrayBoxHandler` of *OAT* makes use of the `AdapterDataRecord`, passed with this invokation, to construct a new data record entry (see [Data Record Entry](#DataRecordEntry)) and applies the gray box classifier.
 * The method `CancelByGrayBox` is responsible for cancelling the current target algorithm run, if the gray box classifier predicts a timeout.

### `ICustomGrayBoxMethods`

The interface `ICustomGrayBoxMethods` comes with the following two methods.
* The method `GetGrayBoxFeaturesFromDataRecord` is responsible for returning the desired gray box features, used by the gray box random forest, from a given data record entry.
* The method `GetGrayBoxFeatureNamesFromDataRecord` is responsible for returning the desired gray box feature names, used for logging, from a given data record entry.

### Exemplary Implementation

The provided [Gurobi adapter](gurobi.md) gives an example on how to make use of the presented gray box extension and on how to implement the corresponding interfaces.

## Notes on the Data Recorder

### <a name="DataRecordEntry"></a>Data Record Entry

A data record entry consists of the following two subclasses.
* The `TunerDataRecord` contains all data, which is available in the tuner.
  * `NodeID`
  * `GenerationID`
  * `TournamentID`
  * `RunID`
  * `InstanceID`
  * `GenomeID`: This column contains the serialized `GenomeDoubleRepresentation`.
  * `GrayBoxConfidence`
  * `Genome`: These columns contain the `GenomeDoubleRepresentation`, grouped by columns.
  * `FinalResult`
* The `AdapterDataRecord` contains all data, which is available in the target algorithm adapter.
  * `TargetAlgorithmName`
  * `TargetAlgorithmStatus`
  * `ExpendedCpuTime`
  * `ExpendedWallClockTime`
  * `TimeStamp`
  * `AdapterFeatures`: These columns contain the target algorithm's runtime features.
  * `CurrentGrayBoxResult`: These columns contain the result, which is returned, if the current evaluation is cancelled by the gray box.

Note that *OAT* provides the base class `AdapterFeaturesBase` to support the implementation of your own adapter features (e.g. see [Gurobi adapter](gurobi.md)).

### Data Record Directory

By enabling the data recording option, *OAT* will write the following files to the data record directory during the tuning.
* `dataLog_generation_{GenerationId}_process_{ProcessId}_id_{EvaluationActorId}_{TargetAlgorithmStatus}.csv` contains the presented data record entries.
* `generationGenomeComposition.csv` contains all genomes, sorted by generations.
* `generationInstanceComposition.csv` contains all instances, sorted by generations.

By enabling the gray box tuning option, *OAT* will write the following additional files.
* `grayBoxRandomForest.rdf` contains the serialized gray box random forest.
* `featureImportance.csv` contains the feature importance, given by the gray box random forest, sorted by generations.