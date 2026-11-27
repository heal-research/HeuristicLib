import data from "../figure-data/traveling-salesperson.json";

// Traveling salesperson, "Read the quality curve": the best tour at four generations of the example run.
export default {
    data: data as unknown as {
        optimum: number;
        cities: [number, number][];
        tours: { generation: number; length: number; tour: number[] }[];
    },
};
