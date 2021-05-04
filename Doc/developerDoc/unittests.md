# Unittests

This page is a collection of technical prerequisites and known issues, that need to be considered when executing the unittests of *OPTANO Algorithm Tuner* and its [adapters](examples.md).

## Additional Files

In order to build the *OPTANO Algorithm Tuner* examples and run their unittests, you need to download some additional tools and add them to the provided `Tools` directory. The following table provides detailed information about these tools and their area of application within the unittests.

Adapter | Needed File | Our Recommendation | Unittest Class | Name in Code
--- | ---| --- | --- | ---
Generic *OAT* Application (resp. SAPS) | target algorithm executable `ubcsat.exe` | Use UBCSAT version 1.1.0, available at [UBCSAT homepage](http://ubcsat.dtompkins.com/downloads). | `TestUtils.cs` (resp. `SapsRunnerTests.cs`) | `PathToTargetAlgorithm` (resp. `PathToExecutable`)
BBOB | bbobbenchmarks python script `bbobbenchmarks.py`  | Use `bbobbenchmarks.py` from BBOB 2013, availabe at [COCO homepage](https://coco.gforge.inria.fr/doku.php?id=bbob-2013-downloads). | --- | ---
Gurobi  | mps instance file `glass4.mps` | Use `glass4.mps`, available at [MIPLIP homepage](https://miplib.zib.de/instance_details_glass4.html). | `GurobiRunnerTests.cs` | `PathToTestInstance`
Lingeling  | lingeling executable `lingeling` | Use lingeling version `lingeling-bbc-9230380-160707` from SAT competition 2016, available at [JKU homepage](http://fmv.jku.at/lingeling/#download). | `LingelingRunnerTests.cs` | `PathToExecutable`

## Adapter specific Issues

### [Generic *OAT* Application](../userDoc/basic_usage.md) & [SAPS Adapter](saps.md)

Since `ubcsat.exe` is referenced in `PathToTargetAlgorithm` (resp. `PathToExecutable`) in the unittests of the Generic *OAT* Application (resp. SAPS adapter), you need to adapt these references, if you want to execute these unittests on an operation system other than Windows.

### [ACLib Adapter](aclib.md)

Some unittests of the ACLib adapter make use of a simple test scenario, written in python 2.7. These unittests will be skipped, if you don't have python 2.7 installed on your system - in a way so that it can be loaded from your [PATH](https://en.wikipedia.org/wiki/PATH_(variable)) environment variable. The ACLib unit test project will automatically use the python 2.7 version that it retrieves from the PATH environment variable.

### [BBOB Adapter](bbob.md)

Some unittests of the BBOB adapter make use of the BBOB python 2.7 adapter script `bbobeval.py`, provided in `Tools`. These unittests will be skipped, if you don't have python 2.7 together with the [NumPy python package](https://numpy.org) installed on your system - in a way so that it can be loaded from your [PATH](https://en.wikipedia.org/wiki/PATH_(variable)) environment variable. The BBOB unit test project will automatically use the python 2.7 version that it retrieves from the PATH environment variable.

Note that starting the actual tuning process still requires an explicit path to python, so that you can also run it in environments where you cannot modify the PATH variable.

### [Lingeling Adapter](lingeling.md)

Since Lingeling is written for execution on a Linux machine, you need to execute the unittests, provided by the Lingeling adapter, on a Linux machine. On operation systems other than Linux the affected unittests will be skipped.

Moreover do not forget to make `lingeling` and `lingelingMemoryLimited.sh` executable by using the command `chmod +x` before executing the unittests.

## TestApplication.dll

Some unittests of *OPTANO Algorithm Tuner* and its [adapters](examples.md) make use of the provided .Net Core 3.1 application `TestApplication.dll`, which takes on multiple roles.

- By calling `dotnet TestApplication.dll returnExitCode [int]` it will return the given integer as exit code.
- By calling `dotnet TestApplication.dll returnInput [args]` it will return the given arguments as line-by-line output, before returning exit code 0.
- By calling `dotnet TestApplication.dll idle [int]` it will sleep the given amount of seconds, before returning exit code 0.