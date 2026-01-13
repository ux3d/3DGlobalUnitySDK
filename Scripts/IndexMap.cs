using System;
using System.Collections.Generic;

public class IndexMap
{
    private int BLANK_VIEW = 250; // corresponds to a black view
    public Dictionary<string, byte[]> maps;
    List<int> generatedMap;

    public void generateDefaultMaps()
    {
        maps = new Dictionary<string, List<int>>();

        string name = "A";
        int[] mapArray = new int[35];
        generate_indexmap(ref mapArray, mapArray.Length, 2, 0.0f, 1.0f, false);
        maps[name] = List.FromArray(mapArray);

        name = "B";
        mapArray = new int[35];
        generate_indexmap(ref mapArray, mapArray.Length, 6, 0.0f, 1.0f, false);
        maps[name] = List.FromArray(mapArray);

        name = "C";
        mapArray = new int[35];
        generate_indexmap(ref mapArray, mapArray.Length, 8, 0.0f, 1.0f, false);
        maps[name] = List.FromArray(mapArray);

        name = "D";
        mapArray = new int[35];
        generate_indexmap(ref mapArray, mapArray.Length, 9, 0.0f, 1.0f, false);
        maps[name] = List.FromArray(mapArray);

        name = "E";
        mapArray = new int[35];
        generate_indexmap(ref mapArray, mapArray.Length, 16, 0.0f, 1.0f, false);
        maps[name] = List.FromArray(mapArray);

        name = "S1D";
        maps.Add(
            name,
            new List<int>
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
            new List<int>
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
            new List<int>
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
            new List<int>
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
            new List<int>
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
        maps.Add(name, new List<int> { 0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4 });

        name = "05C";
        maps.Add(name, new List<int> { 0, 1, 2, 3, 4 });

        name = "05N";
        maps.Add(name, new List<int> { 0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4 });

        name = "05O";
        maps.Add(name, new List<int> { 0, 1, 2, 3, 4 });

        name = "05P";
        maps.Add(name, new List<int> { 0, 1, 2, 3, 4 });

        name = "05Q";
        maps.Add(
            name,
            new List<int>
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
                4
            }
        );

        name = "05R";
        maps.Add(name, new List<int> { 0, 1, 2, 3, 4 });

        name = "07A";
        maps.Add(
            name,
            new List<int> { 0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5, 5, 5, 6, 6, 6 }
        );

        name = "07D";
        maps.Add(
            name,
            new List<int>
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
            new List<int> { 0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5, 5, 5, 6, 6, 6, 7, 7, 7 }
        );

        name = "08B";
        maps.Add(name, new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 });

        name = "08D";
        maps.Add(
            name,
            new List<int>
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
        maps.Add(name, new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 });

        name = "08Q";
        maps.Add(
            name,
            new List<int>
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
            new List<int>
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
            new List<int> { 0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5, 5, 5, 6, 6, 6, 7, 7, 7 }
        );

        name = "A8B";
        maps.Add(name, new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 });

        name = "A8O";
        maps.Add(name, new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 });

        name = "IPRO_CUS01";
        maps.Add(
            name,
            new List<int>
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

    public void generate_indexmap(
        ref int[] indexmap,
        int available_views,
        int content_views,
        float delimiter_views,
        float invert_start,
        bool invert
    )
    {
        if (available_views <= 1 || content_views == 1)
        {
            for (int j = 0; j < available_views; j++)
                indexmap[j] = 0;
            return;
        }

        int i = 0;
        if (invert_start < 1.0f)
        {
            int nonInvertedCount = (int)(available_views * invert_start);
            int[] indexmapNonInvertedPart = new int[nonInvertedCount];
            int[] indexmapInvertedPart = new int[available_views - nonInvertedCount];

            generate_indexmap(
                ref indexmapNonInvertedPart,
                nonInvertedCount,
                content_views,
                0.0f,
                1.0f,
                false
            );
            generate_indexmap(
                ref indexmapInvertedPart,
                available_views - nonInvertedCount,
                content_views,
                0.0f,
                1.0f,
                true
            );
            Array.Copy(indexmapNonInvertedPart, 0, indexmap, 0, nonInvertedCount);
            Array.Copy(
                indexmapInvertedPart,
                0,
                indexmap,
                nonInvertedCount,
                available_views - nonInvertedCount
            );
        }
        else
        {
            // automatic distribution
            if (content_views == 2)
            {
                float eyeAreaSpace = (1.0f - delimiter_views) / 2;

                int delimViews = (int)(available_views * delimiter_views);
                int eyeViews = (int)(available_views * eyeAreaSpace);
                int startDelimViews = (available_views - delimViews - eyeViews * 2) / 2;
                int endDelimViews = available_views - delimViews - eyeViews * 2 - startDelimViews;

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
            }
            else
            {
                int delimViewsStart = (int)(available_views * delimiter_views / 2);
                int delimViewsEnd = (int)(available_views * delimiter_views / 2);
                int spacePerView =
                    (available_views - delimViewsStart - delimViewsEnd) / content_views;
                int spaceLeftover =
                    available_views
                    - delimViewsStart
                    - delimViewsEnd
                    - spacePerView * content_views;

                for (; delimViewsStart > 0; delimViewsStart--)
                    indexmap[i++] = BLANK_VIEW;
                for (int viewIndex = 0; viewIndex < content_views; viewIndex++)
                {
                    int offset = 0;
                    if (spaceLeftover > 0)
                    {
                        offset = 1;
                        spaceLeftover--;
                    }
                    for (int currentSpace = spacePerView + offset; currentSpace > 0; currentSpace--)
                    {
                        indexmap[i++] = content_views - 1 - viewIndex;
                    }
                }
                for (; delimViewsEnd > 0; delimViewsEnd--)
                    indexmap[i++] = BLANK_VIEW;
            }
            for (int j = i; j < available_views; j++)
                indexmap[j] = 0;

            if (invert)
            {
                int x = 0;
                int y = available_views - 1;
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

            if (delimiter_views == 0.0f)
            {
                if (indexmap[0] == BLANK_VIEW)
                    indexmap[0] = 0;
                for (int x = 1; x < available_views; x++)
                {
                    if (indexmap[x] == BLANK_VIEW)
                        indexmap[x] = indexmap[x - 1];
                }
            }
        }
    }
}
