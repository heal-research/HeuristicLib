<script setup lang="ts">
import { useId } from "vue";
import ArrowMarkers from "./ArrowMarkers.vue";
import HlFigure from "./HlFigure.vue";

// One generation of a genetic algorithm, with the setting that controls each step and the two mutation rates.
const id = useId();
const arrow = `url(#${id}-arrow)`;

const width = 104;
const pitch = 118;
const steps = [
    { title: "Select", detail: "2N parents", chip: "Selector" },
    { title: "Cross", detail: "N children", chip: "Crossover" },
    { title: "Mutate", detail: "some of them", chip: "Mutator" },
    { title: "Evaluate", detail: "score them", chip: "Evaluator" },
    { title: "Replace", detail: "keep elites", chip: "Elites" },
].map((step, index) => ({ ...step, x: 0.5 + (index + 1) * pitch }));
const mutate = steps[2].x;
const replaceCenter = steps[4].x + width / 2;
const chipWidth = (text: string) => text.length * 6.6 + 14;
</script>

<template>
    <HlFigure>
        <svg
            class="hl-dg"
            viewBox="-14 -14 724 282"
            role="img"
            aria-label="Create the first population. Then each generation selects 2N parents, crosses them into N children, mutates some of them, evaluates them and keeps the elites. Each generation is one search state."
        >
            <ArrowMarkers :id="id" />
            <g class="shadow"><rect class="node" x="0.5" y="0.5" :width="width" height="52" rx="10" /></g>
            <text class="t" x="14" y="23">Create</text>
            <text class="s" x="14" y="40">first population</text>
            <path :d="`M${width / 2} 53 L ${width / 2} 86`" class="edge" :marker-end="arrow" />

            <circle class="badge" cx="150" cy="16" r="9" />
            <text class="badge-t" x="150" y="20" text-anchor="middle">1</text>
            <text class="s" x="166" y="20">
                <tspan class="m">MutationRate</tspan>
                of the algorithm sets the share of children handed to the mutator
            </text>
            <circle class="badge" cx="150" cy="42" r="9" />
            <text class="badge-t" x="150" y="46" text-anchor="middle">2</text>
            <text class="s" x="166" y="46">
                <tspan class="m">mutationRate</tspan>
                of the mutator sets the share of positions it changes in one child
            </text>

            <rect class="node primary" x="0.5" y="88.5" :width="width" height="88" rx="10" />
            <text class="t" x="14" y="114">Population</text>
            <text class="s" x="14" y="132">N evaluated</text>
            <text class="s" x="14" y="147">candidates</text>

            <g class="shadow">
                <rect
                    v-for="step in steps"
                    :key="step.title"
                    class="node"
                    :x="step.x"
                    y="88.5"
                    :width="width"
                    height="88"
                    rx="10"
                />
            </g>
            <g v-for="step in steps" :key="`${step.title}-text`">
                <text class="t" :x="step.x + 14" y="114">{{ step.title }}</text>
                <text class="s" :x="step.x + 14" y="132">{{ step.detail }}</text>
                <rect class="chip" :x="step.x + 14" y="145" :width="chipWidth(step.chip)" height="19" rx="5" />
                <text class="mm" :x="step.x + 21" y="158">{{ step.chip }}</text>
                <path class="edge" :d="`M${step.x - 13} 132.5 L ${step.x - 2} 132.5`" :marker-end="arrow" />
            </g>

            <circle class="badge" :cx="mutate + 88" cy="109" r="8" />
            <text class="badge-t" :x="mutate + 88" y="113" text-anchor="middle">1</text>
            <circle class="badge" :cx="mutate + 88" cy="154.5" r="8" />
            <text class="badge-t" :x="mutate + 88" y="158.5" text-anchor="middle">2</text>

            <path
                class="edge"
                :d="`M${replaceCenter} 177 C ${replaceCenter} 226, ${replaceCenter - 16} 236, ${replaceCenter - 46} 236 L ${width / 2 + 46} 236 C ${width / 2 + 16} 236, ${width / 2} 226, ${width / 2} 180`"
                :marker-end="arrow"
            />
            <text class="el" :x="(replaceCenter + width / 2) / 2" y="240" text-anchor="middle">
                next generation: one yielded search state
            </text>
        </svg>
        <template #caption><slot /></template>
    </HlFigure>
</template>
