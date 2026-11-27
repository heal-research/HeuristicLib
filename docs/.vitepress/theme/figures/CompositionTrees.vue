<script setup lang="ts">
import HlFigure from "./HlFigure.vue";

// Four compositions as small trees: the composed configuration on top and its children below it.
const pitch = 174;
const panels = [
    {
        root: "ChoosePerItem",
        children: [
            ["uniformCreator", "0.75"],
            ["normalCreator", "0.25"],
        ],
        detail: ["a weighted choice", "for each item"],
    },
    {
        root: "AppliedAtRate(0.2)",
        children: [
            ["mutator", "0.2"],
            ["NoChangeMutator", "0.8"],
        ],
        detail: ["the mutator or no", "change, per candidate"],
    },
    {
        root: "PipelineRefiner",
        children: [
            ["repair", "1"],
            ["simplification", "2"],
            ["parameterFitting", "3"],
        ],
        detail: ["each stage refines", "the previous result"],
    },
    {
        root: "TransformedCrossover",
        children: [
            ["baseCrossover", "1"],
            ["repairMutator", "2"],
        ],
        detail: ["a mutator always", "follows the crossover"],
    },
].map((panel, index) => ({ ...panel, x: index * pitch + 0.5 }));

const rootWidth = 162;
const childWidth = 144;
const childY = (index: number) => 46.5 + index * 38;
</script>

<template>
    <HlFigure>
        <svg
            class="hl-dg"
            viewBox="-14 -14 720 226"
            role="img"
            aria-label="Four compositions as trees. ChoosePerItem holds a uniform creator weighted 0.75 and a normal creator weighted 0.25. AppliedAtRate 0.2 holds the mutator and a no change mutator. PipelineRefiner holds repair, simplification and parameter fitting in order. TransformedCrossover holds a base crossover followed by a repair mutator."
        >
            <g v-for="panel in panels" :key="panel.root">
                <rect class="node primary" :x="panel.x" y="0.5" :width="rootWidth" height="30" rx="8" />
                <text class="mm" :x="panel.x + 10" y="20" style="fill: var(--vp-c-text-1)">{{ panel.root }}</text>
                <g v-for="(child, index) in panel.children" :key="child[0]">
                    <path
                        class="edge"
                        :d="`M${panel.x + 9} 31 L ${panel.x + 9} ${childY(index) + 15} L ${panel.x + 17} ${childY(index) + 15}`"
                    />
                    <rect class="node" :x="panel.x + 18" :y="childY(index)" :width="childWidth" height="30" rx="8" />
                    <text class="mm" :x="panel.x + 28" :y="childY(index) + 19.5" style="fill: var(--vp-c-text-1)">
                        {{ child[0] }}
                    </text>
                    <text class="s" :x="panel.x + 18 + childWidth - 10" :y="childY(index) + 19.5" text-anchor="end">
                        {{ child[1] }}
                    </text>
                </g>
                <text class="s" :x="panel.x" y="178">{{ panel.detail[0] }}</text>
                <text class="s" :x="panel.x" y="194">{{ panel.detail[1] }}</text>
            </g>
        </svg>
        <template #caption><slot /></template>
    </HlFigure>
</template>
