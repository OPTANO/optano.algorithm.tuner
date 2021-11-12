# Parameter Selection

This page contains some advice on how to choose [parameters](parameters.md) for your tuning.

Note that there exist some parameters which change the employed tuning algorithm. These are not discussed here. Hints about selecting the best tuning algorithm for your application can be found on the [page presenting the different algorithms](algorithms.md).

## <a name="runtime-estimation"></a>Introductory Remarks: Overall Tuning Runtime Estimation

To estimate a rough upper bound for the overall tuning runtime, please [download the prepared Excel file](../download.md) and follow the instructions in this section.

In general, the overall tuning runtime mainly depends on three factors: the number of performed target algorithm evaluations, their runtime and the tuner's `Degree of Parallelism`.

While you can limit the target algorithm's runtime by providing a [cpu timeout](parameters.md#racing), the number of performed target algorithm evaluations depends on multiple parameters: A rough upper bound is given by `numGens` * (`popSize` / 2) * `Average Number of Instances per Generation`. Here, the `Average Number of Instances per Generation` can be determined by [(`startInstanceNumber` + `endInstanceNumber`) / 2 * `goalGen` + `endInstanceNumber` * (`numGens` – `goalGen`)] / `numGens`.

Moreover, the *OPTANO Algorithm Tuner* will start `maxParallelEvaluations` per computing node at the same time and can even make use of multiple computing nodes, if executed in [distributed fashion](distributed.md). Hence, the tuner's `Degree of Parallelism` is given by `maxParallelEvaluations` * `Number of Nodes`. Here, the `Number of Nodes` can be determined by `Number of Worker Nodes` + 1, if local evaluations on the master node are allowed (see `--allowLocalEvaluations` for details).

Finally, a rough upper bound for the overall tuning runtime is given by:
* `numGens` * (`popSize` / 2) * `Average Number of Instances per Generation` * `cpuTimeout` / `Degree of Parallelism`

In practice, the actual tuning runtime is often less than half of this estimated tuning runtime for several reasons:
1) Already evaluated genome instance pairs will not be evaluated again in subsequent generations.
1) Enabling [racing](parameters.md#racing) can significantly reduce the number of performed target algorithm evaluations.
1) _Good_ configurations will likely finish (at least some of the) instances before reaching the `cpuTimeout`.

## maxParallelEvaluations
The parameter `--maxParallelEvaluations` specifies how many instances of your target algorithm may be executed in parallel. To set it, you should be aware of

- the maximum memory usage of your target algorithm
- the number of cores your target algorithm uses
- the available memory on each of your computing nodes
- the number of cores of each of your computing nodes

The parameter should be chosen such that sufficent cores and memory are available for the selected number of parallel runs.

## numGens
The number of generations usually depends on how long you want to spend on tuning. We do not recommend to lower it too drastically from its default value 100. In addition to `--numGens`, you can also set `--evaluationLimit` to directly bound the number of target algorithm runs.

## popSize
The default population size is 128, and for best results, your choice should be around the same size.

## miniTournamentSize
The default mini tournament size is 8, and for best results, your choice should be around the same size.

## instanceNumbers
The maximum number of instances used for tuning should be sufficiently high, else the found parameter combinations will be 'specialized' to the instances you chose. 

## <a id="model-based-crossover" name="model-based-crossover"></a>Model-Based Crossover

These parameters apply to the model-based crossover operator. They become relevant (i.e. the ML model is trained during iterations) when 
- `--engineeredProportion > 0`, or
- `--enableSexualSelection=true`, or
- `--trainModel=true`.

This is not an extensive list. The parameters below are those that require the most thought. For a complete list of parameters, see [here](parameters.md#model-based-parameters).

### engineeredProportion
If this parameter is set to a value greater than `0`, some of the new offspring will be _"genetically engineered"_, instead of being produced by the default crossover operator. Given two parents, a targeted sampling is performed, during which thousands of _potential_ offspring is generated and scored by using the predictions of a machine learning model. From all potential offspring, the genome with the _best predicted_ score (combined with some penalization[*] for being "too similar" to the current population) is used in the next iteration.

*): The parameter `--maxRanksCompensatedByDistance` you can specify some "bonus score" that is given to boost the score for "very unique" genomes, i.e. genomes that don't share many features with the current population. See the documentation of that parameter for further information.

