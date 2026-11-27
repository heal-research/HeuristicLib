// Scales and number formats shared by the charts.

export interface Scale {
    (value: number): number;
    invert(position: number): number;
}

export function linearScale(d0: number, d1: number, r0: number, r1: number): Scale {
    const scale = ((value: number) => r0 + ((value - d0) / (d1 - d0)) * (r1 - r0)) as Scale;
    scale.invert = (position: number) => d0 + ((position - r0) / (r1 - r0)) * (d1 - d0);
    return scale;
}

export function logScale(d0: number, d1: number, r0: number, r1: number): Scale {
    const l0 = Math.log10(d0);
    const l1 = Math.log10(d1);
    const scale = ((value: number) => r0 + ((Math.log10(Math.max(value, d0)) - l0) / (l1 - l0)) * (r1 - r0)) as Scale;
    scale.invert = (position: number) => 10 ** (l0 + ((position - r0) / (r1 - r0)) * (l1 - l0));
    return scale;
}

const superscripts: Record<string, string> = {
    "-": "⁻",
    "0": "⁰",
    "1": "¹",
    "2": "²",
    "3": "³",
    "4": "⁴",
    "5": "⁵",
    "6": "⁶",
    "7": "⁷",
    "8": "⁸",
    "9": "⁹",
};

/** Formats a log-scale tick: values below one as a power of ten, others as written. */
export function formatPower(value: number): string {
    if (value >= 1) return String(value);
    const exponent = String(Math.round(Math.log10(value)));
    return "10" + [...exponent].map((c) => superscripts[c]).join("");
}

/** Formats a value for a readout or table: exponent notation below 0.001, four decimals otherwise. */
export function formatValue(value: number): string {
    if (value === 0) return "0";
    return Math.abs(value) < 0.001 ? value.toExponential(2) : value.toFixed(4);
}
