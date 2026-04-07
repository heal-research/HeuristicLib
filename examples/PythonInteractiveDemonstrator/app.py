"""
Symbolic Regression Drawing Demonstrator (HeuristicLib)
========================================================
Draw a curve on a fullscreen grid canvas. HeuristicLib (via pythonnet) fits a
symbolic expression using Genetic Programming. Each generation's population is
streamed to the frontend for live visualization.

Run:  python app.py
Open: http://localhost:8765/
"""

import asyncio
import json
import math
import multiprocessing as mp
import os
import random as stdlib_random
import signal
import time
from pathlib import Path

import numpy as np
import uvicorn
from fastapi import FastAPI, WebSocket, WebSocketDisconnect
from fastapi.responses import HTMLResponse
from fastapi.staticfiles import StaticFiles

HERE = Path(__file__).resolve().parent
PUBLISH_DIR = str(
    (HERE / ".." / ".." / "src" / "HeuristicLib.Extensions" / "bin" / "Release" / "net10.0" / "publish").resolve()
)

# ---------------------------------------------------------------------------
# Default GP parameters (server-side source of truth)
# ---------------------------------------------------------------------------

DEFAULT_PARAMS = {
    "allowed_symbols": "add,sub,mul,div,sqrt,log,constant,variable",
    "population_size": 200,
    "generations": 30,
    "tree_length": 40,
    "tree_depth": 20,
    "mutation_rate": 0.1,
    "tournament_size": 4,
    "elites": 1,
    "parameter_optimization_iterations": 5,
    "use_linear_scaling": True,
    "seed": -1,
    "step_delay_ms": 0,
    "max_displayed_curves": 50,
}

_CLIENT_KEYS = set(DEFAULT_PARAMS.keys())


# ---------------------------------------------------------------------------
# Sympy helpers for LaTeX conversion
# ---------------------------------------------------------------------------

def _expr_to_latex(expr_str: str) -> str:
    """Best-effort conversion of an infix expression string to LaTeX via sympy."""
    try:
        import sympy as sy
        # Normalise HeuristicLib variable names: "x" is used as the single variable
        cleaned = expr_str.replace("'x'", "x").lower()

        simplified = sy.parse_expr(cleaned, transformations="all")
        simplified = sy.nsimplify(simplified, tolerance=1e-4, rational=False)

        # Round floats for display
        for atom in list(simplified.atoms(sy.Number)):
            if isinstance(atom, sy.Float) or (isinstance(atom, sy.Rational) and atom.q != 1):
                simplified = simplified.subs(atom, sy.Float(round(float(atom), 4)))

        return sy.latex(simplified)
    except Exception as e:
        print(f"Error converting to LaTeX: {e}")
        return expr_str


# ---------------------------------------------------------------------------
# Regression subprocess
# ---------------------------------------------------------------------------

