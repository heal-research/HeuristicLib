import DefaultTheme from "vitepress/theme";
import type { Theme } from "vitepress";
import { h } from "vue";
import AnalysisChoice from "./figures/AnalysisChoice.vue";
import CacheLimitOrder from "./figures/CacheLimitOrder.vue";
import CapacityBars from "./figures/CapacityBars.vue";
import ChoiceGranularity from "./figures/ChoiceGranularity.vue";
import CompositionTrees from "./figures/CompositionTrees.vue";
import ConfigurationExecutors from "./figures/ConfigurationExecutors.vue";
import DominanceRegions from "./figures/DominanceRegions.vue";
import EncodingGallery from "./figures/EncodingGallery.vue";
import ExperimentGrid from "./figures/ExperimentGrid.vue";
import ExpressionFit from "./figures/ExpressionFit.vue";
import ForkTree from "./figures/ForkTree.vue";
import GaGeneration from "./figures/GaGeneration.vue";
import HeroSearch from "./figures/HeroSearch.vue";
import HlFigure from "./figures/HlFigure.vue";
import InvariantCheck from "./figures/InvariantCheck.vue";
import LayerStack from "./figures/LayerStack.vue";
import LineChart from "./figures/LineChart.vue";
import ModelDiagram from "./figures/ModelDiagram.vue";
import RunLifecycle from "./figures/RunLifecycle.vue";
import ScatterChart from "./figures/ScatterChart.vue";
import TourPanels from "./figures/TourPanels.vue";
import WrapperOrder from "./figures/WrapperOrder.vue";
import "./custom.css";
import "./figures/figures.css";

export default {
    extends: DefaultTheme,
    Layout: () => h(DefaultTheme.Layout, null, { "home-hero-image": () => h(HeroSearch) }),
    enhanceApp({ app }) {
        app.component("AnalysisChoice", AnalysisChoice);
        app.component("CacheLimitOrder", CacheLimitOrder);
        app.component("CapacityBars", CapacityBars);
        app.component("ChoiceGranularity", ChoiceGranularity);
        app.component("CompositionTrees", CompositionTrees);
        app.component("ConfigurationExecutors", ConfigurationExecutors);
        app.component("DominanceRegions", DominanceRegions);
        app.component("EncodingGallery", EncodingGallery);
        app.component("ExperimentGrid", ExperimentGrid);
        app.component("ExpressionFit", ExpressionFit);
        app.component("ForkTree", ForkTree);
        app.component("GaGeneration", GaGeneration);
        app.component("HlFigure", HlFigure);
        app.component("InvariantCheck", InvariantCheck);
        app.component("LayerStack", LayerStack);
        app.component("LineChart", LineChart);
        app.component("ModelDiagram", ModelDiagram);
        app.component("RunLifecycle", RunLifecycle);
        app.component("ScatterChart", ScatterChart);
        app.component("TourPanels", TourPanels);
        app.component("WrapperOrder", WrapperOrder);
    },
} satisfies Theme;
