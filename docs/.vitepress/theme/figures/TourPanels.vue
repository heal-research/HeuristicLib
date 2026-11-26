<script setup lang="ts">
import { computed, ref } from "vue";
import HlFigure from "./HlFigure.vue";

// The best tour of the population at a few generations, one small panel each. Hovering a city marks it in every
// panel and shows its coordinates and its stop in the hovered tour.
const props = defineProps<{
    data: {
        optimum: number;
        cities: [number, number][];
        tours: { generation: number; length: number; tour: number[] }[];
    };
}>();

const viewWidth = 160;
const viewHeight = 112;
const pad = 7;

const panels = computed(() => {
    const xs = props.data.cities.map((city) => city[0]);
    const ys = props.data.cities.map((city) => city[1]);
    const [x0, x1, y0, y1] = [Math.min(...xs), Math.max(...xs), Math.min(...ys), Math.max(...ys)];
    const sx = (value: number) => pad + ((value - x0) / (x1 - x0)) * (viewWidth - 2 * pad);
    const sy = (value: number) => pad + ((y1 - value) / (y1 - y0)) * (viewHeight - 2 * pad);
    const cities = props.data.cities.map(([x, y]) => ({ x: sx(x), y: sy(y) }));
    const tours = props.data.tours.map((tour) => ({
        ...tour,
        d:
            tour.tour
                .map((city, i) => `${i ? "L" : "M"}${cities[city].x.toFixed(1)} ${cities[city].y.toFixed(1)}`)
                .join("") + "Z",
    }));
    return { cities, tours };
});

// Hover: the nearest city within six units of the pointer, in the panel under the pointer.
const root = ref<HTMLElement>();
const hover = ref<{ city: number; panel: number; left: number; top: number }>();

function onMove(event: PointerEvent, panel: number) {
    const element = event.currentTarget as SVGSVGElement;
    const matrix = element.getScreenCTM();
    const container = root.value;
    if (!matrix || !container) return;
    const cursor = new DOMPoint(event.clientX, event.clientY).matrixTransform(matrix.inverse());
    let nearest = -1;
    let distance = 6 * 6;
    panels.value.cities.forEach((city, index) => {
        const d = (city.x - cursor.x) ** 2 + (city.y - cursor.y) ** 2;
        if (d <= distance) {
            distance = d;
            nearest = index;
        }
    });
    if (nearest < 0) {
        hover.value = undefined;
        return;
    }
    const box = container.getBoundingClientRect();
    const left = event.clientX - box.left;
    hover.value = {
        city: nearest,
        panel,
        left: left > box.width - 180 ? left - 176 : left + 16,
        top: Math.max(0, event.clientY - box.top - 90),
    };
}

const hoverRows = computed(() => {
    const h = hover.value;
    if (!h) return [];
    const [x, y] = props.data.cities[h.city];
    const tour = props.data.tours[h.panel];
    return [
        { label: "x, y", value: `${x}, ${y}` },
        {
            label: `stop in generation ${tour.generation}`,
            value: `${tour.tour.indexOf(h.city) + 1} of ${tour.tour.length}`,
        },
    ];
});

const lengthText = (length: number) => length.toLocaleString("en-US");
const aboveOptimum = (length: number) => `${((length / props.data.optimum - 1) * 100).toFixed(1)} %`;

const tableHeaders = ["generation", "tour length", "above the optimum"];
const tableRows = computed(() =>
    props.data.tours.map((tour) => [tour.generation, lengthText(tour.length), aboveOptimum(tour.length)]),
);
</script>

<template>
    <HlFigure :table-headers="tableHeaders" :table-rows="tableRows">
        <div ref="root" class="hl-multiples hl-tours" style="position: relative">
            <div v-for="(tour, panel) in panels.tours" :key="tour.generation" class="panel">
                <div class="ph">
                    <span>gen {{ tour.generation }}</span
                    ><b>{{ lengthText(tour.length) }}</b>
                </div>
                <svg
                    :viewBox="`0 0 ${viewWidth} ${viewHeight}`"
                    role="img"
                    :aria-label="`Best tour at generation ${tour.generation}, length ${tour.length}`"
                    @pointermove="onMove($event, panel)"
                    @pointerleave="hover = undefined"
                >
                    <path class="hl-tour" :d="tour.d" />
                    <circle
                        v-for="(city, index) in panels.cities"
                        :key="index"
                        class="hl-city"
                        :cx="city.x"
                        :cy="city.y"
                        r="1.9"
                    />
                    <circle
                        v-if="hover"
                        class="hl-city-mark"
                        :cx="panels.cities[hover.city].x"
                        :cy="panels.cities[hover.city].y"
                        r="4.5"
                    />
                </svg>
            </div>
            <div v-if="hover" class="hl-tip" :style="{ left: `${hover.left}px`, top: `${hover.top}px` }">
                <div class="hd">City {{ hover.city + 1 }}</div>
                <div v-for="row in hoverRows" :key="row.label" class="row">
                    <span class="val">{{ row.value }}</span>
                    <span class="lab">{{ row.label }}</span>
                </div>
            </div>
        </div>
        <template #caption><slot /></template>
    </HlFigure>
</template>

<style scoped>
.hl-tours {
    grid-template-columns: repeat(4, minmax(0, 1fr));
}

@media (max-width: 640px) {
    .hl-tours {
        grid-template-columns: repeat(2, minmax(0, 1fr));
    }
}

.hl-tour {
    fill: none;
    stroke: var(--hl-fig-series-1);
    stroke-width: 1.3;
    stroke-linejoin: round;
}

.hl-city {
    fill: var(--vp-c-text-2);
}

.hl-city-mark {
    fill: none;
    stroke: var(--hl-fig-accent);
    stroke-width: 1.5;
}
</style>
