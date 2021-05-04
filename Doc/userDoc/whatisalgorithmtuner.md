# What is _OPTANO Algorithm Tuner_?
_OPTANO Algorithm Tuner_ (*OAT*) is a .Net Standard 2.1 compliant .Net-API that helps tuning any given target algorithm. The tuning can be executed for arbitrary optimization functions and has especially good performance for runtime optimization. It can deal with tuning continuous, discrete and categorical parameters and is able to consider relationships between them.

Moreover it provides [several different tuning algorithms](algorithms.md): GGA and GGA++ for general-purpose tuning, and JADE and active CMA-ES as specialized algorithms for continuous parameters. *OAT* already comes with an [out-of-the-box tuning application](basic_usage.md) that is able to handle runtime or performance tuning for algorithms accepting files as input data. It can be executed on a single computing node, but can also [easily be scaled up by starting additional workers](distributed.md).

## Features

*OAT* comes with:

- [Out-of-the-box algorithm tuning](basic_usage.md) for common optimization functions
- [.Net Standard 2.1](https://docs.microsoft.com/dotnet/standard/net-standard) compatibility
- [Multiple tuning algorithms](algorithms.md) to choose from
- [Simple specification](basic_usage.md#xml) of tuneable parameters and their relations via XML
- Scaleability across [multiple devices](distributed.md)
- Small number of [required parameters](parameters.md#required)
- [Possibility to pause and continue](statusdump.md) the tuning at a later point
- Automatic logging of intermediate results
- Full user control by offering a rich interface of [optional configuration parameters](parameters.md)
- [Full customization](../developerDoc/advanced.md) in form of arbitrary optimization functions, target algorithms, and input values
- Tolerance for [forbidden parameter combinations](../developerDoc/advanced.md#genomeBuilder)