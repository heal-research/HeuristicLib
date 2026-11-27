<script setup lang="ts">
import HlFigure from "./HlFigure.vue";

// The declaration check over a fixed cardinality space: each invariant the space states against what each mutator
// declares about its output.
const columnX = [236.5, 386.5, 536.5];
const columnWidth = 140;
const operators = [
    { name: "BitSwapMutator", cells: ["keeps", "keeps"], verdict: "accepted" },
    { name: "BitFlipMutator", cells: ["keeps", "does not keep"], verdict: "refused" },
    { name: "a custom mutator", cells: ["declares nothing", "declares nothing"], verdict: "not reported" },
];
const invariants = [{ label: "length 4" }, { label: "2 set elements" }];
const rowY = (index: number) => 52.5 + index * 46;
const verdictY = rowY(invariants.length) + 10;
</script>

<template>
    <HlFigure>
        <svg
            class="hl-dg"
            viewBox="-14 -14 708 210"
            role="img"
            aria-label="A FixedCardinalityBoolVectorSearchSpace states a length of 4 and 2 set elements. BitSwapMutator keeps both and is accepted. BitFlipMutator keeps the length but declares that it does not keep the number of set elements, so it is refused. A custom mutator that declares nothing is not reported."
        >
            <text class="t" x="0" y="24">The space states</text>
            <g v-for="(operator, c) in operators" :key="operator.name">
                <text
                    :class="operator.name.includes(' ') ? 't' : 'mt'"
                    :x="columnX[c] + columnWidth / 2"
                    y="24"
                    text-anchor="middle"
                >
                    {{ operator.name }}
                </text>
                <g v-for="(text, r) in operator.cells" :key="r">
                    <rect
                        class="cell"
                        :class="{ on: text === 'keeps', mute: text !== 'keeps' }"
                        :x="columnX[c]"
                        :y="rowY(r)"
                        :width="columnWidth"
                        height="34"
                        rx="7"
                    />
                    <text
                        class="s"
                        :x="columnX[c] + columnWidth / 2"
                        :y="rowY(r) + 21.5"
                        text-anchor="middle"
                        :style="text === 'keeps' ? { fill: 'var(--hl-fig-seq-on-hi)' } : undefined"
                    >
                        {{
                            text === "keeps"
                                ? "✓ keeps"
                                : text === "does not keep"
                                  ? "✗ does not keep"
                                  : "– declares nothing"
                        }}
                    </text>
                </g>
                <text class="t" :x="columnX[c] + columnWidth / 2" :y="verdictY + 22" text-anchor="middle">
                    {{ operator.verdict }}
                </text>
            </g>
            <g v-for="(invariant, r) in invariants" :key="invariant.label">
                <text class="t" x="0" :y="rowY(r) + 22">{{ invariant.label }}</text>
            </g>
            <line class="divider" x1="0" x2="680" :y1="verdictY" :y2="verdictY" />
            <text class="s" x="0" :y="verdictY + 22">creating the run</text>
        </svg>
        <template #caption><slot /></template>
    </HlFigure>
</template>
