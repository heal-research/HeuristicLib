import data from "../figure-data/getting-started-convergence.json";

// Getting started, "Search progress": every generation of the streamed run.
export default {
    data,
    series: [
        { key: "best", label: "Best" },
        { key: "median", label: "Median" },
        { key: "worst", label: "Worst" },
    ],
    label: "Best, median and worst objective value of the population in each generation, on a log scale.",
    xTitle: "generation",
    yTitle: "objective value",
    xTicks: [1, 100, 200, 300, 400, 500],
    yTicks: [1e-8, 1e-6, 1e-4, 1e-2, 1, 100],
    yDomain: [1e-8, 1000] as [number, number],
    yScale: "log" as const,
    note: { x: 10, series: "best", dx: 120, dy: -2, lines: ["global basin reached", "by generation 10"] },
    tableEvery: 25,
};
