# Parameters for *OPTANO Algorithm Tuner*

*OPTANO Algorithm Tuner* (*OAT*) combines a small number of required parameters with a rich interface for configuration. This page describes all possible parameters and their default values.

When using *OAT* in a [distributed fashion](distributed.md), multiple instances can be started as workers to speed up the tuning. The possible parameters for workers can be found in a [separate section](#worker) at the end of the page. 

Note that when you execute a tuning in [several sessions](statusdump.md), most parameters are adopted from your previous session.

Finally, check the [parameter selection](parameter_selection.md) page for some general advice on how to choose some of the described parameters.

## Master

<dl>
 <dt>--help (-h)</dt>
 <dd>Prints information about usage to console.</dd>
</dl>

### <a name="required"></a>Required parameters
Most parameters in *OAT* have a default value, but you have to specify the parameters that depend on your hardware. As part of the tuning, your algorithm is run many, many times with different configurations. You need to specify how many of these runs can be conducted in parallel.

When comparing configurations, the default [GGA algorithm](algorithms.md#gga) executed by *OAT* uses mini tournaments with a certain number of competitors. For optimal tuner run time, all of these should be run in parallel, i.e. the number of competitors should equal the number of maximum parallel evaluations. Use `--cores` to set this number. However, there might be reasons why you would want to have different numbers, e.g. if you do not have a sufficient amount of cores or your algorithm needs several cores to work. In those cases, you can use `--maxParallelEvaluations` and `--miniTournamentSize` instead.

Note: If the mini tournament size is very small, the [racing framework](#racing) is not very effective and tuning might take longer.

<dl>
 <dt>--cores={NUMBER} (-c)</dt>
 <dd>The number of cores per node. Used to set both --maxParallelEvaluations and --miniTournamentSize.<br>
 Either this or --maxParallelEvaluations must be specified.</dd>

 <dt>--maxParallelEvaluations={NUMBER}</dt>
 <dd>The maximum number of parallel target algorithm evaluations per node.<br>
 Either this or --cores must be specified.</dd>
</dl>

Remarks:
- If `--maxParallelEvaluations` or `--miniTournamentSize` are used at the same time as `--cores`, they will override the values given by `--cores`.
- When executing a tuning in [several sessions](statusdump.md), you may only use `--maxParallelEvaluations`, because `--miniTournamentSize` is always adopted from the previous session.

### Address
*OAT* will try to automatically detect your computing nodes' fully qualified domain names and use them for exchanging messages. If you experience connection problems on your system, you should try to set the host names explicitely.

In addition, it is possible to specify the port on which the master listens for worker connections.

<dl>
 <dt>--ownHostName={HOSTNAME}[Fully Qualified Domain Name]</dt>
 <dd>The address that the master uses for incoming messages. On some systems the FQDN cannot be resolved on the fly. In that case, please provide the FQDN or an IP address.</dd>
 <dt>--port={NUMBER} [8081]</dt>
 <dd>The port on which the master listens for worker connections. Must be identical for master and respective workers, but different for different parallel runs.</dd>
</dl>

### Target Algorithm Specific Parameters
In addition to the required parameters, you will often want to change parameters that are closely connected to the algorithm you are tuning the parameters for.

In each iteration, configurations are evaluated using a random subset of the provided training instances. The size of these subsets increases throughout the run such that the best configurations which we still evaluate in later iterations have been evaluated on the largest number of instances. The exact number of instances to use can be specified via parameters.

<dl>
 <dt>--trainingInstanceFolder={PATH}</dt>
 <dd>The complete path to the folder containing instances. Handled differently by each application.<br>
Note: For most applications, if you execute <i>OAT</i> in a distributed fashion, you will have to make sure that all computing nodes either can access the same instance folder or have identical instance folders stored at identical paths.</dd>

 <dt>--testInstanceFolder={PATH}</dt>
 <dd>Similar to <code>--trainingInstanceFolder</code>, but only used for <a href="logging.md">additional information on incumbent quality</a> in case <code>--scoreGenerationHistory</code> is provided.</dd>

 <dt>--instanceNumbers={NUMBER:NUMBER} (-i) [5:100]</dt>
 <dd>The number of instances to use at the first generation and the number of instances to use at the end. Instance numbers increase linearly from the start until <i>goalGen</i> and then stay at maximum size until the end of the tuning.</dd>
</dl>

#### <a name="racing"></a>Speedups in Runtime Tuning
**Racing**: In runtime tuning, *OAT* is able to skip evaluating a configuration further once it becomes clear that it won't get to be a winner of the mini tournament it is part of. This strategy has the potential to greatly reduce the tuner's run time because the worst configurations are evaluated less often. However, if you are not tuning for run time or if another criterium is more important for you, you should not enable this functionality.

**CPU Timeout**: Racing is useful, but only checks the run times between algorithm runs. That means that a really long run will never be cancelled, and the bad configuration will only be ignored afterwards. To avoid this problem, a CPU timeout can be set per run. Runs will be cancelled after this time and the run time will be counted as the run time until cancellation. If you want to handle cancelled runs differently from others, you can still use racing - all configurations that cause CPU timeouts will be ignored when deciding whether a configuration should be evaluated further.

<dl>
 <dt>--enableRacing={BOOLEAN} [false]</dt>
 <dd>Value indicating whether racing should be enabled.</dd>

 <dt>--cpuTimeout={SECONDS} (-t) [int.MaxValue]</dt>
 <dd>The CPU timeout per target algorithm run in seconds.</dd>
</dl>

### Scale of the Tuning
*OAT* uses [evolutionary algorithms](algorithms.md) as the basis for tuning. They can be scaled by their population size and termination criteria.

<dl>
<dt>--popSize={NUMBER} (-p) [128]</dt>
 <dd>The total population size in the genetic algorithm.</dd>
	
 <dt>--numGens={NUMBER} (-g) [100]</dt>
 <dd>The number of generations to execute in the genetic algorithm.</dd>

 <dt>--goalGen={INDEX} [74]</dt>
 <dd>The first generation (0-indexed) at which the maximum number instances per genome evaluation will be reached.</dd>

 <dt>--evaluationLimit={NUMBER}[2147483647]</dt>
 <dd>A maximum number of (configuration - instance) evaluations after which the program terminates.</dd>

 <dt>--maxParallelThreads={NUMBER}[maxParallelEvaluations]</dt>
 <dd>A maximum number of parallel threads to process parallelizable task related to the tuner algorithm. For example this parameter is used to speed up the genetic engineering. If not specified, maxParallelEvaluations is used.</dd>
</dl>

### <a name="master-output-parameters"></a>Logging
<dl>
 <dt>--verbose={0, 1, 2, 3} (-v) [1]</dt>
 <dd>The verbosity level. 0 only prints warnings, 1 regurlarly prints some status information, 2 prints more detailed information, e.g. calls to the target mechanism, and 3 is for debugging.<br/>
Note that workers have the same parameter for their own ouput.</dd>
 <dt>--statusFileDir={ABSOLUTE_PATH} [<i>current directory</i>/status]</dt>
 <dd>Directory to which intermediate results should be written to. If --continue is set, this is also the path from which the intermediate results will be read.</dd>
 <dt>--zipOldStatus={BOOL} [false]</dt>
 <dd>Whether to zip old status files. Otherwise, old status files are overwritten.</dd>
 <dt>--logFile={ABSOLUTE_PATH} [<i>current directory</i>/tunerLog.txt]</dt>
  <dd>Path to which log file should be written to after every generation.</dd>
 <dt>--trackConvergenceBehavior</dt>
  <dd>Add to create <code>averageConvergence.csv</code> <a href="logging.md">logging file</a>.<br/>
 <dt>--scoreGenerationHistory</dt>
  <dd>Add to create <code>scores.csv</code> <a href="logging.md">logging file</a>.<br/>
Can significantly increase total software runtime as it adds a post-processing phase. However, best parameterization is printed before that phase.</dd>
</dl>

### Fault Tolerance
<dl>
 <dt>--faultTolerance={NUMBER} [3]</dt>
 <dd>Maximum number of consecutive failures in an evaluation before the affected instance of <i>OAT</i> is stopped. The most recent exception will be written to its console. If the stopped instance did not act as master, the overall tuning will continue.</dd>

 <dt>--maxRepair={NUMBER} [20]</dt>
 <dd>The maximum number of attempts to repair a genome if it is invalid after crossover or mutation. Only relevant if you have implemented a <code>GenomeBuilder</code>.</dd>

 <dt>--strictCompatibilityCheck={BOOLEAN} [true]</dt>
 <dd>Option to turn off / on the compatibility check between the current and old configuration in case of a <a href="statusdump.md">continued run</a>. Use with care.</dd>
</dl> 

### Tuning Algorithm
By default, *OAT* makes use of the [GGA algorithm](algorithms.md#gga). There exist parameters to switch to a different tuning algorithm or even [hybridize](algorithms.md#hybridization) them.

<dl>
 <dt>--jade</dt>
 <dd>Activates JADE.</dd>

 <dt>--cmaEs</dt>
 <dd>Activates CMA-ES.</dd>

 <dt>--maxGenerationsPerGgaPhase={GENERATIONS} [int.MaxValue]</dt>
 <dd>The maximum number of GGA(++) iterations to execute before switching to an alternative tuning algorithm. Set to 0 for pure JADE or CMA-ES.</dd>

 <dt>--maxGgaGenerationsWithSameIncumbent={GENERATIONS} [int.MaxValue]</dt>
 <dd>The maximum number of consecutive GGA generations not finding a new incumbent. If met, <i>OAT</i> switches to an alternative tuning algorithm. Set to a value greater or equal the total generation number to turn off this criterion.</dd>

 <dt>--maxGenerationsPerDePhase={GENERATIONS} [int.MaxValue]</dt>
 <dd>The number of JADE iterations to execute before starting a GGA(++) phase. Set to a value greater or equal the total generation number for pure JADE.</dd>

 <dt>--maxGenerationsPerCmaEsPhase={GENERATIONS} [int.MaxValue]</dt>
 <dd>The number of CMA-ES iterations to execute before starting a GGA(++) phase. Set to a value greater or equal the total generation number for pure CMA-ES.</dd>

<dt>--focusOnIncumbent={BOOL} [false]</dt>
<dd>Whether JADE/CMA-ES should focus on improving the incumbent or work on the complete population. Focus on incumbent is not possible without GGA(++) phases and not recommended for target algorithms without any categorical parameters.
</dd>
</dl>

### <a name="genetic-algorithm"></a>Gender-based Genetic Algorithm
By default, *OAT* uses [GGA](algorithms.md#gga), a genetic algorithm, as the basis for tuning. Therefore, some genetic algorithm parameters can be set:
<dl>

 <dt>--miniTournamentSize={NUMBER} [8]</dt>
 <dd>The maximum number of participants per mini tournament.<br/>
For optimal tuner run time, all of these should be run in parallel, i.e. the number of competitors should equal the number of maximum parallel evaluations. However, there might be reasons why you would want to have different numbers, e.g. if you do not have a sufficient amount of cores or your algorithm needs several cores to work.<br/>
Note: If the mini tournament size is very small, the racing framework is not very effective and tuning might take longer.
</dd>

 <dt>--maxGenomeAge={NUMBER} [3]</dt>
 <dd>The number of generations a genome survives in the genetic algorithm.</dd>

 <dt>--winnerPercentage={PERCENTAGE} (-w) [0.125]</dt>
 <dd>The percentage of winners per mini tournament. These are the competitive genomes allowed to reproduce.</dd>

 <dt>--enableSexualSelection={BOOL} [false]</dt>
 <dd>Set a value indicating whether an attractiveness measure should be considered during the selection of non-competitive mates. The attractiveness of a genome refers to the rank that is predicted for it by the same model that can also be used for the <a href="#model-based-parameters">model based crossover</a></dd>

 <dt>--mutationRate={RATE} (-m) [0.1]</dt>
 <dd>The probability that a parameter is mutated in the genetic algorithm.</dd>

 <dt>--mutationVariance={PERCENTAGE} [0.1]</dt>
 <dd>The percentage of the variable's domain that is used to determine the variance for Gaussian mutation.</dd>

 <dt>--populationMutantRatio={RATIO} [0.25]</dt>
 <dd>The ratio of the non-competitive population that gets replaced by random mutants after every generation. Value is only used if <code>--engineeredProportion</code> is set to a positive value.</dd>

 <dt>--crossoverSwitchProbability={PROBABILITY} [0.1]</dt>
 <dd>The probability that we switch between parents when doing a crossover and deciding on the value of a parameter that has different values for both parents and has a parent parameter in the parameter tree which also has different values for both parents.</dd>

<dt>--trainModel</dt>
 <dd>Add if a performance model should be trained even if genetic engineering and sexual selection are turned off.</dd>
</dl>

#### <a id="model-based-parameters" name="model-based-parameters"></a>[Model-Based Crossover](model_based_crossover.md) Parameters
*OAT* also implements [GGA++](algorithms.md#gga-1) by providing a model-based crossover operator that can be used for creating new offspring. Note that some of the parameters can have a significant impact on the time consumed for the population update. The application of this operator is also referred to as _Genetic Engineering_.
<dl>
  <dt>--engineeredProportion={PERCENTAGE} [0]</dt>
  <dd>The proportion of offspring that should be engineered by using the model-based crossover operator.</dd>

  <dt>--startIterationEngineering={NUMBER} [3]</dt>
  <dd>Sets the iteration number in which the genetic engineering should be incorporated in the tuning.</dd>

  <dt>--targetSampleSize={NUMBER} [125]</dt>
  <dd>Sets the number of random samples to generate per reachable leaf node during genetic engineering.</dd>

  <dt>--distanceMetric={DistanceMetric} [HammingDistance]</dt>
  <dd>Sets the distance metric to use during genetic engineering. Score for offspring will be a combination of predicted performance and the "uniqueness" (i.e. distance to current population).</dd>

  <dt>--maxRanksCompensatedByDistance={NUMBER} [1.6]</dt>
  <dd>Sets the influence factor for the 'distance' between a potential offspring and the existing population when scoring the potential offspring. All measured distances will be normalized to the range [0, maxRanksCompensatedByDistance] when computing the offspring score.</dd>
    
  <dt>--featureSubsetRatioForDistance={RATIO} [0.3]</dt>
  <dd>Distances between Genomes during genetic engineering are only computed over a subset of the features. The subset of the given size selected at random. </dd>

  <dt>--hammingDistanceRelativeThreshold={RATIO} [0.01]</dt>
  <dd>Sets the relative threshold above which two compared genomes are considered to "be different" in a certain feature.</dd>

  <dt>--crossoverProbabilityCompetitive={PROBABILITY} [0.5]</dt>
  <dd>Sets the probability with which a non-fixed parameter will be selected from the **competitive** genome during the genetic engineering. Can be used to "stir" the engineered genomes away from "known" high-quality areas of the search space (i.e. set the probability < 0.5).</dd>

  <dt>--topPerformerThreshold={PERCENTAGE} [0.1]</dt>
  <dd>Sets the proportion of genomes that are considered to be 'top performers' during model based crossover. Only applied when <code>TopPerformerFocusSplitCriterion</code> is used.</dd>
</dl>

  The _Model-Based Crossover_ uses a random forest in order to predict the behavior of a candidate offspring. The following parameters modify the **random forest**:

<dl>
  <dt>--forestTreeCount={NUMBER} [75]</dt>
  <dd>The number of trees in the random forest.</dd>

  <dt>--forestFeaturesPerSplitRatio={RATIO} [0.3]</dt>
  <dd>The percentage of features to use per split during the training of the trees.<br/>
  It is <b>recommended</b> that this number is set to a value of approx. <i>1/3</i>, or <i>1/sqrt(#features)</i>.<br/>
  The <i>"#features"</i> refers to the length of your parameter tree's double[]-representation. Setting <i>"forestFeaturesPerSplitRatio"</i> to <i>1/sqrt(#features)</i> means that the trees will be trained with <i>#features * 1/sqrt(#features) = sqrt(#features)</i>, which is a commonly used value according to the literature.</dd>

  <dt>--forestMaxTreeDepth={NUMBER} [10]</dt>
  <dd>The maximum depth of a tree.</dd>

  <dt>--forestMinSplitSize={NUMBER} [2]</dt>
  <dd>The minimum size of a split in a tree.</dd>

  <dt>--forestMinInformationGain={NUMBER} [1e-6]</dt>
  <dd>The minimum information gain for a split in the trees.</dd>

  <dt>--forestSubSampleRatio={RATIO} [0.7]</dt>
  <dd>The proportion of the training set that is passed to each tree during training.</dd>

  <dt>--forestRunParallel={BOOL} [true]</dt>
  <dd>Enables parallel training of the random forest.</dd>
</dl>

### JADE Parameters
As detailed on the [page about tuning algorithms](algorithms.md), it is possible to employ the JADE parameter tuner instead of the default GGA(++) one.

Note that JADE only acts on the competitive population part, i.e. it is effectively exploiting only half of the specified population size.

<dl>
 <dt>--jade</dt>
 <dd>Activates JADE.</dd>

 <dt>--maxGenerationsPerGgaPhase={GENERATIONS} [int.MaxValue]</dt>
 <dd>The number of GGA(++) iterations to execute before starting a JADE phase. Set to 0 for pure JADE.</dd>

 <dt>--maxGgaGenerationsWithSameIncumbent={GENERATIONS} [int.MaxValue]</dt>
 <dd>The maximum number of consecutive GGA generations not finding a new incumbent. If met, <i>OAT</i> switches to a JADE phase. Set to a value greater or equal the total generation number to turn off this criterion.</dd>

 <dt>--maxGenerationsPerDePhase={GENERATIONS} [int.MaxValue]</dt>
 <dd>The number of JADE iterations to execute before starting a GGA(++) phase. Set to a value greater or equal the total generation number for pure JADE.</dd>

 <dt>--minDomainSize={NUMBER} [150]</dt>
 <dd>The minimum size an integer domain needs to have to be handled as continuous. Integer parameters with a smaller number of possible values are not tuned by JADE.</dd>

<dt>--focusOnIncumbent={BOOL} [false]</dt>
<dd>Whether JADE should focus on improving the incumbent or work on the complete population. Focus on incumbent is not possible without GGA(++) phases and not recommended for target algorithms without any categorical parameters.<br/>
Note: If activated, JADE only works on a quarter of the complete population.
</dd>

 <dt>--replacementRate={RATE} [0]</dt>
 <dd>Only used if <code>--focusOnIncumbent=true</code>. Specifies the percentage of competitive genomes which get replaced by the best search points found by JADE at the end of a phase. A replacement rate of 0 indicates that only the incumbent itself should be replaced.</dd>

<dt>--fixInstances</dt>
 <dd>Ensures that the set of instances to evaluate on stays the same during a JADE phase. Setting not recommended for longer phases.</dd>

 <dt>--bestPercentage={VALUE} [0.1]</dt>
 <dd>The percentage of population members which may be used as best member in the current-to-pbest mutation strategy. Smaller values speed up convergence, but might miss good configurations. Must be in the range of (0, 1].</dd>

 <dt>--meanMutationFactor={FACTOR} [0.5]</dt>
 <dd>The initial value of the mean mutation factor. Must be in the range of [0, 1].</dd>

 <dt>--meanCrossoverRate={RATE} [0.5]</dt>
 <dd>The initial value of the mean crossover rate. Must be in the range of [0, 1].</dd>

 <dt>--learningRate={RATE} [0.1]</dt>
 <dd>The learning rate for the means. Must be in the range of [0, 1].</dd>
</dl>


### CMA-ES Parameters
As detailed on the [page about tuning algorithms](algorithms.md), it is possible to employ the CMA-ES parameter tuner instead of the default GGA(++) one.

Note that CMA-ES only acts on the competitive population part, i.e. it is effectively exploiting only half of the specified population size.

<dl>
 <dt>--cmaEs</dt>
 <dd>Activates CMA-ES.</dd>

 <dt>--maxGenerationsPerGgaPhase={GENERATIONS} [int.MaxValue]</dt>
 <dd>The number of GGA(++) iterations to execute before starting a CMA-ES phase. Set to 0 for pure CMA-ES.</dd>

 <dt>--maxGgaGenerationsWithSameIncumbent={GENERATIONS} [int.MaxValue]</dt>
 <dd>The maximum number of consecutive GGA generations not finding a new incumbent. If met, <i>OAT</i> switches to a CMA-ES phase. Set to a value greater or equal the total generation number to turn off this criterion.</dd>

 <dt>--maxGenerationsPerCmaEsPhase={GENERATIONS} [int.MaxValue]</dt>
 <dd>The number of CMA-ES iterations to execute before starting a GGA(++) phase. Set to a value greater or equal the total generation number for pure CMA-ES.</dd>

<dt>--focusOnIncumbent={BOOL} [false]</dt>
<dd>Whether CMA-ES should focus on improving the incumbent or work on the complete population. Focus on incumbent is not possible without GGA(++) phases and not recommended for target algorithms without any categorical parameters.
</dd>
 
 <dt>--minDomainSize={NUMBER} [150]</dt>
 <dd>Only used if <code>--focusOnIncumbent=true</code>. The minimum size an integer domain needs to have to be handled as continuous. Integer parameters with a smaller number of possible values will not be tuned.</dd>

 <dt>--replacementRate={RATE} [0]</dt>
 <dd>Only used if <code>--focusOnIncumbent=true</code>. Specifies the percentage of competitive genomes which get replaced by the best search points found by CMA-ES at the end of a phase. The original incumbent is never replaced.</dd>

<dt>--fixInstances</dt>
 <dd>Ensures that the set of instances to evaluate on stays the same during a CMA-ES phase. Setting not recommended for longer phases.</dd>

 <dt>--initialStepSize={SIZE} [3]</dt>
 <dd>The step size with which to start CMA-ES phases. If using hybrid approaches, a smaller step size, e.g. 0.5, is recommended. Note that we standardize all values to fall into [0,10] at the start of a CMA-ES phase.</dd>
</dl>

## Worker
As *OAT* instances that are started as workers connect with a master and get most information from there, they need almost no parameters. Essentially, all they need to know is how to connect to the master. You may additionally change how much information they print to console.

### Connection to Master
<dl>
 <dt>--seedHostName={HOSTNAME} (-s)</dt>
 <dd>The host name of the node the master is running on.</dd>

 <dt>--port={NUMBER} (-p) [8081]</dt>
 <dd>The port on which the master listens for worker connections. Must be identical for master and respective workers, but different for different parallel runs.</dd>
</dl>

### Own Address
*OAT* will try to automatically detect your computing nodes' fully qualified domain names and use them for exchanging messages. If you experience connection problems on your system, you should try to set the host names explicitely.
<dl>
 <dt>--ownHostName={HOSTNAME}[Fully Qualified Domain Name]</dt>
 <dd>The address that the worker uses for incoming messages. On some systems the FQDN cannot be resolved on the fly. In that case, please provide the FQDN or an IP address.</dd>
</dl>

### <a name="worker-output-parameters"></a>Output
<dl>
 <dt>--verbose={0, 1, 2, 3} (-v) [1]</dt>
 <dd>The verbosity level. 0 only prints warnings, 1 prints some status information, 2 prints more detailed information, e.g. calls to the target mechanism, and 3 is for debugging.</dd>
</dl>