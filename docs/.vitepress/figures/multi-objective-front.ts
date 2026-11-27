import data from "../figure-data/multi-objective.json";

// Multiobjective optimization, "Choose a design": the initial population, the final front and the chosen design.
const design = ([x, y, radius, height]: number[]) => ({ x, y, radius, height });
const choice = data.front[data.choice];
const thousands = (value: number) => (value >= 1000 ? `${value / 1000}K` : String(value));

export default {
    groups: [
        {
            label: "Initial population",
            color: "var(--hl-fig-context)",
            points: data.initial.map(design),
            radius: 3,
            ring: false,
        },
        { label: "Final front", color: "var(--hl-fig-series-1)", points: data.front.map(design), radius: 3.6 },
        { label: "Chosen design", color: "var(--hl-fig-accent)", points: [design(choice)], radius: 6 },
    ],
    label: "Material against volume for the initial population and the final Pareto front, with the cheapest design holding at least 2000.",
    xTitle: "material (minimize) →",
    yTitle: "volume (maximize)",
    xTicks: [0, 500, 1000, 1500, 2000, 2500, 3000],
    yTicks: [0, 2000, 4000, 6000, 8000, 10000, 12000],
    xDomain: [0, 3000] as [number, number],
    yDomain: [0, 12000] as [number, number],
    tickFormat: thousands,
    reference: { y: 2000, label: "requirement: volume ≥ 2,000", labelX: 1650 },
    note: {
        x: choice[0],
        y: choice[1],
        dx: -20,
        dy: -86,
        lines: ["cheapest design meeting it", `r ${choice[2].toFixed(2)}, h ${choice[3].toFixed(2)}`],
    },
    tip: (p: { x: number; y: number; radius: number; height: number }) => [
        { label: "material", value: p.x.toFixed(1) },
        { label: "volume", value: p.y.toFixed(1) },
        { label: "radius", value: p.radius.toFixed(2) },
        { label: "height", value: p.height.toFixed(2) },
    ],
    tableHeaders: ["material", "volume", "radius", "height", "h/r"],
    tableRows: data.front.map(([material, volume, radius, height]) => [
        material.toFixed(1),
        volume.toFixed(1),
        radius.toFixed(2),
        height.toFixed(2),
        (height / radius).toFixed(2),
    ]),
};
