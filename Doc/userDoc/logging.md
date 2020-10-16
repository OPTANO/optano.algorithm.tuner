# Logging

## Overview

_OPTANO Algorithm Tuner_ writes several files during the execution:

- `status.oatstat`, `ggaStatus.oatstat`, possibly other `...Status...` files
    - Contain the state of the `AlgorithmTuner` at the end of a generation.
    - The status files can be used to continue an [interrupted run](statusdump.md).
    - Directory of the status files can be changed with a [parameter](parameters.md)
    - If `--zipOldStatus=true` is provided, old status files are zipped instead of overwritten.
- `tunerLog.txt`
    - Contains information about the current run and the incumbent genome in a human-readable format (i.e. text).
    - Name and path of the file can be changed with a [parameter](parameters.md).
    - If `--zipOldStatus=true` is provided, old log files are zipped alongside status files.
- `consoleOutput_{Master/Worker}_{\d+}.log`
    - Contains all output that was logged during the run.
    - This includes all text written to the `Console`, and potentially additional information that was logged with a log level not visible on `Console`.
    - A separate file will be written for each _Master_ and _Worker_. The `{\d+}` is the Id of the thread that the respective agent runs in.
    - All logged output is written with the [NLog package](https://www.nuget.org/packages/NLog/). The layout and output options can be [customized](#customize-output).
- `generationHistory.csv`
    - Summarizes interesting information about each generation
    - Contains average incumbent scores if `scores.csv` file exists
	- Written without IncumbentTrainingScore and IncumbentTestScore after each generation
    - Computed and finally written at the end of the tuning
- `standardDeviation.csv`
    - Contains the development of the _standard deviation_ for each numerical feature, measured over the _competitive_ population.
    - It is written after each generation.
- `scores.csv`
    - Activate by `--scoreGenerationHistory`
    - Only available in combination with an `IMetricRunEvaluator`
    - Contains the average metric score of the incumbent genome, logged every 100 evaluations.
    - Supports a training and a test instance set
    - Computed and written at the end of the tuning
- `averageConvergence.csv`
    - Activate by `--trackConvergenceBehavior=true`	 
    - Only available in combination with an `IMetricRunEvaluator`
    - Contains the average metric score of the incumbent genome's `TResult` over the course of all generations
    - Differs from the values in `scores.csv` and `generationHistory.csv` as it averages over the instances used for evaluation in each generation, instead of all instances
    - Written at the end of the tuning

## <a id="customize-output"></a>Customize Logging Output
All output that is logged by the _OPTANO Algorithm Tuner_ **or** _Akka\.Net_ is logged via an [NLog Logger](http://nlog-project.org/).</br>
You can freely customize the output that is logged both by parametrization and by specifying your own logger configuration. This includes changing the target streams (e.g Console, File) as well as customizing the message format or the `LogLevel` at which certain output streams are used.

By default, _OPTANO Algorithm Tuner_ uses the following configuration:

```csharp
/// <summary>
/// Sets a common NLog configuration.
/// Logs to console + <paramref name="outputFilename"/>.
/// </summary>
/// <param name="outputFilename">
/// The output filename.
/// </param>
public static void Configure(string outputFilename)
{
    // Step 1. Create configuration object 
    var config = new LoggingConfiguration();

    // Step 2. Create targets and add them to the configuration 
    var consoleTarget = new ColoredConsoleTarget();
    config.AddTarget("console", consoleTarget);

    var fileTarget = new FileTarget();
    config.AddTarget("nlogOutput", fileTarget);

    // Step 3. Set target properties 
    consoleTarget.Layout = @"[${level}] ${date:format=HH\:mm\:ss.fff}-${logger}: ${message}";
    fileTarget.FileName = outputFilename;
    fileTarget.Layout = @"[${level}] ${date:format=HH\:mm\:ss.fff}-${logger}: ${message}";

    // Step 4. Define rules
    var rule1 = new LoggingRule("*", LogLevel.Info, consoleTarget);
    config.LoggingRules.Add(rule1);

    var rule2 = new LoggingRule("*", LogLevel.Trace, fileTarget);
    config.LoggingRules.Add(rule2);

    // Step 5. Activate the configuration
    LogManager.Configuration = config;
}
```

You can change the console's log level via the `--verbose` [parameter](parameters.md#master-output-parameters): 0 corresponds to `Warn`, 1 to `Info`, 2 to `Debug` and 3 to `Trace`. Additionally, 3 activates  detailed [Akka.NET](http://getakka.net/) logging.<br/>
You can also completely replace the `LogManager.Configuration` with your own configuration. For further details, have a look at the [NLog documentation](https://github.com/nlog/NLog/wiki/Configuration-API).