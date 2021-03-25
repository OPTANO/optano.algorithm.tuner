
# _OPTANO Algorithm Tuner_ Documentation
_OPTANO Algorithm Tuner_ (*OAT*) is a .Net-API that helps tuning (finding near-optimal parameter) for any given target algorithm. It is easy to use and full featured. For detailed information, please see [What is *OAT*?](userDoc/whatisalgorithmtuner.md)


*OAT* is part of the [OPTANO Platform](https://optano.com/en/platform), the perfect base for planning and optimization software. 

# Easy Start

> [!NOTE] 
> [Download the Generic OPTANO Algorithm Tuner Application](download.md) and [follow these instructions](userDoc/basic_usage.md)!

# Source code

* The souce code of *OAT* can be found at https://github.com/OPTANO/optano.algorithm.tuner.
* The source code of the [Generic OAT Application](download.md) and the [exemplary target algorithm adapters](developerDoc/examples.md) can be found at https://github.com/OPTANO/optano.algorithm.tuner.examples.


# Help and Information

## [The User Documentation](userDoc/intro.md)
The [User Documentation](userDoc/intro.md) gives help for getting started with *OAT*. Among others it provides
* [all information to get started](userDoc/intro.md)
* [detailed preparation tips](userDoc/preparation.md)
* [full list of technical preparations](developerDoc/technical_preparation.md)
* [tips for basic usage of the Generic OAT Application](userDoc/basic_usage.md)
* [tips for advanced usage of OAT](developerDoc/advanced.md)
* [detailed information about exemplary target algorithm adapters](developerDoc/examples.md)

## [The API Documentation](api/index.md)
Every detail about the exposed [API](api/index.md). 

## [Change-Log](changelog.md)
Find information about changes and new features in the [Change-Log](changelog.md).  

## Support
* For all inquiries, questions and bug reports refer to the [issue section of the OAT repository](https://github.com/OPTANO/optano.algorithm.tuner/issues). We'd like to help!
* For all legal requests (e.g. questions regarding copyright, non-commercial license or commercial license), please contact [OPTANO](https://optano.com/en/about-us/#contact).

# Credits
*OAT* is maintained by [OPTANO GmbH, Paderborn, Germany](https://optano.com)

It was created by

- Lars Beckmann, Paderborn University
- Britta Heymann, OPTANO GmbH, Paderborn
- Robin Kemminer, OPTANO GmbH, Paderborn
- Jens Peter Kempkes, OPTANO GmbH, Paderborn
- Jannick Lange, OPTANO GmbH, Paderborn
- Kevin Tierney, Bielefeld University

The theoretical foundations of *OAT* are based on several papers

- The representation of problem instances and the GGA tuning algorithm were introduced in [A Gender-Based Genetic Algorithm for the Automatic Configuration of Algorithms](https://link.springer.com/chapter/10.1007/978-3-642-04244-7_14?no-access=true) by Carlos Ansotegui Gil, Meinolf Sellmann and Kevin Tierney which was published in Proceedings of the 15th intern. Conference on the Principles and Practice of Constraint Programming (CP-09), Springer LNCS 5732, pp. 142-157, 2009.
- The [model-based crossover](developerDoc/model_based_crossover.md) operator is based on the paper [Model-Based Genetic Algorithms for Algorithm Configuration](https://www.ijcai.org/Proceedings/15/Papers/109.pdf) by Carlos Ansótegui, Yuri Malitsky, Horst Samulowitz, Meinolf Sellmann and Kevin Tierney which was published in the Proceedings of the Twenty-Fourth International Joint Conference on Artificial Intelligence [(IJCAI 2015)](http://www.ijcai.org/proceedings/2015).
- The [alternative tuning algorithm JADE](userDoc/algorithms.md#jade) was introduced as a continuous optimization method in [JADE: Adaptive Differential Evolution With Optional External Archive](https://ieeexplore.ieee.org/document/5208221/) by Jingqiao Zhang and Arthur C. Sanderson, which was published in IEEE Transactions on Evolutionary Computation, vol. 13, no. 5, pp. 945-958, Oct. 2009.
- Its technique for constraint handling has been presented in [Experimental Comparison of Methods to Handle Boundary Constraints in Differential Evolution](https://link.springer.com/chapter/10.1007/978-3-642-15871-1_42) by Jarosłlaw Arabas, Adam Szczepankiewicz and Tomasz Wroniak, which was published in Parallel Problem Solving from Nature, PPSN XI. PPSN 2010. Lecture Notes in Computer Science, vol 6239. Springer, Berlin, Heidelberg.
- The implemenation of the [alternative tuning algorithm active CMA-ES](userDoc/algorithms.md#active-cma-es) follows [The CMA Evolution Strategy: A Tutorial.](https://hal.inria.fr/hal-01297037/file/tutorial.pdf) by Nikolaus Hansen, which is available at ArXiv e-prints, arXiv:1604.00772, 2016. 2005.<br/>It also makes use of several practical hints provided on [Nikolaus Hansen's website](https://www.lri.fr/~hansen/cmaes_inmatlab.html#practical).

Basic ideas for distributed execution have been adopted from the project  *Distributed Gender-Based Genetic Algorithm for the Automatic Configuration of Algorithms* by Josep Pon Farreny.

Many people and companies have supported this project with

- Ideas and code
- Fine algorithms to tune
- and intense testing of new versions. 

Thanks for all your help!

This project is sponsored

![Bad Image Exception](images/sponsor.png)

## Licenses
This version of *OAT* is [distributed under the MIT license](https://optano.com/en/algorithm-tuner-mit-license/).

This project links to the following nuget packages:

* [Akka.Cluster](https://www.nuget.org/packages/Akka.Cluster/)
* [Akka.Logger.NLog](https://www.nuget.org/packages/Akka.Logger.NLog/)
* [Akka.Serialization.Hyperion](https://www.nuget.org/packages/Akka.Serialization.Hyperion/)
* [MathNet.Numerics](https://www.nuget.org/packages/MathNet.Numerics/)
* [NDesk.Options.Patched](https://www.nuget.org/packages/NDesk.Options.Patched/)
* [OPTANO.Modeling.Gurobi](https://www.nuget.org/packages/OPTANO.Modeling.Gurobi/)
* [OptimizedPriorityQueue](https://www.nuget.org/packages/OptimizedPriorityQueue/)
* [StyleCop.Analyzers](https://www.nuget.org/packages/StyleCop.Analyzers/)

Moreover its test project links to the following nuget packages:

* [Accord.Statistics](https://www.nuget.org/packages/Accord.Statistics/)
* [Akka.TestKit.Xunit2](https://www.nuget.org/packages/Akka.TestKit.Xunit2/)
* [coverlet.collector](https://www.nuget.org/packages/coverlet.collector/)
* [Microsoft.NET.Test.Sdk](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/)
* [Moq](https://www.nuget.org/packages/Moq/)
* [Shouldly](https://www.nuget.org/packages/Shouldly/)
* [xunit](https://www.nuget.org/packages/xunit/)
* [xunit.runner.visualstudio](https://www.nuget.org/packages/xunit.runner.visualstudio/)
* [Xunit.SkippableFact](https://www.nuget.org/packages/Xunit.SkippableFact/)

Please refer to the named nuget pages for further license information and indirectly used nuget packages.