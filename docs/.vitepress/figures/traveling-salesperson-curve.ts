import data from "../figure-data/traveling-salesperson.json";

// Traveling salesperson, "Read the quality curve": how far each population is above the known optimum.
const mergeIndex = data.series.best.findIndex((_, i) =>
    data.series.best.slice(i).every((value, k) => value === data.series.median[i + k]),
);

export default {
    data: { x: data.x, series: data.series },
    series: [
        { key: "best", label: "Best" },
        { key: "median", label: "Median" },
        { key: "worst", label: "Worst" },
    ],
    label: "Best, median and worst tour length of the population in each generation, as percent above the known optimum, on a log scale.",
    xTitle: "generation",
    yTitle: "above the known optimum",
    xTicks: [1, 250, 500, 750, 1000],
    yTicks: [1, 3, 10, 30, 100, 300],
    yDomain: [1, 500] as [number, number],
    yScale: "log" as const,
    unit: " %",
    digits: 1,
    note: {
        x: data.x[mergeIndex],
        series: "best",
        dx: 70,
        dy: 16,
        lines: [`best = median from generation ${data.x[mergeIndex]}, 3.4 % above the optimum`],
    },
    tableEvery: 50,
};