def _run_regression(points_json: str, params_json: str, result_queue: mp.Queue):
    """Run symbolic regression via HeuristicLib in a child process."""
    signal.signal(signal.SIGINT, signal.SIG_IGN)

    try:
        # --- pythonnet bootstrap -------------------------------------------
        from pythonnet import load
        load("coreclr")

        import clr  # noqa: E402
        import sys as _sys
        _sys.path.append(PUBLISH_DIR)
        clr.AddReference("HEAL.HeuristicLib.Extensions")

        from HEAL.HeuristicLib.PythonInterOptScripts import (
            InteractiveSymbolicRegression,
            InteractiveSymRegParameters,
        )
        from HEAL.HeuristicLib.Genotypes.Trees import SymbolicExpressionTree
        from HEAL.HeuristicLib.Optimization import ObjectiveVector
        from System import Func, Array, Double

        # --- parse points & bin by x --------------------------------------
        pts = json.loads(points_json)
        xs = np.array([p[0] for p in pts], dtype=np.float64)
        ys = np.array([p[1] for p in pts], dtype=np.float64)

        n_bins = min(200, len(xs))
        x_min, x_max = float(xs.min()), float(xs.max())
        if x_max - x_min < 1e-9:
            result_queue.put({"type": "error", "message": "Draw a wider curve"})
            return

        bin_edges = np.linspace(x_min, x_max, n_bins + 1)
        bin_idx = np.clip(np.digitize(xs, bin_edges) - 1, 0, n_bins - 1)

        x_binned, y_binned = [], []
        for i in range(n_bins):
            mask = bin_idx == i
            if mask.any():
                x_binned.append(float(xs[mask].mean()))
                y_binned.append(float(ys[mask].mean()))

        if len(x_binned) < 3:
            result_queue.put({"type": "error", "message": "Need more points"})
            return

        x_arr = np.array(x_binned, dtype=np.float64)
        y_arr = np.array(y_binned, dtype=np.float64)

        # --- merge client params -------------------------------------------
        params = dict(DEFAULT_PARAMS)
        client_params = json.loads(params_json) if params_json else {}
        for k, v in client_params.items():
            if k in _CLIENT_KEYS:
                params[k] = v

        # Dense x for curve evaluation
        # x_dense = np.linspace(x_min, x_max, 300) # draw only within the x-range of the input points to avoid extrapolation artifacts
        x_dense = np.linspace(-10, 10, 300) # draw for full range to show extrapolation behavior, can be adjusted as needed

        # --- configure HeuristicLib parameters -----------------------------
        hl_params = InteractiveSymRegParameters()
        hl_params.PopulationSize = int(params["population_size"])
        hl_params.Generations = int(params["generations"])
        hl_params.TreeLength = int(params["tree_length"])
        hl_params.TreeDepth = int(params["tree_depth"])
        hl_params.MutationRate = float(params["mutation_rate"])
        hl_params.TournamentSize = int(params["tournament_size"])
        hl_params.Elites = int(params["elites"])
        hl_params.ParameterOptimizationIterations = int(params["parameter_optimization_iterations"])
        hl_params.Seed = int(params["seed"])
        hl_params.UseLinearScaling = bool(params["use_linear_scaling"])

        # Allowed symbols as .NET string array
        sym_list = [s.strip() for s in str(params["allowed_symbols"]).split(",") if s.strip()]
        hl_params.AllowedSymbols = Array[str](sym_list)

        step_delay_s = max(0, int(params.get("step_delay_ms", 0))) / 1000.0
        max_curves = max(1, int(params.get("max_displayed_curves", 50)))
        total_gens = int(params["generations"])
        gen_counter = [0]

        # --- population callback (called each generation) ------------------
        def population_callback(trees, objectives):
            gen_counter[0] += 1
            gen = gen_counter[0]

            pop_size = len(trees)
            # Find best individual (highest R² = first objective)
            best_idx = 0
            best_r2 = float("-inf")
            r2_values = []
            for i in range(pop_size):
                r2 = float(objectives[i][0])
                r2_values.append(r2)
                if r2 > best_r2:
                    best_r2 = r2
                    best_idx = i

            # Format best expression
            best_expr = InteractiveSymbolicRegression.FormatTree(trees[best_idx])
            best_latex = _expr_to_latex(best_expr)

            # Predict best curve
            best_y = InteractiveSymbolicRegression.PredictValues(trees[best_idx], Array[Double](x_dense.tolist()))
            best_curve = list(zip(x_dense.tolist(), [float(v) for v in best_y]))

            # Sample population curves (up to max_curves)
            sampled_curves = []
            if pop_size <= max_curves:
                indices = list(range(pop_size))
            else:
                indices = sorted(stdlib_random.sample(range(pop_size), max_curves))

            for idx in indices:
                try:
                    y_pred = InteractiveSymbolicRegression.PredictValues(trees[idx], Array[Double](x_dense.tolist()))
                    curve = list(zip(x_dense.tolist(), [float(v) for v in y_pred]))
                    sampled_curves.append(curve)
                except Exception:
                    pass

            result_queue.put({
                "type": "generation",
                "gen": gen,
                "totalGens": total_gens,
                "bestR2": round(best_r2, 6),
                "bestExpr": best_expr,
                "bestLatex": best_latex,
                "bestCurve": best_curve,
                "populationCurves": sampled_curves,
            })

            if step_delay_s > 0:
                time.sleep(step_delay_s)

            # Pass through original objectives unchanged
            result = []
            for i in range(pop_size):
                result.append(Array[Double]([float(objectives[i][0])]))
            return Array[Array[Double]](result)

        callback_func = Func[
            Array[SymbolicExpressionTree],
            Array[ObjectiveVector],
            Array[Array[Double]]
        ](population_callback)

        # --- run GP --------------------------------------------------------
        x_net = Array[Double](x_arr.tolist())
        y_net = Array[Double](y_arr.tolist())

        population = InteractiveSymbolicRegression.Run(
            x_net, y_net, callback_func, hl_params
        )

        # --- extract top solutions -----------------------------------------
        top_solutions = []
        for sol in population.Solutions:
            tree = sol.Genotype
            r2 = float(sol.ObjectiveVector[0])
            expr = InteractiveSymbolicRegression.FormatTree(tree)
            latex = _expr_to_latex(expr)
            try:
                y_pred = InteractiveSymbolicRegression.PredictValues(tree, Array[Double](x_dense.tolist()))
                curve = list(zip(x_dense.tolist(), [float(v) for v in y_pred]))
            except Exception:
                curve = []
            top_solutions.append({
                "r2": round(r2, 6),
                "length": tree.Length,
                "depth": tree.Depth,
                "expr": expr,
                "latex": latex,
                "curve": curve,
            })

        # Sort by R² descending, take top 20
        top_solutions.sort(key=lambda s: s["r2"], reverse=True)
        top_solutions = top_solutions[:20]

        # Best overall
        best_sol = top_solutions[0] if top_solutions else None
        result_queue.put({
            "type": "result",
            "expression": best_sol["expr"] if best_sol else "",
            "latex": best_sol["latex"] if best_sol else "",
            "curve": best_sol["curve"] if best_sol else [],
            "topSolutions": top_solutions,
            "bestIdx": 0,
        })

    except Exception as e:
        result_queue.put({"type": "error", "message": str(e)})