### enableSexualSelection
By default, the non-competitive mates for the crossover (classical and model-based) are selected uniformly at random. By setting `--enableSexualSelection=true`, the random selection is performed with a so-called _roulette-wheel selection_, using the predicted performance of the non-competitive mates as weight. The predicted performance is obtained by querying the machine learning model that is otherwise used for the model-based crossover operator.

_**Note:**_ You can even enable sexual selection if you perform a run that uses `--engineeredProportion=0`. The model will be trained/updated in the same way that it would be during the model-based crossover. If you do not want to use sexual selection (and model-based crossover) in the beginning of your tuning, but still want to leave you the option to do so on a `--continue`, you can set `--trainModel=true` when you start the tuning. If this parameter is set to `true`, the machine learning model will always be trained/updated, so that you can `--continue` with a model that has the "correct" state.

### maxRanksCompensatedByDistance and featureSubsetRatioForDistance
During the model-based crossover, many offspring candidates of two parents are scored using a machine learning model that predicts the expected performance. Since this method is very greedy, you can specify a _bonus_ for parameterizations that are "very different", when compared to the current population:<br/>
For each potential offspring, its distance to each of the members of the current population is computed, using the specified `--distanceMetric`. The aggregated distances (e.g. average, 3-NN, ...) are normalized over _all_ potential offspring and then multiplied by `--maxRanksCompensatedByDistance`. The resulting value in the range of `[0, maxRanksCompensatedByDistance]` is subtracted from the predicted score for each potential offspring. This means genomes with a mediocre predicted score may still be favored over genomes with better predicted score that are "very similar" to the current population.

***Note:*** The distance computations may have a huge impact on the duration of the crossover-phase. You can disable the computations by setting `--maxRanksCompensatedByDistance=0` _(not recommended)_. <br/>
Alternatively, you can reduce the number of features that are are considered during the distance computation via the parameter `--featureSubsetRatioForDistance`. Fewer number of considered features results in a shorter computation time _(recommended)_.

### distanceMetric
You can select a distance metric from the `Optano.Algorithm.Tuner.Data.Configuration.DistanceMetric` enum:
-  L1Average
    - Average of the sum of absolute differences between each of the features.
    - Also known as Minkowski distance, or Taxicab geometry.
- HammingDistance
    - Counts the number of _different_ features between the potential offspring and the current population.
    - Uses the sum of the `k=3` smallest hamming distances (i.e. to the "closest" genomes) as reported distance for a potential offspring.
    - Two features are considered to be different, if their relative difference exceeds `--hammingDistanceRelativeThreshold` (i.e. a _"small"_ number).

### forestFeaturesPerSplitRatio
This value controls the ratio of the genome's <i>double[]</i>-representation will be "shown" to the trees that are used in the model-based crossover operator. The length of the <i>double[]</i>-representation depends on your parameter tree and the chosen <i>categorical encoding</i>. The length of a categorical domain's <i>double[]</i>-representation varies between different encodings. E.g., for the `OrdinalEncoding`, all categorical feature lengths will be <i>1</i>. For the `OneHotEncoding`, the length will be equal to the number of categorical values in a respective category (i.e. 1 column per possible domain value).

A rule of thumb (according to literature) is to train trees using feature **counts** of about <i>#features/3</i> or <i>sqrt(#features)</i>. Since you need to specify the **ratio**, those numbers need to be divided by the number of features, yielding a `--forestFeaturesPerSplitRatio` of <i>1/3</i> or <i>1/sqrt(#features)</i>. We decided to use ratios because this makes it easier to provide "meaningful" (default-)values without actually knowing the _exact_ number of features.