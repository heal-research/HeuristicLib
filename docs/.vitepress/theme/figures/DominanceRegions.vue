<script setup lang="ts">
import HlFigure from "./HlFigure.vue";

// The four regions around one design when material is minimized and volume is maximized.
const plot = { l: 150, r: 530, t: 0, b: 250 };
const a = { x: 340, y: 125 };
</script>

<template>
    <HlFigure>
        <svg
            class="hl-dg"
            viewBox="-14 -14 708 300"
            role="img"
            aria-label="Around design A, the region with less material and more volume holds the designs that dominate A. The region with more material and less volume holds the designs A dominates. The two other regions hold designs that are not comparable with A."
        >
            <rect class="region" :x="plot.l" :y="plot.t" :width="a.x - plot.l" :height="a.y - plot.t" />
            <rect class="region mute" :x="a.x" :y="a.y" :width="plot.r - a.x" :height="plot.b - a.y" />
            <line class="divider" :x1="a.x" :x2="a.x" :y1="plot.t" :y2="plot.b" />
            <line class="divider" :x1="plot.l" :x2="plot.r" :y1="a.y" :y2="a.y" />
            <path class="axis-line" :d="`M${plot.l} ${plot.t} L ${plot.l} ${plot.b} L ${plot.r} ${plot.b}`" />

            <text class="t" :x="(plot.l + a.x) / 2" y="52" text-anchor="middle">dominate A</text>
            <text class="s" :x="(plot.l + a.x) / 2" y="70" text-anchor="middle">less material, more volume</text>
            <text class="t" :x="(a.x + plot.r) / 2" y="190" text-anchor="middle">dominated by A</text>
            <text class="s" :x="(a.x + plot.r) / 2" y="208" text-anchor="middle">more material, less volume</text>
            <text class="t" :x="(a.x + plot.r) / 2" y="52" text-anchor="middle">not comparable</text>
            <text class="s" :x="(a.x + plot.r) / 2" y="70" text-anchor="middle">holds more, costs more</text>
            <text class="t" :x="(plot.l + a.x) / 2" y="190" text-anchor="middle">not comparable</text>
            <text class="s" :x="(plot.l + a.x) / 2" y="208" text-anchor="middle">costs less, holds less</text>

            <circle class="dot" cx="205" cy="100" r="5" />
            <circle class="dot" cx="280" cy="30" r="5" />
            <circle class="dot context" cx="420" cy="150" r="5" />
            <circle class="dot context" cx="490" cy="230" r="5" />
            <circle class="dot accent" :cx="a.x" :cy="a.y" r="6.5" />
            <text class="t" :x="a.x + 11" :y="a.y - 9">A</text>

            <text class="el" :x="plot.r" :y="plot.b + 20" text-anchor="end">material, minimized →</text>
            <text class="el" :x="plot.l - 10" :y="plot.t + 4" text-anchor="end">volume,</text>
            <text class="el" :x="plot.l - 10" :y="plot.t + 19" text-anchor="end">maximized ↑</text>
        </svg>
        <template #caption><slot /></template>
    </HlFigure>
</template>
