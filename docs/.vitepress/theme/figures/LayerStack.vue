<script setup lang="ts">
import { useId } from "vue";
import ArrowMarkers from "./ArrowMarkers.vue";
import HlFigure from "./HlFigure.vue";

// The six layers from analysis at the top to domain vocabulary at the bottom, with two example types each.
const id = useId();

const layers = [
    {
        id: "L5",
        name: "Analysis",
        does: "results from observations",
        types: ["Analyzer<TResult>", "AggregateAnalyzer"],
    },
    { id: "L4", name: "Hosting", does: "start and coordinate runs", types: ["AlgorithmRun", "ExperimentRun"] },
    {
        id: "L3",
        name: "Library content",
        does: "algorithms, operators, encodings",
        types: ["GeneticAlgorithm", "CounterClock"],
    },
    { id: "L2", name: "Resolution", does: "run results, executor graphs", types: ["ResolutionScope", "Substitution"] },
    {
        id: "L1",
        name: "Composition model",
        does: "contracts and authoring bases",
        types: ["IConfigurationNode", "IExecutor"],
    },
    {
        id: "L0",
        name: "Domain vocabulary",
        does: "values independent of execution",
        types: ["SearchSpace<T>", "ObjectiveVector"],
    },
];

const bandHeight = 40;
const gap = 8;
const width = 612;

const bands = layers.map((layer, index) => {
    const y = 0.5 + index * (bandHeight + gap);
    let x = width - 10;
    const chips = [...layer.types].reverse().map((type) => {
        const chipWidth = type.length * 6.6 + 14;
        x -= chipWidth;
        const chip = { type, x, width: chipWidth };
        x -= 6;
        return chip;
    });
    return { ...layer, y, chips, opacity: (0.35 + (0.65 * (5 - index)) / 5).toFixed(2) };
});
</script>

<template>
    <HlFigure>
        <svg
            class="hl-dg"
            viewBox="-12 -12 680 312"
            role="img"
            aria-label="Six layers. From the top: L5 analysis, L4 hosting, L3 library content, L2 resolution, L1 composition model and L0 domain vocabulary. Each layer may use the layers below it."
        >
            <ArrowMarkers :id="id" />
            <g v-for="band in bands" :key="band.id">
                <rect class="band" x="0.5" :y="band.y" :width="width" :height="bandHeight" rx="9" />
                <rect class="bar" x="0.5" :y="band.y" width="5" :height="bandHeight" rx="2.5" :opacity="band.opacity" />
                <text class="mm" x="18" :y="band.y + 25">{{ band.id }}</text>
                <text class="t" x="46" :y="band.y + 25">{{ band.name }}</text>
                <text class="s" x="184" :y="band.y + 25">{{ band.does }}</text>
                <g v-for="chip in band.chips" :key="chip.type">
                    <rect class="chip" :x="chip.x" :y="band.y + 10" :width="chip.width" height="20" rx="5" />
                    <text class="mm" :x="chip.x + 7" :y="band.y + 24">{{ chip.type }}</text>
                </g>
            </g>
            <path class="edge" d="M640 18 L 640 268" :marker-end="`url(#${id}-arrow)`" />
            <text class="el" x="628" y="143" text-anchor="middle" transform="rotate(-90 628 143)">
                may use the layers below
            </text>
        </svg>
        <template #caption><slot /></template>
    </HlFigure>
</template>
