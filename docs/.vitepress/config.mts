import { defineConfig } from "vitepress";

export default defineConfig({
    title: "HeuristicLib",
    description: "Heuristic and evolutionary algorithms for modern .NET",
    base: "/",
    sitemap: {
        hostname: "https://heuristiclib.github.io/",
    },
    markdown: {
        theme: {
            light: "light-plus",
            dark: "dark-plus",
        },
        lineNumbers: true,
        tableTabIndex: false,
        config(md) {
            md.renderer.rules.table_open = (tokens, index, options, _env, renderer) =>
                '<div class="doc-table-scroll" tabindex="0">' + renderer.renderToken(tokens, index, options);
            md.renderer.rules.table_close = (tokens, index, options, _env, renderer) =>
                renderer.renderToken(tokens, index, options) + "</div>\n";
        },
    },
    themeConfig: {
        nav: [
            { text: "Guide", link: "/guide/", activeMatch: "^/guide/" },
            { text: "Examples", link: "/examples/", activeMatch: "^/examples/" },
            { text: "Extend", link: "/extend/", activeMatch: "^/extend/" },
            { text: "Reference", link: "/reference/", activeMatch: "^/reference/" },
            { text: "Contributing", link: "/contributing/", activeMatch: "^/contributing/" },
            { text: "NuGet", link: "https://www.nuget.org/packages/HEAL.HeuristicLib" },
        ],
        sidebar: {
            "/guide/": [
                {
                    text: "Start",
                    items: [
                        { text: "Introduction", link: "/guide/" },
                        { text: "Getting started", link: "/guide/getting-started" },
                    ],
                },
                {
                    text: "Model a problem",
                    items: [
                        { text: "Problems", link: "/guide/fundamentals/problems" },
                        { text: "Search spaces", link: "/guide/fundamentals/search-spaces" },
                        { text: "Objectives and candidates", link: "/guide/fundamentals/objectives" },
                    ],
                },
                {
                    text: "Configure a search",
                    items: [
                        { text: "Operators", link: "/guide/fundamentals/operators" },
                        { text: "Algorithms", link: "/guide/fundamentals/algorithms" },
                        { text: "Configurations", link: "/guide/fundamentals/configurations" },
                        { text: "Operator composition", link: "/guide/fundamentals/operator-composition" },
                        { text: "Validation and invariants", link: "/guide/fundamentals/invariants-and-validation" },
                    ],
                },
                {
                    text: "Run and analyze",
                    items: [
                        { text: "Algorithm runs", link: "/guide/execution/running-algorithms" },
                        { text: "Search states", link: "/guide/execution/search-states" },
                        { text: "Randomness", link: "/guide/execution/randomness" },
                        { text: "Analysis", link: "/guide/execution/observability-and-analysis" },
                        { text: "Experiments", link: "/guide/execution/experiments" },
                    ],
                },
                {
                    text: "Domains",
                    items: [
                        { text: "Data and machine learning", link: "/guide/domains/data-and-machine-learning" },
                        { text: "Symbolic expressions", link: "/guide/domains/symbolic-expressions" },
                        { text: "Symbolic regression", link: "/guide/domains/symbolic-regression" },
                    ],
                },
                {
                    text: "Interop",
                    items: [{ text: "Python", link: "/guide/interop/python" }],
                },
            ],
            "/examples/": [
                {
                    text: "Start",
                    items: [{ text: "Overview", link: "/examples/" }],
                },
                {
                    text: "Solve a problem",
                    items: [
                        { text: "Compare algorithms", link: "/examples/compare-algorithms" },
                        { text: "Traveling salesperson", link: "/examples/traveling-salesperson" },
                        { text: "Symbolic regression", link: "/examples/symbolic-regression" },
                        { text: "Custom problems", link: "/examples/custom-problem" },
                        { text: "Multiobjective optimization", link: "/examples/multi-objective" },
                    ],
                },
                {
                    text: "Connect from Python",
                    items: [
                        {
                            text: "Symbolic regression from Python",
                            link: "/examples/python-symbolic-regression",
                        },
                    ],
                },
            ],
            "/extend/": [
                {
                    text: "Start",
                    items: [{ text: "Overview", link: "/extend/" }],
                },
                {
                    text: "Foundations",
                    items: [{ text: "How execution works", link: "/extend/how-execution-works" }],
                },
                {
                    text: "Operators",
                    items: [
                        { text: "Write an operator", link: "/extend/writing-operators" },
                        { text: "Write a composition", link: "/extend/writing-compositions" },
                        { text: "Define an operation", link: "/extend/defining-operations" },
                        { text: "Declare invariants", link: "/extend/declaring-invariants" },
                    ],
                },
                {
                    text: "Algorithms",
                    items: [
                        { text: "Write an algorithm", link: "/extend/writing-algorithms" },
                        { text: "Write a meta-algorithm", link: "/extend/writing-meta-algorithms" },
                    ],
                },
                {
                    text: "Wrappers and analysis",
                    items: [
                        { text: "Write wrappers", link: "/extend/writing-wrappers" },
                        { text: "Observations and clocks", link: "/extend/observations-and-clocks" },
                    ],
                },
            ],
            "/reference/": [
                {
                    text: "Start",
                    items: [{ text: "Overview", link: "/reference/" }],
                },
                {
                    text: "Terms",
                    items: [{ text: "Glossary", link: "/reference/glossary" }],
                },
                {
                    text: "Catalogs",
                    items: [
                        { text: "Algorithms and defaults", link: "/reference/algorithms" },
                        { text: "Operators by encoding", link: "/reference/operators" },
                        { text: "Analyzers and traces", link: "/reference/analyzers" },
                    ],
                },
            ],
            "/contributing/": [
                {
                    text: "Start",
                    items: [
                        { text: "Start contributing", link: "/contributing/" },
                        { text: "Writing documentation", link: "/contributing/writing-documentation" },
                    ],
                },
                {
                    text: "Principles",
                    items: [
                        { text: "Design goals", link: "/contributing/design-goals" },
                        { text: "Requirements", link: "/contributing/requirements" },
                        { text: "Developer guidelines", link: "/contributing/developer-guidelines" },
                    ],
                },
                {
                    text: "Architecture",
                    items: [
                        { text: "Architecture overview", link: "/contributing/architecture/" },
                        { text: "Layering", link: "/contributing/architecture/layering" },
                        { text: "Execution nodes", link: "/contributing/architecture/execution-nodes" },
                        { text: "Execution resolution", link: "/contributing/architecture/execution-resolution" },
                        { text: "Operator implementation", link: "/contributing/architecture/operator-implementation" },
                        { text: "Analyzers", link: "/contributing/architecture/analyzers" },
                    ],
                },
            ],
        },
        search: {
            provider: "local",
        },
        outline: [2, 3],
        socialLinks: [{ icon: "github", link: "https://github.com/heal-research/HeuristicLib" }],
        footer: {
            message: "Released under the MIT License.",
            copyright: "Heuristic and Evolutionary Algorithms Laboratory",
        },
    },
});
