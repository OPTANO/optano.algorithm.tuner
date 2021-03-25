# Model-Based Crossover Operator
With the [GGA++](../userDoc/algorithms.md#gga-1) tuning algorithm, _OPTANO Algorithm Tuner_ offers an alternative method for [GGA](../userDoc/algorithms.md#gga) to generate offspring. Instead of the _classical_ genetic crossover, a model-based crossover operator can be used. The operator trains a random forest model that predicts the performance of some potential offspring (i.e. a combination of a tournament winner and a non-competitive genome).<br>
For each pair of parents a set of potential offspring is generated (using a targeted sampling method) and the offspring with the best predicted rank (=performance) is selected. Specific details on the algorithm can be found in this [paper](https://www.ijcai.org/Proceedings/15/Papers/109.pdf).

## Usage
_OPTANO Algorithm Tuner_ provides several flavors with respect to the internal components that are used by the model-based crossover operator. The default implementation of the _OPTANO Algorithm Tuner_ uses the following set of components:
* Learner Model
    * `StandardRandomForestLearner<ReuseOldTreesStrategy>`
* Predictor Model
    * `GenomePredictionForestModel<GenomePredictionTree>`
* Sampling Strategy
    * `ReuseOldTreesStrategy`

These components, that can be specified via generic type parameters for the `AlgorithmTuner`, mainly reflect the behavior that is described in the referenced paper.
An exception to this is the learner model: The original learner model is implemented in `GenomePredictionRandomForest<ReuseOldTreesStrategy` and can still be specified. However, internal experiments have shown that a standard random forest often leads to better results in practice.

Another setting which has proven beneficial in some cases is the following configuration:
* Additional [parameters](../userDoc/parameters.md#model-based-parameters)
    * Make sure to use the `HammingDistance` with `--maxRanksCompensatedByDistance` approx. 20% of your `--miniTournamentSize`.

In order to _enable_ the model-based crossover, you need to set the `--engineeredProportion` to a value larger than _0_.