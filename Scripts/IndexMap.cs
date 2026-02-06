using System;
using System.Collections.Generic;

public class IndexMap
{
    private static readonly IndexMap internalInstance = new IndexMap();

    // Explicit static constructor to tell C# compiler
    // not to mark type as beforefieldinit
    static IndexMap()
    {
        internalInstance.generateDefaultMaps();
    }

    public static IndexMap Instance
    {
        get { return internalInstance; }
    }

    private int BLANK_VIEW = 250; // corresponds to a black view
    public Dictionary<string, int[]> maps;

    public int[] currentMap = new int[] { 0 };

    public void generateDefaultMaps()
    {
        maps = new Dictionary<string, int[]>();

        string name = "A";
        int[] mapArray = new int[35];
        generate_indexmap(ref mapArray, mapArray.Length, 2, 0.0f, 1.0f, false);
        maps[name] = mapArray;

        name = "B";
        mapArray = new int[35];
        generate_indexmap(ref mapArray, mapArray.Length, 6, 0.0f, 1.0f, false);
        maps[name] = mapArray;

        name = "C";
        mapArray = new int[35];
        generate_indexmap(ref mapArray, mapArray.Length, 8, 0.0f, 1.0f, false);
        maps[name] = mapArray;

        name = "D";
        mapArray = new int[35];
        generate_indexmap(ref mapArray, mapArray.Length, 9, 0.0f, 1.0f, false);
        maps[name] = mapArray;

        name = "E";
        mapArray = new int[35];
        generate_indexmap(ref mapArray, mapArray.Length, 16, 0.0f, 1.0f, false);
        maps[name] = mapArray;

        name = "S1D";
        maps.Add(
            name,
            new int[]
            {
                250,
                250,
                250,
                250,
                250,
                250,
                250,
                250,
                250,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                250,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                250,
                250,
                250,
                250,
                250,
                250,
                250,
                250,
                250
            }
        );

        name = "S2D";
        maps.Add(
            name,
            new int[]
            {
                250,
                250,
                250,
                250,
                250,
                250,
                250,
                250,
                250,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                250,
                250,
                250,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                250,
                250,
                250,
                250,
                250,
                250,
                250,
                250,
                250
            }
        );

        name = "S3D";
        maps.Add(
            name,
            new int[]
            {
                250,
                250,
                250,
                250,
                250,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                250,
                250,
                250,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                250,
                250,
                250,
                250,
                250
            }
        );

        name = "S4D";
        maps.Add(
            name,
            new int[]
            {
                250,
                250,
                250,
                250,
                250,
                250,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                250,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                250,
                250,
                250,
                250,
                250,
                250
            }
        );

        name = "S6D";
        maps.Add(
            name,
            new int[]
            {
                250,
                250,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                250,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                250,
                250
            }
        );

        name = "05A";
        maps.Add(name, new int[] { 0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4 });

        name = "05C";
        maps.Add(name, new int[] { 0, 1, 2, 3, 4 });

        name = "05N";
        maps.Add(name, new int[] { 0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4 });

        name = "05O";
        maps.Add(name, new int[] { 0, 1, 2, 3, 4 });

        name = "05P";
        maps.Add(name, new int[] { 0, 1, 2, 3, 4 });

        name = "05Q";
        maps.Add(
            name,
            new int[] { 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4 }
        );

        name = "05R";
        maps.Add(name, new int[] { 0, 1, 2, 3, 4 });

        name = "07A";
        maps.Add(name, new int[] { 0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5, 5, 5, 6, 6, 6 });

        name = "07D";
        maps.Add(
            name,
            new int[]
            {
                0,
                0,
                0,
                0,
                0,
                1,
                1,
                1,
                1,
                1,
                2,
                2,
                2,
                2,
                2,
                3,
                3,
                3,
                3,
                3,
                4,
                4,
                4,
                4,
                4,
                5,
                5,
                5,
                5,
                5,
                6,
                6,
                6,
                6,
                6
            }
        );

        name = "08A";
        maps.Add(
            name,
            new int[] { 0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5, 5, 5, 6, 6, 6, 7, 7, 7 }
        );

        name = "08B";
        maps.Add(name, new int[] { 0, 1, 2, 3, 4, 5, 6, 7 });

        name = "08D";
        maps.Add(
            name,
            new int[]
            {
                0,
                0,
                0,
                0,
                0,
                1,
                1,
                1,
                1,
                1,
                2,
                2,
                2,
                2,
                2,
                3,
                3,
                3,
                3,
                3,
                4,
                4,
                4,
                4,
                4,
                5,
                5,
                5,
                5,
                5,
                6,
                6,
                6,
                6,
                6,
                7,
                7,
                7,
                7,
                7
            }
        );

        name = "08O";
        maps.Add(name, new int[] { 0, 1, 2, 3, 4, 5, 6, 7 });

        name = "08Q";
        maps.Add(
            name,
            new int[]
            {
                0,
                0,
                0,
                0,
                0,
                1,
                1,
                1,
                1,
                1,
                2,
                2,
                2,
                2,
                2,
                3,
                3,
                3,
                3,
                3,
                4,
                4,
                4,
                4,
                4,
                5,
                5,
                5,
                5,
                5,
                6,
                6,
                6,
                6,
                6,
                7,
                7,
                7,
                7,
                7
            }
        );

        name = "120views";
        maps.Add(
            name,
            new int[]
            {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
                9,
                10,
                11,
                12,
                13,
                14,
                15,
                16,
                17,
                18,
                19,
                20,
                21,
                22,
                23,
                24,
                25,
                26,
                27,
                28,
                29,
                30,
                31,
                32,
                33,
                34,
                35,
                36,
                37,
                38,
                39,
                40,
                41,
                42,
                43,
                44,
                45,
                46,
                47,
                48,
                49,
                50,
                51,
                52,
                53,
                54,
                55,
                56,
                57,
                58,
                59,
                60,
                61,
                62,
                63,
                64,
                65,
                66,
                67,
                68,
                69,
                70,
                71,
                72,
                73,
                74,
                75,
                76,
                77,
                78,
                79,
                80,
                81,
                82,
                83,
                84,
                85,
                86,
                87,
                88,
                89,
                90,
                91,
                92,
                93,
                94,
                95,
                96,
                97,
                98,
                99,
                100,
                101,
                102,
                103,
                104,
                105,
                106,
                107,
                108,
                109,
                110,
                111,
                112,
                113,
                114,
                115,
                116,
                117,
                118,
                119
            }
        );

        name = "A8A";
        maps.Add(
            name,
            new int[] { 0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5, 5, 5, 6, 6, 6, 7, 7, 7 }
        );

        name = "A8B";
        maps.Add(name, new int[] { 0, 1, 2, 3, 4, 5, 6, 7 });

        name = "A8O";
        maps.Add(name, new int[] { 0, 1, 2, 3, 4, 5, 6, 7 });

        name = "IPRO_CUS01";
        maps.Add(
            name,
            new int[]
            {
                0,
                77,
                0,
                77,
                0,
                77,
                0,
                77,
                77,
                77,
                77,
                77,
                1,
                77,
                1,
                77,
                1,
                77,
                1,
                77,
                77,
                77,
                77,
                77
            }
        ); // 77 == blank? if so: 77 = 250!
    }

