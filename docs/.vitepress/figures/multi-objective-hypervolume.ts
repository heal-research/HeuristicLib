import data from "../figure-data/multi-objective.json";

// Multiobjective, "Track progress toward the front": the hypervolume of the designs found so far, by generation.
export default {
    data: { x: data.generations, series: { hyperVolume: data.hyperVolume } },
    series: [{ key: "hyperVolume", label: "Hypervolume" }],
    label: "Hypervolume of the nondominated designs found so far, in millions, for each generation of seed 5. It rises fast in the first generations and then flattens.",
    xTitle: "generation",
    yTitle: "hypervolume, millions",
    xTicks: [1, 50, 100, 150, 200],
    yTicks: [14.1, 14.3, 14.5, 14.7],
    yDomain: [14.1, 14.7] as [number, number],
    digits: 3,
    tableEvery: 10,
};
