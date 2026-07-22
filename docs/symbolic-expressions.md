# Symbolic expressions

`ExpressionTree` is the aggregate root of an immutable symbolic-expression genotype. It owns the root node and is the public entry point for edits.

## Structure and identity

- `ExpressionNode` represents immutable expression structure and payload. Nodes can be shared between trees or occur more than once in the same tree.
- `ExpressionPoint` identifies one node occurrence and its path within a particular tree. Use a point when selecting a mutation, crossover, or replacement location.
- `ExpressionTree` applies edits and returns a new tree. Unaffected nodes remain structurally shared with the original tree.

Node reference identity therefore does not identify a unique location. Obtain points from `ExpressionTree.RootPoint` and traverse those points when an operation must target a specific occurrence.

## Editing

Use the tree or a tree-bound point for public edits:

```csharp
var point = tree.RootPoint.Child(0);
var edited = tree.Replace(point, replacementNode);

// Equivalent convenience form.
var editedFromPoint = point.ReplaceWith(replacementNode);
```

`ExpressionTree.Replace` accepts either a replacement node or a replacement tree. `ReplaceMany` applies non-overlapping edits in one operation and rebuilds every affected ancestor at most once. `WithVariable` and `WithConstant` provide domain-specific tree edits.

Child-rebuilding methods on nodes are implementation details. Keeping those methods internal ensures that public edits retain tree ownership checks, occurrence semantics, and efficient structural sharing.
