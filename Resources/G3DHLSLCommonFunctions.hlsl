float2 calculateUVForMosaic(int viewIndex, float2 fullScreenUV, int mosaic_rows = 4, int mosaic_columns = 4) {
    viewIndex = max(0, viewIndex);
    int xAxis = viewIndex % mosaic_columns;
    int yAxis = viewIndex / mosaic_columns;
    // invert y axis to account for different coordinate systems between Unity and OpenGL (OpenGL has origin at bottom left)
    // The shader was written for OpenGL, so we need to invert the y axis to make it work in Unity.
    yAxis = mosaic_rows - 1 - yAxis;
    int2 moasicIndex = int2(xAxis, yAxis);
    float2 scaledUV = float2(fullScreenUV.x / mosaic_columns, fullScreenUV.y / mosaic_rows);
    float2 cellSize = float2(1.0 / mosaic_columns, 1.0 / mosaic_rows);
    return scaledUV + cellSize * moasicIndex;
}


// the text coords of the original left and right view are from 0 - 1
// the tex coords of the texel we are currently rendering are also from 0 - 1
// but we want to create a grid of views, so we need to transform the tex coords
// to the grid size.
// basically we want to figure out in which grid cell the current texel is, then convert the texel coords to the grid cell coords.
// example assuming a grid size of 3x3:
// original tex coords: 0.8, 0.5
// step 1: transform the tex coords to the grid size by multiplying with grid size
//    -> e.g. original x coord 0.8 turns to 0.8 * 3 = 2.4
// step 2: figure out the grid cell by taking the integer part of the transformed tex coords
//    -> e.g. 2.4 turns to 2
// step 3: subtract the integer part from the transformed tex coords to get the texel coords in the grid cell
//   -> e.g. 2.4 - 2 = 0.4 -> final texel coords in the grid cell are 0.4, 0.5
float2 getCellCoordinates(float2 uv, int2 gridSize) {
    // flip y coordiate to have cell index 0 in upper left corner
    return float2(uv.x, 1.0 - uv.y) * float2(gridSize);
}

uint getViewIndex(float2 cellCoordinates, int2 gridSize) {
    return uint(cellCoordinates.x) + gridSize.x * uint(cellCoordinates.y);
}

float2 getCellTexCoords(float2 cellCoordinates) {
    float2 uv = frac(cellCoordinates);
    return float2(uv.x, 1.0 - uv.y);
}
