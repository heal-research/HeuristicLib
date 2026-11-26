import data from "../figure-data/multi-objective.json";

// Multiobjective optimization, "Run and read the front": height over radius along the front.
const thousands = (value: number) => (value >= 1000 ? `${value / 1000}K` : String(value));

export default {
    groups: [
        {
            label: "Front member",
            color: "var(--hl-fig-series-1)",
            points: data.front.map(([material, , radius, height]) => ({ x: material, y: height / radius })),
            radius: 3.6,
        },
    ],
    label: "Height over radius along the front, close to two through the middle.",
    xTitle: "material",
    yTitle: "h / r",
    xTicks: [0, 500, 1000, 1500, 2000, 2500, 3000],
    yTicks: [1.2, 1.6, 2.0, 2.4],
    xDomain: [0, 3000] as [number, number],
    yDomain: [1.2, 2.4] as [number, number],
    tickFormat: thousands,
    yTickFormat: (value: number) => value.toFixed(1),
    reference: { y: 2, label: "h = 2r", labelX: 60 },
    tip: (p: { x: number; y: number }) => [
        { label: "h / r", value: p.y.toFixed(3) },
        { label: "material", value: p.x.toFixed(1) },
    ],
    tableHeaders: ["material", "h/r"],
    tableRows: data.front.map(([material, , radius, height]) => [material.toFixed(1), (height / radius).toFixed(3)]),
    height: 190,
};
