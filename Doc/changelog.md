# _OPTANO Algorithm Tuner_ Change-Log

_OPTANO Algorithm Tuner_ is a Genetic Algorithm implementation. It finds near-optimal parameters (a configuration) for any given target algorithm. 

## Version 1.0.0 (2021-03-25)

Changes:

- Improvement: Reworked parallelization of genome/instance evaluations.
- Feature: Racing rules can be customized by end-user.
	- By customization, Racing now can kill more evaluations, leading to a reduction of required compute time, while not altering any tournament-outcome (when compared to a tuning without racing).
	- Overall, a speedup of factor `2` up to `5` over version 0.9.1 is achieved. (In combination with the new parallelization.)
- Improvement: Further code clean ups + additional unit tests.

## Version 0.9.1 (2020-10-14)

Changes:

- Feature: Add option for specifying the default configuration of the target algorithm, in order to include it in the initial population when a new tuning is started.
    - See [notes on usage here](userDoc/basic_usage.md#xml) or refer to [Gurobi's](developerDoc/gurobi.md#parameter-tree) `parameterTree.xml` for an example on how to use the default value feature.
- Improvement: The new [download page](download.md) now provides a _self-contained_ version of the [Tuner.Application](userDoc/basic_usage.md) for `win-x64`, `linux-x64` and `osx-x64`.
- Improvement: Export the generation history after each generation.
    - Previously, the generation history was only exported after the tuning was finished (and the optional evaluation of the test set has been completed).
- Improvement: (More) Clean ups and refactoring for the [example projects](developerDoc/examples.md). E.g.:
    - [Gurobi](developerDoc/gurobi.md) now targets Gurobi 9.0 and supports parsing of compressed instance- and start solution files.
- Fix: The _logged_ age of the incumbent genome now is updated properly in the case that the incumbent did not change.
    - This issue did not affect the behavior/performance of OAT and was simply related to logging.

## Version 0.9.0 (2020-05-29)

Changes:

- Feature: Introduce [.Net Standard 2.1](https://docs.microsoft.com/dotnet/standard/net-standard) compatibility
- Feature: Add [out-of-the-box algorithm tuning](userDoc/basic_usage.md)  for common optimization functions
- Improvement: Reorganize [exemplary adapters](developerDoc/examples.md) for BBOB, Gurobi, Lingeling and SAPS
- Improvement: Remove unnecessary NuGet packages
- Improvement: Update referenced NuGet packages
    - Akka.Cluster 1.3.14 -> 1.4.3
    - Akka.Logger.NLog 1.3.3 -> 1.3.5
    - Akka.Serialization.Hyperion 1.3.14-beta -> 1.4.3
    - MathNet.Numerics 4.8.1 -> 4.9.0
- Improvement: Revise default parameters to improve overall tuning behaviour
- Feature: The `tunerLog.txt` logging file is now part of the zip archive created in case of `--zipOldStatus=true`
- Improvement: Improved default parameters
    - Default usage of standard random forest in [model-based crossover](developerDoc/model_based_crossover.md)
- Improvement: Update of referenced NuGet packages
    - Akka 1.3.2 -> 1.3.14
    - Akka.Cluster 1.3.2 -> 1.3.14
    - Akka.Logger.NLog 1.3.0-beta -> 1.3.3
    - Akka.Remote 1.3.2 -> 1.3.14
    - Akka.Serialization.Hyperion 1.3.2-beta54 ->1.3.14-beta
    - DotNetty.Buffers 0.4.7 -> 0.6.0
    - DotNetty.Codecs 0.4.7 -> 0.6.0
    - DotNetty.Common 0.4.7 -> 0.6.0
    - DotNetty.Handlers 0.4.7 -> 0.6.0
    - DotNetty.Transport 0.4.7 -> 0.6.0
    - Google.Protobuf 3.4.1 -> 3.9.1
    - Hyperion 0.9.6 -> 0.9.8
    - MathNet.Numerics 4.0.0-beta06 -> 4.8.1
    - Microsoft.NETCore.Platforms 2.0.1 -> 2.2.3
    - NETStandard.Library 2.0.1 -> 2.0.3
    - Newtonsoft.Json 9.0.1 -> 12.0.2
    - NLog 5.0.0-beta09 -> 4.6.7
    - System.Collections.Immutable 1.3.1 -> 1.5.0
    - System.Runtime.CompilerServices.Unsafe 4.4 -> 4.5.2
- Improvement: Better stability during remote execution (e.g. on clusters) due to several bug fixes in referenced libraries
- Fix: Elapsed time is now written to status file, resulting in correct logging of the sum of elapsed time in the case of [tuning in multiple sessions](userDoc/statusdump.md)
- Extended the package until April 1st.
- Feature: Added additional ways to [hybridize GGA(++) with JADE or CMA-ES](userDoc/algorithms.md#hybridization) which focus on improving the current incumbent.
    - Useful if tuning a mix of categorical and numerical parameters
    - Can be activated via command line: `--focusOnIncumbent=true`
- Feature: Added an additional adaptive termination criterion for GGA(++) phases when [hybridizing](userDoc/algorithms.md#hybridization).
    - Supports change of tuning algorithm dependent on tuning progress
    - Can be controlled via the `--maxGgaGenerationsWithSameIncumbent` argument
- Improvement: Improved default parameters for number of generations per strategy
    - Set to maximum integer value, since the number of generations is usually limited by overall parameter `numGens`
- Improvement: Parameter `generationsPerGgaPhase` renamed to `maxGenerationsPerGgaPhase`
- Improvement: Internal code cleanups.
 - Improved namespace logic comes with changes in namespaces for some classes.
- Extended the package until September 1st.
- Feature: Added additional tuning algorithms specialized for continuous parameters:
	-  [JADE](userDoc/algorithms.md#jade) 
	-  [Active CMA-ES](userDoc/algorithms.md#active-cma-es) 
- Feature: New option `--scoreGenerationHistory` to evaluate a tuner run on complete training and test set if it optimizes a numerical evaluation value
    - Average scores are [logged](developerDoc/logging.md) in two new logging files, `generationHistory.csv` and `scores.csv`
    - Calling `Master.Run` now requires the `AlgorithmTuner` factory method to take both a training and a test instance folder
    - Using the test set by calling `AlgorithmTuner.SetTestInstances` is optional
- Improvement: More information is logged, esp. on debug level 
    - Complete _OPTANO Algorithm Tuner_ configuration
     - All types inheriting from `ConfigurationBase` must now implement `toString`
    - Information about the total number of evaluations
    - Information about repair operations
- Improvement: Increased stability for distributed execution by employing smaller messages
- Improvement: Metric compare values are now defined by `IMetricRunEvaluator` instead of `IMetricResult`
- Improvement: Simplified support of status files. This should lead to less breaking changes in the future. 
    - Segmented status files. We now create multiple status files which are saved in a single directory. In console arguments, you can specify the directory instead of the status file name.
    - All types inheriting from `ConfigurationBase` must now implement the method `IsTechnicallyCompatible(ConfigurationBase other)`
    - All types implementing `IConfigBuilder` must implement `BuildWithFallback(ConfigurationBase fallback)`
- Improvement: New parameter `--zipOldStatus` to determine whether old status files should be zipped or overwritten. Default is false.
- Feature: The number of evaluations can be bounded via `--evaluationLimit`.
- Improvement: Additional information is written to output file, and verbosity options can be used to specify which log types become visible on console
- Improvement: Internal code cleanups.
    - Comes with an interface change for subclasses of `HelpSupportingArgumentParser{T}`. Use `this.InternalConfigurationBuilder` instead of `this.configurationBuilder` to fix any errors.
    - Improved namespace logic comes with changes in namespaces for many classes.
- Fix: By default, the status file now is written to the working directory instead of the directory that contains the tuner.exe.
- Fix: Fixed several minor issues regarding the console output. (E.g. Frequencies of output, formats, etc.)
- Improvement: Speedups in  [machine-learning based tuning](developerDoc/model_based_crossover.md).
- Improvement: Reduced amount of traffic sent over TCP between Master and Workers.
- Improvement: Clarified role of the `IRunEvaluator` interface both in the [user documentation](developerDoc/advanced.md#evaluator) and API.
- Fix: Evaluation settings were not properly updated on all external workers.
    - This led to unfair comparisons between configurations when *OPTANO Algorithm Tuner* was used in a [distributed fashion](userDoc/distributed.md).
- Fix: Target sampling was not performed for some reachable leaves during model based crossover.
- Fix: Console output is now written to working directory instead of *.exe directory.
- .NET Core 2.0 Compatibility
    - Running *OPTANO Algorithm Tuner*  via .NET Core comes with improved stability. We now recommend this setup for all projects, but especially if you are tuning dozens of parameters.
- Cleaner API
    - Clear naming
    - Complete [API documentation](api/index.md)
    - Merged `IComparer<IInstanceFile>` and `IInstanceFile` into `InstanceBase`
- Simplified  [status dumps](userDoc/statusdump.md) 
- Updated user documentation
	- especially with respect to [machine-learning based tuning](developerDoc/model_based_crossover.md)
- `parameterTree.xsd` and `SharpLearningCustom`  are now automatically added to builds
- Added native PAR-k evaluations.
- Logging
    - All console output generated by _OPTANO Algorithm Tuner_ is also written to consoleOutput.log. This can be changed using LoggingHelper.Write.
    - Export information about current incumbent genome to tunerLog.txt after each generation.
- Faulty target algorithm evaluations on distributed run will only cause the affected worker to stop, but the run will continue.
- Added "-ownHostName" parameter to explicitly tell Akka.Cluster via which channel it shall communicate with other cluster members.
    - If this option is omitted, the Fully Qualified Domain Name will be used.
    - Make sure to pass the FQDN of your master node as "--seedHostName" when you start the workers.
- Added beta-support for machine learning based tuning
    - Based on: [Ansótegui, Carlos, et al. "Model-Based Genetic Algorithms for Algorithm Configuration." IJCAI. 2015.](http://www.aaai.org/ocs/index.php/IJCAI/IJCAI15/paper/download/11435/10765)
    - Documentation will follow shortly.
- Update Akka.Cluster and dependencies from 1.2.3 to 1.3.2.
- Update Akka.Cluster and dependencies from 1.1.3 to 1.2.3.
    - The new version uses DotNetty instead of the deprecated Helios and comes with several bug fixes. Check the official [Akka release notes](https://github.com/akkadotnet/akka.net/releases) for detailed information.
- Initial Commit. 

Thanks for reading thoroughly!