    private void generate_indexmap(
        ref int[] indexmap,
        int availableViews,
        int contentViews,
        float delimiterViews,
        float yoyoStart,
        bool invert,
        bool invertIndices = false
    )
    {
        // sanitize inputs
        yoyoStart = Math.Clamp(yoyoStart, 0.0f, 1.0f);
        delimiterViews = Math.Clamp(delimiterViews, 0.0f, 1.0f);

        if (availableViews <= 1 || contentViews == 1)
        {
            for (int j = 0; j < availableViews; j++)
                indexmap[j] = 0;
            return;
        }

        if (yoyoStart < 1.0f)
        {
            int nonInvertedCount = (int)(availableViews * yoyoStart);
            int[] indexmapNonInvertedPart = new int[nonInvertedCount];
            int[] indexmapInvertedPart = new int[availableViews - nonInvertedCount];

            generateAutomaticDistribution(
                ref indexmapNonInvertedPart,
                nonInvertedCount,
                contentViews,
                0.0f,
                false
            );
            generateAutomaticDistribution(
                ref indexmapInvertedPart,
                availableViews - nonInvertedCount,
                contentViews,
                0.0f,
                true
            );
            Array.Copy(indexmapNonInvertedPart, 0, indexmap, 0, nonInvertedCount);
            Array.Copy(
                indexmapInvertedPart,
                0,
                indexmap,
                nonInvertedCount,
                availableViews - nonInvertedCount
            );
        }
        else
        {
            generateAutomaticDistribution(
                ref indexmap,
                availableViews,
                contentViews,
                delimiterViews,
                invert
            );
        }

        if (invert)
        {
            int x = 0;
            int y = availableViews - 1;
            int tmp;
            while (x < y)
            {
                tmp = indexmap[x];
                indexmap[x] = indexmap[y];
                indexmap[y] = tmp;
                x++;
                y--;
            }
        }

        if (invertIndices)
        {
            for (int i = 0; i < indexmap.Length; i++)
            {
                if (indexmap[i] != BLANK_VIEW)
                {
                    indexmap[i] = contentViews - 1 - indexmap[i];
                }
            }
        }
    }

