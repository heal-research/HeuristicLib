<script setup lang="ts">
import HlFigure from "./HlFigure.vue";

// The two orders of a cache and an evaluation limit around one problem evaluator. A request enters the outer layer,
// and a cache hit returns from the cache layer without reaching the layers inside it.
const panels = [
    {
        x: 0.5,
        code: "problemEvaluator.Cached(k).LimitEvaluations(n)",
        layers: [
            ["limit", "every request spends allowance"],
            ["cache", "a hit returns here"],
        ],
        verdict: "The budget counts evaluation requests.",
    },
    {
        x: 352.5,
        code: "problemEvaluator.LimitEvaluations(n).Cached(k)",
        layers: [
            ["cache", "a hit returns here"],
            ["limit", "only misses spend allowance"],
        ],
        verdict: "The budget counts problem evaluations.",
    },
];
const width = 328;
</script>

<template>
    <HlFigure>
        <svg
            class="hl-dg"
            viewBox="-14 -14 708 248"
            role="img"
            aria-label="Left: the limit sits outside the cache, so every request spends allowance, including cache hits. Right: the cache sits outside the limit, so a hit returns before the limit and only misses spend allowance."
        >
            <g v-for="panel in panels" :key="panel.code">
                <text class="mm" :x="panel.x" y="10" style="fill: var(--vp-c-text-1)">{{ panel.code }}</text>
                <rect class="layer1" :x="panel.x" y="24.5" :width="width" height="148" rx="14" />
                <rect class="layer2" :x="panel.x + 18" y="62.5" :width="width - 36" height="98" rx="12" />
                <rect class="node primary" :x="panel.x + 36" y="100.5" :width="width - 72" height="46" rx="10" />
                <text class="t" :x="panel.x + 14" y="48">{{ panel.layers[0][0] }}</text>
                <text class="s" :x="panel.x + 66" y="48">{{ panel.layers[0][1] }}</text>
                <text class="t" :x="panel.x + 32" y="86">{{ panel.layers[1][0] }}</text>
                <text class="s" :x="panel.x + 86" y="86">{{ panel.layers[1][1] }}</text>
                <text class="t" :x="panel.x + width / 2" y="128" text-anchor="middle">problem evaluator</text>
                <text class="t" :x="panel.x" y="200">{{ panel.verdict }}</text>
            </g>
        </svg>
        <template #caption><slot /></template>
    </HlFigure>
</template>
