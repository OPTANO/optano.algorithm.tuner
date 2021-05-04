# _OPTANO Algorithm Tuner_ Advanced Examples and Developer Documentation

If the [basic usage](../userDoc/basic_usage.md) provided for the *OPTANO Algorithm Tuner* (*OAT*) does not suffice for your requirements, you do have the option to implement a _custom adapter_ for your _Target Algorithm_.

This will, e.g., enable you to define a customized [evaluation metric](advanced.md#evaluator), build up your [parameter tree](advanced.md#paramtree) directly in `C#` code (instead of an `xml` file), call your algorithm via a `.NET` API (instead of an executable file), and much more.

## Implementing a Custom Adapter

Please follow this [guide](advanced.md) when implementing a custom adapter.<br/>
You can also have a look at the aleady [existing examples](examples.md).

## [Gray Box Tuning](gray_box_tuning.md)

This section contains detailed information about the innovative [gray box extension](gray_box_tuning.md) of *OAT* to minimize the overall tuning time.

* [Gray Box Tuning](gray_box_tuning.md)
* [Gray Box Simulation](gray_box_simulation.md)
* [Post Tuning Runner](post_tuning_runner.md)

## Existing Adapter Examples

Here is a collection of [existing adapters](examples.md) that are already implemented and ready to use, and/or serve as a guideline for your own custom adapter implementation.

If you'd like to *publish* your custom adapter, we're happy to include it in our list.<br/>
Please [contact us](https://optano.com/en/about-us/#contact) or send a [pull-request](https://github.com/OPTANO/optano.algorithm.tuner.examples)! :)

### [SAPS Tuning](saps.md)

### [Gurobi Tuning](gurobi.md)

### [Lingeling Tuning](lingeling.md)

### [BBOB Tuning](bbob.md)

### [ACLib Scenario Tuning](aclib.md)
