# Advanced Usage of *OPTANO Algorithm Tuner*

To tune arbitrary algorithms for custom optimization functions, [basic usage](../userDoc/basic_usage.md) is not sufficient. Instead, you'll have to implement various interfaces and extend abstract classes to teach *OPTANO Algorithm Tuner* (*OAT*) how to tune your algorithm.

For optimal stability, we recommend employing *OAT* in .NET Core projects.

## Model-Based Crossover Operator
Besides the _"classical"_ genetic crossover, *OAT* also offers a model-based crossover operation. Details on how to use it can be found [here](model_based_crossover.md). You can customize the used machine learning models, data aggregators, and encoders for categorical domains by specifying the respective [_generic parameterization_](#model-based-custom) of the `Master{TTargetAlgorithm,TInstance,TResult,TLearnerModel,TPredictorModel,TSamplingStrategy}` class that you build in your custom code.

## Implement a subtype of `InstanceBase`
`InstanceBase` is an abstract class that represents your algorithm's input data.

If the algorithm you want to tune accepts a file as input, you can use the already implemented `InstanceFile` that stores a path to such a file. If you want to treat a combination of a given file and seed as a single "instance", you can use the `InstanceSeedFile`. Otherwise, you should write a class for your input data that extends `InstanceBase`.

`InstanceBase` requires you to implement `ToString`, `GetHashCode` and `Equals`. As your class will get serialized  if you are working in a [distributed fashion](../userDoc/distributed.md), your implementations may not depend on object references. Additionally, the class needs to be immutable to guarantee thread-safety.

## Implement a subtype of `ResultBase`

The abstract class `ResultBase` represents your algorithm's output and information about the run. It is used in `IRunEvaluator` to sort different configurations by quality.

`ResultBase` already contains two properties: an `int Runtime` storing the run's run time in milliseconds, and a `bool IsCancelled` that stores whether the run was cancelled due to CPU timeout (see [Parameters](../userDoc/parameters.md)). When extending it, you have to at least add a parameterless constructor.

You're free to add more information, of course. If you don't need any, you can use the existing `RuntimeResult` class. If you only need one additional continuous value, you can use the already implemented `ContinuousResult`.

Note that subtypes of `ResultBase` will be printed into log files. The default implementation of `ResultBase.ToString` returns the run's run time, possibly extended by a note that the run was cancelled.

Similar to `InstanceBase`, `ResultBase` objects will be serialized if you are working in a [distributed fashion](../userDoc/distributed.md). Therefore, they shouldn't get too large. In addition, your implementation has to be immutable to guarantee thread-safety.

## Implement `ITargetAlgorithm`
The class implementing `ITargetAlgorithm` is the one that calls the algorithm you want to tune. Different instances will be instantiated for different parameter combinations, but those will be used multiple times with different inputs.

The interface consists of a single method

	Task<TResult> Run(TInstance instance, CancellationToken cancellationToken)

where `TResult` is your `ResultBase` class and `TInstance` is your `InstanceBase` class.

The method needs to create a cancellable task that runs the algorithm on the given instance. The task run should already be started by the method. To enable correct CPU timeouts, the cancellation token should be checked regurlarly.

If the task gets cancelled, *OAT* will set the result's `Runtime` and `IsCancelled` properties for you. If it is not cancelled, you'll have to set `Runtime`.

**Example:** The `Run` method of the `ITargetAlgorithm` class for running the SAT solver SAPS might look like this:

```csharp
public Task<RuntimeResult> Run(InstanceFile instance, CancellationToken cancellationToken)
{
    // Define process to start SAPS.
    var processInfo = this.BuildProcessStartInfo(instance);

    return Task.Run(
        function: () =>
        {
            // Start process.
            using (var process = Process.Start(processInfo))
            using (var processRegistration = cancellationToken.Register(() => ProcessUtils.CancelProcess(process))
            {
                // Wait until end of process.
                process.WaitForExit();

                // If the process was cancelled, clean up resources and escalate it up.
                if (cancellationToken.IsCancellationRequested)
                {
                    this.CleanUp(process);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                // If the process was not cancelled, first check the output for CPU time.
                [...]

                // Then clean up resources.
                this.CleanUp(process);

                // Finally return CPU time as result.
                return new RuntimeResult(runtimeInMilliseconds: (int)(cpuTime * 1000));
            }
        },
        cancellationToken: cancellationToken);
}

```
The `ProcessUtils` class is part of *OAT* so you can use it yourself to cancel your process.
        
### Disposable Resources
If your class handles system resources that need to be released, it can implement `IDisposable`. In this case, *OAT* will make sure that it will get disposed as soon as possible.

## Implement `ITargetAlgorithmFactory`
*OAT* needs to know how to instantiate and configure your `ITargetAlgorithm` class. Therefore, you need to provide it with an `ITargetAlgorithmFactory` implementation that implements
	
	TTargetAlgorithm ConfigureTargetAlgorithm(Dictionary<string, IAllele> parameters)

where `TTargetAlgorithm` is your `ITargetAlgorithm` and the given parameters are the active subset of the parameters you defined for tuning. `IAllele` is a wrapper around objects that can be unwrapped via its `object GetValue()` function.

Note: `ITargetAlgorithmFactory` gets serialized over the network if *OAT* is used in a [distributed fashion](../userDoc/distributed.md).

## <a name="evaluator"></a>Implement `IRunEvaluator`

The `IRunEvaluator` interface is responsible for comparing multiple runs. Moreover you can implement a custom [racing strategy](../userDoc/parameters.md#racing) to speed-up the whole tuning. To implement the interface, you have to provide the following methods:

	IEnumerable<ImmutableGenomeStats<TInstance, TResult>> Sort(IEnumerable<ImmutableGenomeStats<TInstance, TResult>> allGenomeStatsOfMiniTournament)

which is responsible for your tuning metric and returns the genome stats of the best genomes (i.e. parameter combinations) first. Note that `TInstance` is your `InstanceBase` class and `TResult` is your `ResultBase` class.

	IEnumerable<ImmutableGenome> GetGenomesThatCanBeCancelledByRacing(
    IEnumerable<ImmutableGenomeStats<TInstance, TResult>> allGenomeStatsOfMiniTournament,
    int numberOfMiniTournamentWinners)

which is responsiple for your racing strategy and returns all genomes that should not be evaluated further due to [racing](../userDoc/parameters.md#racing).

**Remark:**  We strongly recommend to not cancel any target algorithm evaluations of possible mini tournament winners by racing, even if all other genome evaluations have been finished, to ensure that all mini tournament winners have seen all instances. That's why the tuner will throw an exception, if you want to cancel more genomes by racing than "number of mini tournament participants - desired number of mini tournament winners".

	double ComputeEvaluationPriorityOfGenome(ImmutableGenomeStats<TInstance, TResult> genomeStats, TimeSpan cpuTimeout)

which is responsiple for the evaluation priority queue of your racing strategy and returns the evaluation priority score of the current genome.

If your sorting depends on a single value, implementing `IMetricRunEvaluator` provides you with additional logging capabilities. It adds the method

	double GetMetricRepresentation(TResult result)

to the `IRunEvaluator` interface.

If you decided to use the already implemented `RuntimeResult` or `ContinuousResult`, you can use `SortByRuntime` resp. `SortByValue` (descending or ascending) instead of implementing the interface by yourself.

In case you do implement the interface yourself, note that the argument `allGenomeStatsOfMiniTournament` contains all genomes (i.e. parameter combinations) evaluated in the current generation / mini tournament along with their respective genome stats. If [racing](../userDoc/parameters.md#racing) is enabled (i.e. `--enableRacing=true`), the number of finished instances per genome may vary. Moreover The finished instances may include results cancelled due to CPU timeouts. For those the <see cref="ResultBase{TResult}.IsCancelled"/>-Boolean is true.

**Example:** You can find an example of a custom `IRunEvaluator` implementation in the [Gurobi example](gurobi.md).

## <a name="paramtree"></a>Build up a parameter tree
In order to tune your algorithm, *OAT* needs to be aware of the tuneable parameters and their relationships.

You have two possibilities to define these parameters:

1. **Write an XML document.** How to write the XML is already described in [Basic Usage](../userDoc/basic_usage.md). To [reference it from code](#put-it-all-together), you can call `ParameterTreeConverter.ConvertToParameterTree(<pathToXML>)`.
2. **Directly define the parameter tree in code.** Read the explanation about the XML document to learn about the properties and structure of the parameter tree. Then, you can create a new object of the `ParameterTree` class using the different `IParameterTreeNode` and `IDomain` classes.

If you cannot express your parameters by such a parameter tree alone, you should check whether additionally [implementing a `GenomeBuilder`](#genomeBuilder) solves your issues.

**Example:** For the SAT Solver Saps implemented in [ubcsat](http://ubcsat.dtompkins.com/), the parameter tree may be defined like this:

```csharp
public static ParameterTree CreateParameterTree()
{
    var alphaNode = new ValueNode<double>(
        AlphaIdentifier,
        new LogDomain(minimum: 1.01, maximum: 1.4));
    var rhoNode = new ValueNode<double>(
        RhoIdentifier,
        new ContinuousDomain(minimum: 0, maximum: 1));
    var pSmoothNode = new ValueNode<double>(
        PSmoothIdentifier,
        new ContinuousDomain(minimum: 0, maximum: 0.2));
    var wpNode = new ValueNode<double>(
        WpIdentifier,
        new ContinuousDomain(minimum: 0, maximum: 0.06));

    var rootNode = new AndNode();
    rootNode.AddChild(alphaNode);
    rootNode.AddChild(rhoNode);
    rootNode.AddChild(pSmoothNode);
    rootNode.AddChild(wpNode);

    return new ParameterTree(rootNode);
}
```
For a description on how to define the same tree via XML, take a look at the [Basic Usage](../userDoc/basic_usage.md) article.

### Optional: Use Indicator Parameters and Replacements
If your algorithm uses parameters that have a rather large domain (e.g. `x` from `0` to `int.MaxValue`), and some values are linked to a "very specific" behavior (e.g. `x == 0` disables "heuristic x", `x > 0` controls the intensity), it is helpful to introduce an _Indicator Parameter_ that lets the *OAT* **choose** (with equal probability) between _"disable x"_ and _"use heuristic with x > 0"_. Otherwise it is very unlikely, that the value `x = 0` will ever be evaluated.<br/>
The proper way for modeling this is described [here](parameter_replacement.md).

## Put it all together
After implementing these interfaces, you are ready to start tuning your algorithm. To do so, call

	Master<TTargetAlgorithm, TInstance, TResult>.Run(string[] args, Func<AlgorithmTunerConfiguration, string, string, AlgorithmTuner<TTargetAlgorithm, TInstance, TResult>> algorithmTunerBuilder)

resp.

	Worker.Run(string[] args)

where `TTargetAlgorithm` is your `ITargetAlgorithm` class, `TInstance` your `InstanceBase` class and `TResult` your `ResultBase` class.<br/>
`args` may contain all possible [*OAT* parameters](../userDoc/parameters.md).

The `algorithmTunerBuilder` is where you provide *OAT* with your specific classes. It takes an `AlgorithmTunerConfiguration` object, a path to a training instance folder, and a path to a test instance folder, if provided (all based on `args`), and returns an `AlgorithmTuner` object.

To instantiate an `AlgorithmTuner` object, you need to provide:

- an instance of your `ITargetAlgorithmFactory`
- an instance of your `IRunEvaluator`
- a set of `InstanceBase`s (which may be produced using the provided instance folder)
- your parameter tree and
- the configuration you a provided with.
- _Optional_:
    - If you have implemented a `GenomeBuilder`, you can provide that to the `AlgorithmTuner`'s constructor, too.
    - If you plan to use `--scoreGenerationHistory`, you can provide the `AlgorithmTuner` with test instances (which may be produced using the provided instance folder) by calling `AlgorithmTuner.SetTestInstances`

**Example**: An algorithm tuner builder for the MIP solver Gurobi:

```csharp
private static AlgorithmTuner<GurobiRunner, GurobiInstance, GurobiResult> BuildGurobiRunner(
    AlgorithmTunerConfiguration configuration, string pathToTrainingInstanceFolder, string pathToTestInstanceFolder)
{
    var tuner = new AlgorithmTuner<GurobiRunner, GurobiInstance, GurobiResult>(
        targetAlgorithmFactory: new GurobiRunnerFactory(),
        runEvaluator: new GurobiRunEvaluator(), 
        instances: GurobiUtils.CreateInstances(pathToInstanceFolder),
        parameterTree: GurobiUtils.CreateParameterTree(),
        configuration: configuration);
     
    if (testInstanceFolder != null)
    {
        tuner.SetTestInstances(GurobiUtils.CreateInstances(pathToTestInstanceFolder));
    }
     
    return tuner;
}
```
### <a id="model-based-custom"></a> Extension: Use a custom model-based crossover operator
The `Master<TTargetAlgorithm, TInstance, TResult>` and `AlgorithmTuner<TTargetAlgorithm, TInstance, TResult>` inherit from more complex base-classes that provide _three additional_ generic parameters, named `TLearnerModel`,`TPredictorModel`, and `TSamplingStrategy` with the following generic type constraints:
```csharp
    where TLearnerModel : IGenomeLearner<TPredictorModel, TSamplingStrategy>
    where TPredictorModel : IEnsemblePredictor<GenomePredictionTree>
    where TSamplingStrategy : IEnsembleSamplingStrategy<IGenomePredictor>, new()
```
These parameters specify which machine learning model should be used for the [model-based crossover](model_based_crossover.md) operator. By default, `Master<TTargetAlgorithm, TInstance, TResult>` uses the following setup:

```csharp
    Master<TTargetAlgorithm, TInstance, TResult> : 
        Master<TTargetAlgorithm, TInstance, TResult,
            GenomePredictionRandomForest<ReuseOldTreesStrategy>, 
            GenomePredictionForestModel<GenomePredictionTree>, 
            ReuseOldTreesStrategy>
        where TTargetAlgorithm : ITargetAlgorithm<TInstance, TResult> 
        where TInstance : InstanceBase 
        where TResult : ResultBase<TResult>, new()
```
This setup is the one that was used in the underlying [paper](https://www.ijcai.org/Proceedings/15/Papers/109.pdf). But the *OAT* also provides other random forest- and data aggregation implementations, for example a model that uses a _"default"_ implementation of a random forest, which does not use the special _split criterion_ that was described in the paper.

**Example:**
```csharp
    Master<TTargetAlgorithm, TInstance, TResult,
        StandardRandomForestLearner<ReuseOldTreesStrategy>,
        GenomePredictionForestModel<GenomePredictionTree>,
        ReuseOldTreesStrategy>
```
You can also provide your own implementations for the machine learning models. Just make sure to make them implement the specified interfaces:
- `IGenomeLearner<TPredictorModel, TSamplingStrategy>`
    - An algorithm that takes some aggregated (i.e. _sampled_) training data and trains a _predictor model_.
- `IEnsemblePredictor<GenomePredictionTree>`
    - A (tree-based) ensemble predictor that can predict the performance of `Genome`s that are represented by a `double[]` row in a matrix.
- `IEnsembleSamplingStrategy<IGenomePredictor>`
    - A method that filters and aggregates the data that should be used to train the current generation of predictor models, given all the observed tournament results.

## <a name="genomeBuilder"></a>Implement a `GenomeBuilder`
If the parameters you want to tune cannot be correctly represented by the syntax of a parameter tree, e.g. because some combinations are forbidden, you can still use *OAT* to tune your algorithm by modifying the methods that create or modify new parameter combinations (*Genomes*).

These methods are bundled in a class `GenomeBuilder` which you can inherit from. The ones you can override are:

- `bool IsGenomeValid(Genome genome)` decides whether a provided parameter combination is valid. Here, you can handle forbidden parameter combinations.
- `void MakeGenomeValid(Genome genome)` tries to repair a genome. The default implementation first checks for validity and then mutates parameter values one by one until the genome becomes valid or each parameter was mutated for a certain number of times without success, at which point it throws an exception. The threshold for this can be modified by a [command line parameter](../userDoc/parameters.md).<br/> Because you are aware of how `IsGenomeValid` is implemented, you can reimplement `MakeGenomeValid` in a more intelligent and therefore more efficient and less risky fashion.
- `Genome CreateRandomGenome(int age)` is responsible for creating configurations at the start of the tuning. The default implementation randomly chooses each parameter:
	
```csharp
    // Create genome with correct age.
    var genome = new Genome(age);

    // Randomly set each gene value.
    foreach (var parameterNode in parameterNodes)
    {
        genome.SetGene(parameterNode.Identifier, parameterNode.Domain.GenerateRandomGeneValue());
    }

    MakeGenomeValid(genome);
    return genome;
```

If your parameters are such that this strategy is very unlikely to succeed, you should modify the method `GenomeBuilder.MakeGenomeValid(Genome genome)` accordingly.

## Error Handling

When implementing *OAT* yourself, make sure to handle upcoming errors (e.g. crash of target algorithm) reasonable. Exemplary error handling is shown in the [advanced examples](examples.md).