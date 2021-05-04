# Limiting the Memory for each Target-Algorithm Evaluation

Depending on your operation system, we recommend the following two ways to limit the memory of each target algorithm evaluation of *OPTANO Algorithm Tuner*.
	
## Using a bash script on a Linux machine

To limit the memory of each target algorithm evaluation of *OPTANO Algorithm Tuner* on a Linux machine, the following bash script can be used as a wrapper for the target algorithm.

```
#!/bin/bash
ulimit -m $2
echo ulimit -m: $(ulimit -m)
ulimit -v $2
echo ulimit -v: $(ulimit -v)
echo $1
exec $1
```

Here, the command ``exec`` makes sure, that the execution of the target algorithm is stopped, when the `bash script` is terminated, e.g. because ``process.kill()`` is called by the *OPTANO Algorithm Tuner*, or because the memory limit is exceeded.

For more details on how to use this bash script in an adapter of *Optano Algorithm Tuner*, please take a look on our [Lingeling example](../developerDoc/lingeling.md).

## Using a generic .Net core approach

It is not as easy as you'd expect to determine the exact amount of memory that is allocated by a process, when using dotnet core. The API provides means for determining the allocated memory by `Process Name`, however, when you're running multiple instances of the same application at once, this option is useless.

Based on a [blog post](https://weblog.west-wind.com/posts/2014/Sep/27/Capturing-Performance-Counter-Data-for-a-Process-by-Process-Id) by Rick Strahl, a possible solution for this problem might look similar to this:

The following utility method iterates all currently running threads for the given `process name`, checking if the `process id` matches the process that we'd like to observe. It then returns an instance of a [PerformanceCounter](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.performancecounter?view=net-5.0) that measures the used memory. The actual name of the process will look like this: `your_algo_executable#<NUMBER>`, where the `NUMBER` goes from `0 .. n-1`, with `n` being the current number of active evaluations:

```java
public class ProcessMemoryMeasue
{
    public static PerformanceCounter GetMemoryMeasureForProcessId(int processId, string processCounterName = "Working Set - Private")
    {
        var process = GetInstanceNameForProcessId(processId);
        if (string.IsNullOrEmpty(process))
        {
			return null;
		}

        return new PerformanceCounter("Process", processCounterName, process);
    }

    private static string GetInstanceNameForProcessId(int processId)
    {
        var process = Process.GetProcessById(processId);
        var processName = Path.GetFileNameWithoutExtension(process.ProcessName);

        var performanceCategory = new PerformanceCounterCategory("Process");
        var instances = performanceCategory.GetInstanceNames()
            .Where(instance => instance.StartsWith(processName))
            .ToArray();

        foreach (var instance in instances)
        {
            using var performanceCounter = new PerformanceCounter("Process", "ID Process", instance, true);
            var currentId = (int)performanceCounter.RawValue;
            if (currentId == processId)
			{
				return instance;
			}            
        }
        return null;
    }
}
```

This `PerformanceCounter` can then be used in a background task, to periodically check the current memory usage of the target algorithm that is evaluated your `ITargetAlgorithm.Run` method. _**Note:**_ Due to the "pain" of identifying a process to observe with the `PerformanceCounter`, this approach might "break" when another evaluation that has a lower `NUMBER` is completed during the LOC where the performance counter is created, and the line where the current value (`maxMemory = Convert.ToInt64(pc?.NextValue() ?? this._memoryLimitBytes + 1);`) is read.<br/>
To remedy this, it might be advised to double-check a newly instanciated performance counter, when the first one returns a memory violation.

```java
private Task<int> EnforceMemoryLimit(Process process)
{
    return Task<int>.Factory.StartNew(
        () =>
            {
                var maxMemory = 0L;

                // some memory limit that can be hard coded or parsed from param args
                if (!this._memoryLimitBytes.HasValue)
                {
                    return 0;
                }

                while (!process.HasExited)
                {
                    // set a delay
                    Thread.Sleep(1000);

                    // process name # changes when other processes are terminated. always re-create the performance counter.
                    lock (this._lock)
                    {
                        using var pc = ProcessMemoryMeasue.GetMemoryMeasureForProcessId(process.Id);
                        maxMemory = Convert.ToInt64(pc?.NextValue() ?? this._memoryLimitBytes + 1);
                    }

                    if (maxMemory > this._memoryLimitBytes)
                    {
                        var id = process.Id;
                        ProcessUtils.CancelProcess(process);
                        throw new InsufficientMemoryException(
                            $"Evaluation process with ID {id} consumes {maxMemory} Bytes of memory, while a limit of {this._memoryLimitBytes} Bytes was in place.");
                    }
                }

                return 0;
            });
}
```

Lastly, you need to start + observe the background task within your implementation of the `ITargetAlgorithm`'s `Run` method. Here is an example of this this might look like, based on our *OAT Application's* [`TimeMeasuringExecutor`](https://github.com/OPTANO/optano.algorithm.tuner.examples/blob/master/Source/Tuner.Application/Optano.Algorithm.Tuner.Application/TimeMeasuringExecutor.cs):

```java
/// <summary>
/// Creates a cancellable task that runs the <see cref="CommandExecutorBase{TResult}.Command"/> on the given instance.
/// </summary>
/// <param name="instance">Instance to run on.</param>
/// <param name="cancellationToken">Token that is regularly checked for cancellation.
/// If cancellation is detected, the task will be stopped.</param>
/// <returns>A task that returns the run's runtime on completion.</returns>
public override Task<RuntimeResult> Run(InstanceFile instance, CancellationToken cancellationToken)
{
    // Define process to target algorithm from command line.
    var processInfo = this.BuildProcessStartInfo(instance);

    return Task.Run(
        function: () =>
            {
                var timer = new Stopwatch();
                timer.Start();
                // Start process and make sure it's cancelled if the cancellationToken is cancelled.
                using (var process = Process.Start(processInfo))
                using (var processRegistration =
                    cancellationToken.Register(() => ProcessUtils.CancelProcess(process)))
                {
                    var limiter = this.EnforceMemoryLimit(process);
                    
                    // Wait until end of process.
                    process.WaitForExit();
                    bool cancelledByMemoryLimit = false;
                    try
                    {
                        limiter.Wait(cancellationToken);
                        cancelledByMemoryLimit = false;
                        limiter.Dispose();
                    }
                    catch (AggregateException e)
                    {
                        limiter.Dispose();
                        cancelledByMemoryLimit = true;
                        LoggingHelper.WriteLine(
                            VerbosityLevel.Warn,
                            CultureInfo.InvariantCulture,
                            $"Evaluation on instance {instance.ToString()} was aborted due to memory limit violation.\nMessage:{e.Message}");
                    }

                    // If the process was cancelled, escalate it up.
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    
                    // If the process has inappropriate exit code, clean up resources and return cancelled result.
                    if (process.ExitCode != 0 || cancelledByMemoryLimit)
                    {
                        return RuntimeResult.CreateCancelledResult(this._cpuTimeOut);
                    }

                    // If the process was not cancelled, return CPU time as result.
                    timer.Stop();
                    return new RuntimeResult(timer.Elapsed);
                }
            },
        cancellationToken: cancellationToken);
}
```

Now, whenever one of your target algorithm evaluations exceeds the memory limit that you have specified, the respective evaluation will be cancelled and treated similar to a timeout/cancelled result.