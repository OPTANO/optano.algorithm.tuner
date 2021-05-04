# Getting started with _OPTANO Algorithm Tuner_

*OPTANO Algorithm Tuner* (*OAT*) enables you to tune (find near-optimal parameters for) arbitrary algorithms.

## Preparation

Note that you will have the best experience with *OAT*, if you heed our [detailed preparation tips](preparation.md).

### [Basic Usage](basic_usage.md)
Tune your algorithm either optimizing (penalized) run time or its output value. Can be used if the tuneable algorithm accepts instance files as input and its parameters are contained in defined ranges.

*Side-Note:* Even though the *OAT* Application is provided as a stand-alone executable, technically it also is a *custom* adapter, similar to the implementations of our [examples](../developerDoc/examples.md) in the [advanced usage](../developerDoc/advanced.md) section.

### [Advanced Usage](../developerDoc/advanced.md)
You want to optimize using a more complex or even multicriterial function? There are some forbidden parameter combinations? Your algorithm does not accept instance files as input? You can still use *OAT* in these cases, but you will need to specify your algorithm's special characteristics by code.

## Scaling to your computing power

Depending on the amount of computational resources available to you, you may execute *OAT* 

- either on a single computation node or in a  [distributed fashion](distributed.md),
- in one single run or in  [multiple sessions](statusdump.md), and of course
- [configure](parameters.md) settings like the number of parallel algorithm executions per node
