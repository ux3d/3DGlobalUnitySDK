float2 calculateUVForMosaic(uint viewIndex, float2 fullScreenUV, uint mosaic_rows = 4, uint mosaic_columns = 4) {
    viewIndex = max(0, viewIndex);
    uint xAxis = viewIndex % mosaic_columns;
    uint yAxis = viewIndex / mosaic_columns;
    // invert y axis to account for different coordinate systems between Unity and OpenGL (OpenGL has origin at bottom left)
    // The shader was written for OpenGL, so we need to invert the y axis to make it work in Unity.
    yAxis = mosaic_rows - 1 - yAxis;
    int2 moasicIndex = int2(xAxis, yAxis);
    float2 scaledUV = float2(fullScreenUV.x / mosaic_columns, fullScreenUV.y / mosaic_rows);
    float2 cellSize = float2(1.0 / mosaic_columns, 1.0 / mosaic_rows);
    return scaledUV + cellSize * moasicIndex;
}

uint getViewIndex(float2 cellCoordinates, int2 gridSize) {
    return uint(cellCoordinates.x) + gridSize.x * uint(cellCoordinates.y);
}

float2 getCellTexCoords(float2 cellCoordinates) {
    float2 uv = frac(cellCoordinates);
    return float2(uv.x, 1.0 - uv.y);
}
