# Examples

The following pages provide explanations for the concrete tuning implementations available at https://github.com/OPTANO/optano.algorithm.tuner.examples/.

##[SAPS](saps.md)

SAPS is a SAT solver with four independent parameters. Correcly choosing those parameters depending on your SAT instances can greatly reduce runtime.

In the setting of this example, it is possible to use [out-of-the-box](../userDoc/basic_usage.md) tuning provided by *OPTANO Algorithm Tuner*. However, [customizing <i>OPTANO Algorithm Tuner</i>](advanced.md) reduces overhead and comes with increased precision. For applications dealing with small SAT instances solvable in a few milliseconds, this can make a noticeable difference.<br>

The example code illustrates:

- how to build up a parameter tree in code
- how to start external processes from tuner code
- how to introduce custom common line parameters
- the basics of customizing *OPTANO Algorithm Tuner*

##[Gurobi](gurobi.md)

Gurobi is a solver for several mathematical program classes including linear programs (LP). It comes with a multitude of parameters and the goal of tuning them might be a complex tradeoff of success rate, quality and runtime.

Our example concentrates on tuning Gurobi on LPs, tuning for success rate first, then for quality and finally runtime.

In addition, to what is done in the [SAPS example](saps.md), the example code illustrates:

- how to build up a complex parameter tree
- how to deal with parameter dependencies not expressable through parameter trees
- how to read a parameter tree from XML
- how to clean up disposable resources in target algorithm runs
- how to implement a custom tuning metric and racing strategy
- how to integrate the innovative [gray box extension](gray_box_tuning.md) for *OAT* into custom adapters to minimize the overall tuning time
- an involved way to customize *OPTANO Algorithm Tuner*

##[Lingeling](lingeling.md)

Lingeling is a SAT solver with a large number of different parameters. Correcly choosing those parameters depending on your SAT instances can greatly reduce runtime.

In addition, to what is done in the [SAPS example](saps.md) and the [Gurobi example](gurobi.md), the example code illustrates:

- how to [limit the memory of each target algorithm run][memory_limited.md] on a Linux machine

##[BBOB](bbob.md)

Black-Box Optimization Benchmarking (BBOB) is part of the [Black Box Optimization Competition](https://bbcomp.ini.rub.de/) and includes different multi-dimensional objective functions, which act as a good benchmark for black-box optimization. We use these BBOB functions via a python adapter to evaluate the behaviour of our tuner.

##[ACLib](aclib.md)

The [Algorithm Configuration Library 2.0](https://bitbucket.org/mlindauer/aclib2) (ACLib) contains a large number of benchmark scenarios for algorithm tuning and defines a popular standard for parameter definitions, scenario descriptions and target algorithm interfaces. Our example provides you with a way to use _OPTANO Algorithm Tuner_ to execute many tuning problems formulated in that way.