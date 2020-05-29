# Cross Platform Execution of *OPTANO Algorithm Tuner*

Since *Optano Algorithm Tuner* provides a .Net Standard 2.1 project, all its adapters can be executed on the common operation systems, given an installation of a .Net Core 3.0 (or higher) runtime.

Useful commands of the .Net Core CLI are
- [dotnet build](https://docs.microsoft.com/dotnet/core/tools/dotnet-build) to build your project or solution.
- [dotnet test](https://docs.microsoft.com/dotnet/core/tools/dotnet-test) to execute the unittests of your project.
- [dotnet clean](https://docs.microsoft.com/dotnet/core/tools/dotnet-clean) to clean the output of your project or solution.
- [dotnet restore](https://docs.microsoft.com/dotnet/core/tools/dotnet-restore) to restore the dependencies of your project.

You can find detailed information about the .Net Core CLI in the [documentation of Microsoft](https://docs.microsoft.com/dotnet/core/tools/).

### Limit the memory of *OPTANO Algorithm Tuner* on a Linux machine
A bash script to limit the memory of each target algorithm call by *OPTANO Algorithm Tuner* on a Linux machine may look like ...

```
#!/bin/bash
ulimit -m $2
echo ulimit -m: $(ulimit -m)
ulimit -v $2
echo ulimit -v: $(ulimit -v)
echo $1
exec $1
```

Here, the command ``exec`` makes sure, that the execution of the executed target algorithm is stopped, when the `bash script` is terminated, e.g. because ``process.kill()`` is called by the *OPTANO Algorithm Tuner*, or because the memory limit is exceeded.

For more details on how to use such a bash script in an adapter of *Optano Algorithm Tuner*, please take a look on our [Lingeling example](lingeling.md).

### Troubleshooting: 
Do not forget to make your bash script executable by using the command ``chmod +x [PATH TO *.sh OF BASH-SCRIPT]`` before starting *OPTANO Algorithm Tuner*.