    private void generateAutomaticDistribution(
        ref int[] indexmap,
        int availableViews,
        int contentViews,
        float delimiterViews,
        bool invert
    )
    {
        if (indexmap.Length == 0)
            return;

        int i;
        // automatic distribution
        if (contentViews == 2)
        {
            i = generateTwoContentViews(ref indexmap, availableViews, delimiterViews);
        }
        else
        {
            i = generateMoreThanTwoContentViews(
                ref indexmap,
                availableViews,
                contentViews,
                delimiterViews
            );
        }
        for (int j = i; j < availableViews; j++)
            indexmap[j] = 0;

        if (invert)
        {
            int x = 0;
            int y = availableViews - 1;
            int tmp;
            while (x < y)
            {
                tmp = indexmap[x];
                indexmap[x] = indexmap[y];
                indexmap[y] = tmp;
                x++;
                y--;
            }
        }

        if (delimiterViews == 0.0f)
        {
            if (indexmap[0] == BLANK_VIEW)
                indexmap[0] = 0;
            for (int x = 1; x < availableViews; x++)
            {
                if (indexmap[x] == BLANK_VIEW)
                    indexmap[x] = indexmap[x - 1];
            }
        }
    }

    private int generateTwoContentViews(
        ref int[] indexmap,
        int availableViews,
        float delimiterViews
    )
    {
        int i = 0;
        float eyeAreaSpace = (1.0f - delimiterViews) / 2;

        int delimViews = (int)(availableViews * delimiterViews);
        int eyeViews = (int)(availableViews * eyeAreaSpace);
        int startDelimViews = (availableViews - delimViews - eyeViews * 2) / 2;
        int endDelimViews = availableViews - delimViews - eyeViews * 2 - startDelimViews;

        for (; startDelimViews > 0; startDelimViews--)
            indexmap[i++] = BLANK_VIEW;
        for (int j = 0; j < eyeViews; j++)
            indexmap[i++] = 1;
        for (; delimViews > 0; delimViews--)
            indexmap[i++] = BLANK_VIEW;
        for (int j = 0; j < eyeViews; j++)
            indexmap[i++] = 0;
        for (; endDelimViews > 0; endDelimViews--)
            indexmap[i++] = BLANK_VIEW;

        return i;
    }

    private int generateMoreThanTwoContentViews(
        ref int[] indexmap,
        int availableViews,
        int contentViews,
        float delimiterViews
    )
    {
        int i = 0;
        int delimViewsStart = (int)(availableViews * delimiterViews / 2);
        int delimViewsEnd = (int)(availableViews * delimiterViews / 2);
        int spacePerView = (availableViews - delimViewsStart - delimViewsEnd) / contentViews;
        int spaceLeftover =
            availableViews - delimViewsStart - delimViewsEnd - spacePerView * contentViews;

        for (; delimViewsStart > 0; delimViewsStart--)
            indexmap[i++] = BLANK_VIEW;
        for (int viewIndex = 0; viewIndex < contentViews; viewIndex++)
        {
            int offset = 0;
            if (spaceLeftover > 0)
            {
                offset = 1;
                spaceLeftover--;
            }
            for (int currentSpace = spacePerView + offset; currentSpace > 0; currentSpace--)
            {
                indexmap[i++] = (int)(contentViews - 1 - viewIndex);
            }
        }
        for (; delimViewsEnd > 0; delimViewsEnd--)
            indexmap[i++] = BLANK_VIEW;

        return i;
    }

