// 12 colors, spaced evenly around the color wheel
export const loContrastColor = col => `hsl(${(col % 12) * 30}, 80%, 50%)`

// 12 colors, picking every 5th one around the wheel so there's high contrast
export const hiContrastColor = col => `hsl(${((col * 5) % 12) * 30}, 80%, 50%)`

export const columnColor = loContrastColor
