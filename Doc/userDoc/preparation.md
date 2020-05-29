# Preparation

This page is a collection of questions that will start you off when considering tuning an algorithm's parameters. While it's possible to use *OPTANO Algorithm Tuner* without considering these in advance, doing so will make for a much smoother experience.

## Know your target algorithm

- Which parameters do you want to tune?
    - What are their dependencies?
    - What are their domains?
    - Are there any forbidden parameter combinations?
- What do you want to optimize in tuning (e.g. runtime, result, a multiobjective function, ...)?
- How can you extract these results from an algorithm run?
- How long does a single run take?
- How much memory may a single run need?
- How many cores does a run utilize?
- Is it possible to start multiple instances of your algorithm in parallel?
- On which platforms can your algorithm be run (Windows, Linux, ...)?
- Is your algorithm dependent on other programs that may need to be installed on your computing node(s)?

## Know your input

- What kind of input does your algorithm take?
- Does the input you expect in the application you tune for have some common structure?
- Do you have a sufficient number of suitable instances to tune your algorithm and optimally test the parameters on an independent instance set?
- Where do you read your input from (file, database, ...)? Will it be possible to access it from your computing node(s)?

## Know your resources

- How many computing nodes do you have?
- Is it possible to connect them via TCP?
- How many cores do they have?
- How much memory do they provide?
- Do all of them provide the same resources (speed, memory, ...)? If not: Does it matter for your optimization if evaluations may happen on different machines?
- How much time do you want to invest?

## Know your options

- Is [basic usage](basic_usage.md) of *OPTANO Algorithm Tuner* sufficient or do you need to [customize](advanced.md)?
- Are there any [existing implementations](examples.md) you can use or learn from?
- Which [tuning algorithm](algorithms.md) do you want to apply?
- Which [parameters](parameters.md) should you set? Take a look at [some of our hints](parameter_selection.md) for an idea on how they relate to you algorithm, input and resources.