# Indicator Parameters and Replacements
Sometimes you may stumble across parameters that you cannot represent with a "simple" node in a `ParameterTree`. This is, for example, the case for parameters that you want to put into any kind `LogDomain`, but that also can have a value of `0`. The smallest feasible value within a `LogDomain` is `1`.<br/>
Even if you do not want to put your parameter into a `LogDomain`, it may still be helpful to treat some special cases separately: Imagine some _heuristic x_ that is controlled by the parameter `x >= 0`, where `x == 0` disables the heuristic and `x > 0` controls the intensity of the heuristic. If the upper bound for that parameter is rather large it becomes quite unlike that (given a uniform distribution), `x` will be randomly set to `0`. The `special case` of _"Disable heuristic x"_ differs _a lot_ from all the other possible values for `x`, and it is quite desirable to distinguish between _"x disabled"_ and _"Perform x with some intensity > 0"_.<br/>
Both cases (and many more) can be modeled by defining some _**Parameter Replacements**_:

## Define an _Indicator Parameter_
First, you need to "split your parameter" up into an _IndicatorParameter_ and some depending _ControlledParameter_. The IndicatorParameter indicates whether the controlled parameter is active (i.e. "`x > 0`") or if it is currently disabled (i.e. "`x = 0`"). The easiest case is to introduce an additional _bool-parameter_ in your tree with a depending _choice-parameter_ (that controls the values for `x > 0`).

_**Example:**_<br/>
In the following case we want to introduce a node for the `bcaminuse` parameter from the _LingelingRunner-example_. Lingeling will accept values in the range of `[0, int.MaxValue]`. `0` disables the "`bca min use`" parameter. Since the domain is quite large, we want to use a `DiscreteLogDomain`. By default, this is not possible, since `0` is not a valid member for log-domains.<br/>
Instead, we introduce an _IndicatorParameter_, called `bcaminuseActive` that can take the boolean values `true` and `false`. If the parameter is set to `true` (i.e. "_activate bcaminuse_"), the value for `bcaminuse` is chosen from a log-domain with range `[1, int.MaxValue]`. Otherwise, no value for `bcaminuse` can be selected.

```xml
<node xsi:type="or" id="bcaminuseActive">
    <domain xsi:type="categorical" booleans="true false"/>
    <choice>
        <boolean>true</boolean>
        <child xsi:type="value" id="bcaminuse">
            <domain xsi:type="discrete" log="true" start="1" end="247483647"/>
        </child>
    </choice>
</node>
```
**_Important:_** This is only the first step for handling this case! In most cases, your target algorithm cannot _"understand"_ (i.e handle) the _artifical_ IndicatorParameter. You need to make sure to remove it from the list of _active parameters_ that you pass as arguments to your target algorithm. The easiest way for this is shown in the following section.

## Define a _Parameter Replacement_
If you introduces some (artificial) _IndicatorParameter_ into you parameter tree, you probably need to make sure that it will be removed from the list of parameters that you pass as arguemtns to your target algorithm. **Additionally**, you may want to replace it by setting the _ControlledParameter_ to some specific value.

**_Example:_**<br/>
In the previous example we defined an _IndicatorParameter_ (`bcaminuseActive`) that en- or disables the use of some heuristic (`bcaminuse`). Since the target algorithm cannot handle a parameter that is called `--bcaminuseActive`, it needs to be removed from the list of parameters that is given to the target algorithm. If the _IndicatorParameter_ is set to `false`, we also need to make sure that `bcaminuse` is actually set to `0` (i.e. that will be disabled in a way that the target algorithm can understand).<br/>
This can be done by defining a _filter rule_ for your parameter tree:

```csharp
    public ParameterReplacementDefinition AddParameterReplacementDefinition<T>(
        string indicatorParameterName,
        object indicatorParameterValue,
        string controlledParameterName,
        T nativeOverrideValue,
        bool alwaysRemoveIndicatorParameter = false);
```
Simply add a rule after loading your parameter tree from XML (or building it up in your code).<br/>
**Make sure** to specify whether you want the _IndicatorParameter_ to always be removed from the active parameter set, or only if the defined _IndicatorValue_ is matched. In our example, the `bacminuseActive`-parameter cannot be handled by Lingeling, and thus needs to always be removed.
```csharp
parameterTree.AddParameterReplacementDefinition("bcaminuseActive", false, "bcaminuse", 0, true);
```
After adding this filter, the `bcaminuseActive`-parameter will always be removed from the set of parameters that is returned by `Genome.GetFilteredParameters(ParameterTree tree)`. Additionally, the value for the `bcaminuse`-parameter will be set to `0`, if the current value for `bcaminuseActive == false`. Otherwise, the entry for `bcaminuse` is not altered.

## Optional: Always ignore a certain parameter
If you do not want to use some of the parameters that are defined in your parameter tree, you can simply add a filter rule that will always remove them from the set of active parameters. This can be useful if you defined an indicator parameter that indicates whether the algorithm's default value of the controlled parameter or a value that is drawn from the defined domain should be used. It can also be useful if you want to use a parameter tree that is slightly outdated (and contains some parameters that are no longer supported by your target algorithm), or if it contains parameters that can/should not be tuned (e.g. some epsilon-tolerance, the log level, etc.).<br/>
```csharp
    parameterTree.AddIgnoredParameter("ignoredParameterName");
```
The `ignoredParameterName` needs to be a member of the current `parameterTree`. It will always be removed (after checking for matched `ParameterReplacementDefinition`s).

___
## Effects
Keep in mind that the ignored parameters and defined replacements will _always_ be applied when the set of active parameters is computed. This also includes the console output of the incumbent genome and the final configuration.

## Customization
If this framework does not suit your needs, you can always define/apply custom filters for the current set of parameters. A good place for doing this is the `ConfigureTargetAlgorithm(Dictionary<string, IAllele> parameters)` method of the `ITargetAlgorithmFactory`.