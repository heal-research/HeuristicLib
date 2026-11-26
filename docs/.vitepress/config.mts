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
            { text: "Guide", link: "/guide/" },
            { text: "Examples", link: "/examples/" },
            { text: "Contributing", link: "/contributing/" },
            { text: "NuGet", link: "https://www.nuget.org/packages/HEAL.HeuristicLib" },
        ],
        sidebar: {
            "/guide/": [
                {
                    text: "Start",
                    items: [
                        { text: "Overview", link: "/guide/" },
                        { text: "Getting started", link: "/guide/getting-started" },
                    ],
                },
                {
                    text: "Fundamentals",
                    items: [
                        { text: "Core concepts", link: "/guide/fundamentals/core-concepts" },
                        { text: "Problems", link: "/guide/fundamentals/problems" },
                        { text: "Search spaces", link: "/guide/fundamentals/search-spaces" },
                        { text: "Objectives and candidates", link: "/guide/fundamentals/objectives" },
                        { text: "Operators", link: "/guide/fundamentals/operators" },
                        { text: "Algorithms", link: "/guide/fundamentals/algorithms" },
                        { text: "Configurations", link: "/guide/fundamentals/configurations" },
                        { text: "Validation and invariants", link: "/guide/fundamentals/invariants-and-validation" },
                    ],
                },
                {
                    text: "Execution",
                    items: [
                        { text: "Algorithm runs", link: "/guide/execution/running-algorithms" },
                        { text: "Analysis", link: "/guide/execution/observability-and-analysis" },
                        { text: "Search states", link: "/guide/execution/search-states" },
                        { text: "Randomness", link: "/guide/execution/randomness" },
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
                {
                    text: "Extending HeuristicLib",
                    collapsed: true,
                    items: [
                        { text: "Write an operator", link: "/guide/extending/writing-operators" },
                        { text: "Compose operators", link: "/guide/extending/operator-composition" },
                        { text: "Write wrappers", link: "/guide/extending/writing-wrappers" },
                        { text: "Write an algorithm", link: "/guide/extending/writing-algorithms" },
                        { text: "Write a meta-algorithm", link: "/guide/extending/writing-meta-algorithms" },
                    ],
                },
                {
                    text: "Reference",
                    items: [{ text: "Glossary", link: "/guide/glossary" }],
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
                        { text: "Numeric optimization", link: "/examples/numeric-optimization" },
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
            "/contributing/": [
                {
                    text: "Contributing",
                    items: [
                        { text: "Overview", link: "/contributing/" },
                        { text: "Design goals", link: "/contributing/design-goals" },
                        { text: "Requirements", link: "/contributing/requirements" },
                        { text: "Developer guidelines", link: "/contributing/developer-guidelines" },
                    ],
                },
                {
                    text: "Architecture",
                    items: [
                        { text: "Layering", link: "/contributing/architecture/layering" },
                        {
                            text: "Execution nodes",
                            link: "/contributing/architecture/execution-nodes",
                        },
                        {
                            text: "Execution resolution",
                            link: "/contributing/architecture/execution-resolution",
                        },
                        {
                            text: "Operator implementation",
                            link: "/contributing/architecture/operator-implementation",
                        },
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
