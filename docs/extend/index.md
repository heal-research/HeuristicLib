# Extend HeuristicLib

These pages show how to write your own operators, operations, algorithms, wrappers and analyzers. Most applications need none of them. Configure and [compose](/guide/fundamentals/operator-composition) built-in components first, and put domain evaluation in a [problem](/guide/fundamentals/problems).

Your components work like built-in ones. They are validated, observed and run the same way.

| You need                                                       | Read                                                       |
| -------------------------------------------------------------- | ---------------------------------------------------------- |
| The model every component builds on                            | [How execution works](/extend/how-execution-works)         |
| A search step the built-in operators do not provide            | [Write an operator](/extend/writing-operators)             |
| An operator that coordinates child operators                   | [Write a composition](/extend/writing-compositions)        |
| A step that no existing role describes                         | [Define an operation](/extend/defining-operations)         |
| Validation that knows what your search space or operator keeps | [Declare invariants](/extend/declaring-invariants)         |
| A new state transition or control flow                         | [Write an algorithm](/extend/writing-algorithms)           |
| An algorithm that schedules other algorithms                   | [Write a meta-algorithm](/extend/writing-meta-algorithms)  |
| A runtime policy around selected executors                     | [Write wrappers](/extend/writing-wrappers)                 |
| An analyzer over several observation types or a new clock      | [Observations and clocks](/extend/observations-and-clocks) |

The [contributor architecture](/contributing/architecture/layering) states the rules the library's own components follow.
