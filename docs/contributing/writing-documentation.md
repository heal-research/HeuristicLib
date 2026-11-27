# Writing documentation

This site is built with VitePress from the `docs` folder. Run `npm run docs:dev` to see a change while you write.

## Choose the section

| Section      | Readers                                | Holds                                |
| ------------ | -------------------------------------- | ------------------------------------ |
| Guide        | Users who configure and run algorithms | Concepts and tasks, in reading order |
| Examples     | Users who want a complete program      | One task from start to result        |
| Extend       | Users who write their own components   | Authoring contracts and walkthroughs |
| Reference    | Anyone looking something up            | The glossary and catalogs            |
| Contributing | People who change the library          | Rules, architecture and workflow     |

A page belongs to one section. Link to a page in another section rather than repeating it. Features of the Experimental package get no page. Describe them in the XML remarks of their code.

## Write the text

- Describe how the library works today. Do not write that it "now" works some way, and leave design history to the issues.
- Use the terms of the [glossary](/reference/glossary).
- Keep sentences short. Use no semicolons. Split the sentence instead.
- Start a page with what the reader can do with it, in one or two sentences.
- End a Guide page with the next page of the guide.

## Show code that runs

Code on a page comes from `test/HeuristicLib.Tests.Docs`. Mark the shown part with a named region and import it:

```md
<<< ../../test/HeuristicLib.Tests.Docs/Extending/AlgorithmAuthoringDocumentationSpecs.cs#iterative-algorithm-type{csharp}
```

The suite compiles and runs that code, and the build fails when a path or region is missing. A fenced code block is not checked, so keep those to short excerpts.

- Keep test attributes and assertions outside the region.
- Give methods a block body, as [developer guidelines](/contributing/developer-guidelines) § 10.3 states.
- Put `Parameter` declarations in a second part of a `partial record`. Show them in a collapsed `::: details` block, so the first part shows what the component does.
- When a page quotes output, the spec asserts the same text.

## Add a figure

A chart reads its data from `docs/.vitepress/figure-data`. A documentation spec computes that data and calls `FigureData.Write`, which writes the file on every run. When a change to the library alters a documented run, the file changes in your working tree. Commit it with the change, together with any output or caption the page quotes from that run, so the pull request review sees it.

Each figure has a settings file in `docs/.vitepress/figures` that imports its data and says how to draw it. The page imports the settings file and passes it to a component, with the caption as content:

```md
<script setup>
import convergence from "../.vitepress/figures/getting-started-convergence";
</script>

<LineChart v-bind="convergence">Caption text.</LineChart>
```

Every chart offers its data as a table, so a reader who cannot see the chart can still read it.

The charts `LineChart`, `ScatterChart`, `TourPanels` and `ExperimentGrid` take a settings file. `ExpressionFit` reads its data file itself, and `CapacityBars` draws the numbers its page quotes. The other components are diagrams that take only a caption. All live in `docs/.vitepress/theme/figures` and draw with the `--hl-fig-*` color tokens of `figures.css`, which have light and dark values.

## Keep the catalogs complete

The [operator](/reference/operators) and [algorithm](/reference/algorithms) catalogs list every public type of the core package. `ReferenceCatalogDocumentationSpecs` fails when a type is missing or a row names a type that does not exist. Add a row when you add an operator or an algorithm.

## Check the change

```console
dotnet test --project test/HeuristicLib.Tests.Docs/HeuristicLib.Tests.Docs.csproj --configuration Release --no-restore
npm run docs:build
npx --no-install prettier --check docs
```
