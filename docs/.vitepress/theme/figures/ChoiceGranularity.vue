<script setup lang="ts">
import { useId } from "vue";
import ArrowMarkers from "./ArrowMarkers.vue";
import HlFigure from "./HlFigure.vue";

// One mutation call of six candidates, once with a choice per item and once with one choice for the whole call.
const id = useId();
const arrow = `url(#${id}-arrow)`;

const cell = 30;
const cellX = (index: number) => 196.5 + index * (cell + 4);
const perItem = ["A", "A", "B", "A", "B", "A"];
const groupX = 448.5;
const groupWidth = 196;
</script>

<template>
    <HlFigure>
        <svg
            class="hl-dg"
            viewBox="-14 -30 708 238"
            role="img"
            aria-label="With ChoosePerItem, each of six candidates draws its own child: four go to child A and two to child B, as one call for each child, and the results return in input order. With ChoosePerInvocation, the call draws once and all six candidates go to child B."
        >
            <ArrowMarkers :id="id" />
            <text class="mt" x="0" y="20">ChoosePerItem</text>
            <text class="s" x="0" y="38">one draw per candidate</text>
            <g v-for="(child, index) in perItem" :key="`item${index}`">
                <rect
                    class="cell"
                    :class="{ on: child === 'A' }"
                    :x="cellX(index)"
                    y="0.5"
                    :width="cell"
                    height="30"
                    rx="5"
                />
                <text
                    class="cell-t"
                    :class="{ on: child === 'A' }"
                    :x="cellX(index) + cell / 2"
                    y="20"
                    text-anchor="middle"
                >
                    {{ child }}
                </text>
            </g>
            <path class="edge" :d="`M${cellX(6) + 4} 15 C 420 15, 420 -2, ${groupX - 2} -2`" :marker-end="arrow" />
            <path class="edge" :d="`M${cellX(6) + 4} 15 C 420 15, 420 40, ${groupX - 2} 40`" :marker-end="arrow" />
            <g class="shadow">
                <rect class="node" :x="groupX" y="-14.5" :width="groupWidth" height="26" rx="7" />
                <rect class="node" :x="groupX" y="27.5" :width="groupWidth" height="26" rx="7" />
            </g>
            <text class="t" :x="groupX + 12" y="3">child A</text>
            <text class="s" :x="groupX + groupWidth - 12" y="3" text-anchor="end">one call, 4 items</text>
            <text class="t" :x="groupX + 12" y="45">child B</text>
            <text class="s" :x="groupX + groupWidth - 12" y="45" text-anchor="end">one call, 2 items</text>
            <text class="el" :x="groupX" y="74">results return in input order</text>

            <line class="divider" x1="0" x2="680" y1="96" y2="96" />

            <text class="mt" x="0" y="136">ChoosePerInvocation</text>
            <text class="s" x="0" y="154">one draw per call</text>
            <g v-for="index in 6" :key="`call${index}`">
                <rect class="cell mute" :x="cellX(index - 1)" y="116.5" :width="cell" height="30" rx="5" />
            </g>
            <path class="edge" :d="`M${cellX(6) + 4} 131.5 L ${groupX - 2} 131.5`" :marker-end="arrow" />
            <g class="shadow"><rect class="node" :x="groupX" y="118.5" :width="groupWidth" height="26" rx="7" /></g>
            <text class="t" :x="groupX + 12" y="136">child B</text>
            <text class="s" :x="groupX + groupWidth - 12" y="136" text-anchor="end">one call, 6 items</text>
            <text class="el" :x="groupX" y="168">the same child for the whole batch</text>
        </svg>
        <template #caption><slot /></template>
    </HlFigure>
</template>