    /// <summary>
    /// Updates the internal index map stored in "currentMap".
    /// Also returns the generated index map.
    /// Creates a new index map and adds it to the internal dictionary with the given key.
    /// DANGEROUS: if the key already exists, the existing map will not be overwritten
    /// if the parameter overwrite is false!
    /// </summary>
    /// <param name="key">key to the internal map.</param>
    /// <param name="availableViews">number of available views on the display. has to be larger than 0</param>
    /// <param name="contentViews">number of views your content has. has to be larger than 0</param>
    /// <param name="yoyoStart">set the percentage where the inversion will start (e.g. 0, 1,2,3,2,1,0); will be clamped between 0.0 and 1.0</param>
    /// <param name="invert">if true, the final index map will be inverted</param>
    /// <param name="overwrite">if true, the internal map in the internal dictionary will be overwritten</param>
    /// <returns></returns>
    public int[] UpdateIndexMapFromKey(
        string key,
        int availableViews,
        int contentViews,
        float yoyoStart,
        bool invert,
        bool invertIndices = false,
        bool overwrite = false
    )
    {
        if (maps.ContainsKey(key) && !overwrite)
        {
            // TODO: also check for parameters
            // key found, return the loaded map
            currentMap = maps[key];
        }
        else
        {
            // key not found -> generate a map and return a pointer to that one
            int[] mapArray = GetIndexMapTemp(
                availableViews,
                contentViews,
                yoyoStart,
                invert,
                invertIndices
            );
            maps[key] = mapArray;
            currentMap = mapArray;
        }
        return currentMap;
    }

    /// <summary>
    /// Updates the internal index map stored in "currentMap".
    /// Also returns the generated index map.
    /// </summary>
    /// <param name="availableViews">number of available views on the display. has to be larger than 0</param>
    /// <param name="contentViews">number of views your content has. has to be larger than 0</param>
    /// <param name="yoyoStart">set the percentage where the inversion will start (e.g. 0, 1,2,3,2,1,0); will be clamped between 0.0 and 1.0</param>
    /// <param name="invert">if true, the final index map will be inverted</param>
    /// <param name="overwrite">if true, the internal map in the internal dictionary will be overwritten</param>
    /// <returns></returns>
    public int[] UpdateIndexMap(
        int availableViews,
        int contentViews,
        float yoyoStart,
        bool invert,
        bool invertIndices = false
    )
    {
        int[] mapArray = GetIndexMapTemp(
            availableViews,
            contentViews,
            yoyoStart,
            invert,
            invertIndices
        );
        currentMap = mapArray;
        return currentMap;
    }

    /// <summary>
    /// Creates a new index map without storing it in the internal dictionary.
    /// </summary>
    /// <param name="availableViews">number of available views on the display. has to be larger than 0</param>
    /// <param name="contentViews">number of views your content has. has to be larger than 0</param>
    /// <param name="yoyoStart">set the percentage where the inversion will start (e.g. 0, 1,2,3,2,1,0); will be clamped between 0.0 and 1.0</param>
    /// <param name="invert">if true, the final index map will be inverted</param>
    /// <returns></returns>
    public int[] GetIndexMapTemp(
        int availableViews,
        int contentViews,
        float yoyoStart,
        bool invert,
        bool invertIndices = false
    )
    {
        int[] mapArray = new int[availableViews];
        generate_indexmap(
            ref mapArray,
            availableViews,
            contentViews,
            0,
            yoyoStart,
            invert,
            invertIndices
        );
        return mapArray;
    }

    public float[] convertToFloatArray(in int[] intArray)
    {
        float[] floatArray = new float[intArray.Length];
        for (int i = 0; i < intArray.Length; i++)
        {
            floatArray[i] = intArray[i];
        }
        return floatArray;
    }

    public float[] currentMapAsFloatArray()
    {
        return convertToFloatArray(currentMap);
    }

    public float[] getPaddedIndexMapArray()
    {
        float[] paddedArray = new float[256];
        float[] indexMapArray = currentMapAsFloatArray();
        for (int i = 0; i < paddedArray.Length; i++)
        {
            if (i < indexMapArray.Length)
            {
                paddedArray[i] = indexMapArray[i];
            }
            else
            {
                paddedArray[i] = 0.0f;
            }
        }
        return paddedArray;
    }
}
