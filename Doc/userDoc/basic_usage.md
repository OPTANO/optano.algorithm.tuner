# Basic Usage of *OPTANO Algorithm Tuner*

> [!NOTE] 
> [Download the Generic OPTANO Algorithm Tuner Application!](../download.md)


When using *OPTANO Algorithm Tuner* (*OAT*) in its basic version, it enables you to tune algorithms that

- Accept files as input
- Use parameters defined in certain ranges
- Accept these parameters in the form "-parameterName1 parameter1 ... -parameterNameN parameterN"

Tuning will either optimize for (penalized) run time or for the algorithm's output value.

## Provide your algorithm
To provide your algorithm, you have to provide a command to execute it with. That command may include two placeholders, {instance} and {arguments}. When evaluating a parameter combination, {instance} will be replaced with the path to an instance file and {arguments} with values for the parameters. These values will be written in the form "-parameterName1 parameter1 ... -parameterNameN parameterN".

Note: If you execute *OAT* in [distributed fashion](distributed.md), you have to make sure that all computing nodes are able to execute the command.

**Example:** To tune the SAT Solver SAPS implemented in [ubcsat](http://ubcsat.dtompkins.com/), one can provide the command

	[PATH]\ubcsat.exe -alg saps -i {instance} {arguments} -timeout 10 -cutoff max -seed 42

## Provide instances
It is assumed your algorithm takes files as input. To provide them to *OAT*, all you have to do is to make sure you have a folder that contains all the instances you want to use for tuning and nothing else.

**Example:** You may use [FuzzSAT](http://fmv.jku.at/fuzzsat/) as a generator for your SAT instances.

Note: If you execute *OAT* in [distributed fashion](distributed.md), you have to make sure that all computing nodes either can access the same instance folder or have identical instance folders stored at identical paths. Moreover note, that your instance folder should not contain other files, since *OAT* will treat them as instances.

## Provide parameters to tune
To provide parameters to tune, you have to write an XML file defining your parameters and their relationships. The complete schema definition can be found in parameterTree.xsd. The following paragraphs provide some context.

### <a name="xml"></a>Parameter definition
For each parameter, you need to provide

- the parameter's name as it is accepted in command line execution of your algorithm
- the parameter's type: Is it continuous, discrete, or categorical? If a parameter is defined as continuous or discrete, *OAT* assumes that close values indicate a similar setting, while that is not the case for categorical parameters.
- if the parameter is *continuous* or *discrete*, you can also define whether it is logarithmically or uniformly distributed
- if the parameter is *continuous* or *discrete*, you need to define its range
- if the parameter is *categorical*, you have to define the values it can take
- _OPTIONAL:_ Default Value
	- When the parameter's `defaultIndexOrValue` property is specified, this value will be used to create a _default_ genome, that will be part of the initial competitive population. <br/>I.e. if you wish to include the default configuration of the target algorithm in the tuning, you can do this by assigning the default value via the `defaultIndexOrValue` property to all parameters.
	- When the parameter is a `categorical` parameter, you need to set the 0-based index of the desired default value in the list of categorical values of the parameter's domain.
	- For discrete and continuous parameters, simply set the default value as `int` or `double` number. <br/>Make sure that this value lies within the min/max of the respective domain.
	- See the `parameterTree.xml` within the [Gurobi Adapter](gurobi.md) for an example usage.
	- Note: This feature also works _in-code_, when you're not specifying your parameter tree via xml. <br/>Simply pass the default value when calling a `Domain`'s constructor.

The parameter's name is specified via the `id` attribute, its type via the `domain` element. To have a numerical parameter be distributed logarithmically, `log` has to be set to `true`. Ranges are defined via `start` and `end`. Categorical values are set using one of the lists `doubles`, `strings`, `ints` or `booleans`.

### Relationship definition
*OAT* is able to work with parameters that are dependent on each other or which might even be turned off and on depending on another parameter's value. To make this possible, the parameter set has to be defined via an AND-OR-Tree:

- Each node either represents a parameter or an AND node
- If a parameter is a descendent of another parameter, it is dependent on that parameter
- Each node representing a parameter may have one child node
- To realize multiple childs, you can insert an AND node which does not represent a parameter and may have arbitrary many children
- Finally, an OR node represents a parameter that turns off and on other parameters: It always is categorical and you may assign a child to each of the values. When evaluating, only the subtree connected to the active value will be used in the {parameters} replacement.


**Example:** For the SAT Solver Saps implemented in [ubcsat](http://ubcsat.dtompkins.com/), the parameter tree file may look like this:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<!-- Tuneable parameters for ubcsat with algorithm SAPS. -->
<node xsi:type="and" xsi:noNamespaceSchemaLocation="parameterTree.xsd" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
	<node xsi:type="value" id="alpha">
		<domain xsi:type="continuous" log="true" start="1.01" end="1.4"/>
	</node>
	<node xsi:type="value" id="rho">
		<domain xsi:type="continuous" start="0" end="1"/>
	</node>
	<node xsi:type="value" id="ps">
		<domain xsi:type="continuous" start="0" end="0.2"/>
	</node>
	<node xsi:type="value" id="wp">
		<domain xsi:type="continuous" start="0" end="0.06"/>
	</node>
</node>
```
A more complicated parameter tree definition using the other properties and types defined above can be found in the [example source code for Gurobi tuning](gurobi.md).

## Provide what to tune for
By default, *OAT* will tune for small run time. If, in addtion, you provide the  `--parK` argument, e.g. `--parK=10`, the tuning will use timeout penalized average run time (PAR-K) and penalize timed-out runs by multiplying there actual run time by K. To make *OAT* exploit run time tuning in order to improve its own run time, set `--enableRacing=true`.

You can also configure *OAT* to tune the last number that your algorithm writes to console before exiting. This value can then either be minimized or maximized.

To enable value tuning, you need to provide `--byValue` as an argument. You can change minimization to maximization by setting `--ascending=false`.

Note that *OAT* assumes that all runs finish when `--byValue` is specified. Therefore, you should not set the `--cpuTimeout` parameter in this case.

## Putting it all together
To run *OAT* and tune your algorithm, you'll now simply have to call
	 
	dotnet Optano.Algorithm.Tuner.Application.dll --master -- maxParallelEvaluations=<maxParellelEvaluations> --basicCommand=<commandToExecuteYourAlgorithmWith> --parameterTree=<pathToXML> --trainingInstanceFolder=<instanceFolder> <optionalAdditionalParameters>

If you want to tune by value, add `--byValue`. If you want to exploit run time tuning, add `--enableRacing=true`.

Finally, if you would like to see how the parameterization quality changed throughout the tuning, add

	--scoreGenerationHistory
	
to acquire [respective logging files](logging.md).

In some cases, you might be interested in performance on a test set of instances. Then, create a second folder with test instances and add

	--testInstanceFolder=<instanceFolder>

### Additional Parameters

All parameters that can be specified either for master or for worker instances of *OAT* can be found at [Parameters](parameters.md). 

_Note:_ Already the basic implementation of the *OAT* enables you to use the [Model-Based Crossover Operator](model_based_crossover.md). The default setup the modified version of a random forest, that is described in the respective [paper](https://www.ijcai.org/Proceedings/15/Papers/109.pdf). More information on the [control parameters](parameters.md) and how to [specify them](parameter_selection.md#model-based-crossover) is given in the further documentation.
