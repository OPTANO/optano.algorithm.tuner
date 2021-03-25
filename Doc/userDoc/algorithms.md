# Tuning Algorithms

_OPTANO Algorithm Tuner_ (*OAT*) provides several tuning algorithms to choose from.

## Selecting a Tuning Algorithm

While the best algorithm depends on your application, we recommend [GGA++](#gga-1), a state-of-the-art general-purpose tuning algorithm, for most use cases. It can handle continuous, integer and categorical parameters and is often able to find "good" parameterizations earlier than its predecessor [GGA](#gga).

However, GGA requires less time between two iterations, which might make it the better choice if your algorithm is very fast to execute.

If you are only tuning numerical parameters, the specialized [JADE](#jade) or [CMA-ES](#active-cma-es) algorithms might yield better results than GGA++. Here, you should choose CMA-ES if you suspect that your parameters have complex interdependencies.

Finally, *OAT* allows you to [mix](#hybridization) the different algorithms. We only recommend this option for expert users.

## GGA

GGA is a general-purpose tuning algorithm able to handle continuous, integer and categorical parameters as well as complex relationships between them. It employs a specialized genetic algorithm to search the parameter search space.

The algorithm was introduced in [A Gender-Based Genetic Algorithm for the Automatic Configuration of Algorithms](https://link.springer.com/chapter/10.1007/978-3-642-04244-7_14?no-access=true) by Carlos Ansotegui Gil, Meinolf Sellmann and Kevin Tierney which was published in Proceedings of the 15th intern. Conference on the Principles and Practice of Constraint Programming (CP-09), Springer LNCS 5732, pp. 142-157, 2009.

## GGA++

GGA++ enhances [GGA](#gga) by a [model-based crossover](../developerDoc/model_based_crossover.md) and is considered as a state-of-the-art algorithm by the algorithm tuning research community.

It was first presented in [Model-Based Genetic Algorithms for Algorithm Configuration](https://www.ijcai.org/Proceedings/15/Papers/109.pdf) by Carlos Ansótegui, Yuri Malitsky, Horst Samulowitz, Meinolf Sellmann and Kevin Tierney, which was published in the Proceedings of the Twenty-Fourth International Joint Conference on Artificial Intelligence [(IJCAI 2015)](http://www.ijcai.org/proceedings/2015).

## JADE

JADE is a variant of differential evolution, a successful continuous optimization method. *OAT* offers the algorithm for tuning continuous and integer parameters. Although JADE cannot deal with categorical parameters, it exploits properties of numerical search spaces, making it a good choice for such tuning problems.

When you use JADE to tune integer parameters, make sure to correctly set the `--minDomainSize` [parameter](parameters.md).

JADE was introduced as a continuous optimization method in [JADE: Adaptive Differential Evolution With Optional External Archive](https://ieeexplore.ieee.org/document/5208221/) by Jingqiao Zhang and Arthur C. Sanderson, which was published in IEEE Transactions on Evolutionary Computation, vol. 13, no. 5, pp. 945-958, Oct. 2009.

The technique for constraint handling used by *OAT* has been presented in [Experimental Comparison of Methods to Handle Boundary Constraints in Differential Evolution](https://link.springer.com/chapter/10.1007/978-3-642-15871-1_42) by Jarosłlaw Arabas, Adam Szczepankiewicz and Tomasz Wroniak, which was published in Parallel Problem Solving from Nature, PPSN XI. PPSN 2010. Lecture Notes in Computer Science, vol 6239. Springer, Berlin, Heidelberg.

## Active CMA-ES

Active CMA-ES is a famous variant of the popular Covariance Matrix Adaptation Evolution Strategy (CMA-ES), a continuous optimization method with especially good performance on non-separable functions. *OAT* offers the algorithm for tuning continuous, integer and categorical parameters, although, similar to [JADE](#jade), it works best on large numerical domains.

We recommend using active CMA-ES as your tuning if the majority of your parameters are either continuous or have large integer domains, and if you suspect complex interdependencies between them.

The active CMA-ES implementation of *OAT* follows [The CMA Evolution Strategy: A Tutorial.](https://hal.inria.fr/hal-01297037/file/tutorial.pdf) by Nikolaus Hansen, which is available at ArXiv e-prints, arXiv:1604.00772, 2016. 2005. In addition, it makes use of several practical hints provided on [Nikolaus Hansen's website](https://www.lri.fr/~hansen/cmaes_inmatlab.html#practical).

## Hybridization

*OAT* exposes [parameters](parameters.md#tuning-algorithm) to mix the described tuning algorithms.

In the case of [GGA](#gga) and [GGA++](#gga-1), such a mix (or *hybrid*) employs different crossover operations in a single generation; in the case of [JADE](#jade) or [CMA-ES](#active-cma-es), *OAT* employs alternating phases of GGA(++) and the continuous optimization method. This allows you to utilize JADE even if your algorithm exposes categorical parameters.

As there is not much knowledge available on how hybridization influences tuning results, it is only recommended for expert users.