# ---------------------------------------------------------------------------
# FastAPI app
# ---------------------------------------------------------------------------

app = FastAPI()
app.mount("/static", StaticFiles(directory=str(HERE / "static")), name="static")


@app.get("/", response_class=HTMLResponse)
async def index():
    return (HERE / "templates" / "index.html").read_text()


@app.websocket("/ws")
async def websocket_endpoint(websocket: WebSocket):
    await websocket.accept()

    process: mp.Process | None = None
    result_queue: mp.Queue | None = None
    paused: bool = False
    last_points: str | None = None
    last_params: str | None = None

    def _kill_process():
        nonlocal process, result_queue, paused
        if process is not None and process.is_alive():
            if paused:
                try:
                    os.kill(process.pid, signal.SIGCONT)
                except OSError:
                    pass
            process.terminate()
            process.join(timeout=2)
            if process.is_alive():
                process.kill()
                process.join(timeout=1)
        process = None
        result_queue = None
        paused = False

    def _start_regression(points_json: str, params_json: str):
        nonlocal process, result_queue, last_points, last_params, paused
        _kill_process()
        last_points = points_json
        last_params = params_json
        result_queue = mp.Queue()
        process = mp.Process(
            target=_run_regression,
            args=(points_json, params_json, result_queue),
            daemon=True,
        )
        process.start()
        paused = False

    try:
        while True:
            try:
                raw = await asyncio.wait_for(websocket.receive_text(), timeout=0.1)
                msg = json.loads(raw)
            except asyncio.TimeoutError:
                msg = None
            except WebSocketDisconnect:
                break

            if msg is not None:
                mtype = msg["type"]

                if mtype == "cancel":
                    _kill_process()

                elif mtype == "fit":
                    points = msg["points"]
                    params = msg.get("params")
                    _start_regression(
                        json.dumps(points),
                        json.dumps(params) if params else "{}",
                    )

                elif mtype == "pause":
                    if process is not None and process.is_alive() and not paused:
                        os.kill(process.pid, signal.SIGSTOP)
                        paused = True
                        await websocket.send_text(json.dumps({"type": "paused"}))

                elif mtype == "resume":
                    if process is not None and process.is_alive() and paused:
                        new_params = json.dumps(msg.get("params")) if msg.get("params") else "{}"
                        if new_params == last_params:
                            os.kill(process.pid, signal.SIGCONT)
                            paused = False
                            await websocket.send_text(json.dumps({"type": "resumed"}))
                        else:
                            if last_points is not None:
                                _start_regression(last_points, new_params)
                                await websocket.send_text(json.dumps({"type": "resumed"}))

                elif mtype == "stop":
                    _kill_process()
                    await websocket.send_text(json.dumps({"type": "stopped"}))

            # Poll result queue — forward all messages to WebSocket
            if result_queue is not None and not paused:
                try:
                    result = result_queue.get_nowait()
                    await websocket.send_text(json.dumps(result))
                    # Only clean up process on final result/error
                    if result.get("type") in ("result", "error"):
                        if process is not None:
                            process.join(timeout=1)
                        process = None
                        result_queue = None
                except Exception:
                    pass  # queue empty, keep polling

    except WebSocketDisconnect:
        pass
    finally:
        _kill_process()


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    mp.set_start_method("spawn", force=True)
    uvicorn.run(app, host="0.0.0.0", port=8765)
