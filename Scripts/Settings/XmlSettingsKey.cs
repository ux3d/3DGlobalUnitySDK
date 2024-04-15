using System;
using UnityEngine;

public enum XmlSettingsKey {
    NULL,
    
    CAMERACOUNT,
    VIEWSHIFT,
    TESTVIEWS,
    TESTCOLORS,
    RESOLUTION,
    EYEDISTANCE,
    STEREODELIMITERSPACE,
    STEREOEYEAREASPACE,
    MODE_VIEWMAP,
    STEREODEPTH,
    STEREOPLANE,
    UNUSED2,
    CAMPOSITIONCIRCULAR,
    CIRCLE_ANGLE,
    CIRCLE_DISTANCE,
    UNUSED,
    STEREO_ZONE_DISTANCE,
    STEREO_ZONE_WIDTH,
    INVERT_HEADTRACKING,
    ALGO_ANGLE_COUNTER,
    ALGO_ANGLE_DENOMINATOR,
    ALGO_HQVIEWS,
    ALGO_DIRECTION,
    BLUR_FACTOR
}

public static class XMLSettingsKeyExtension {
    public static XmlSettingsKey AsXMLSettingsKey(this string s) {
        try {
            return (XmlSettingsKey)Enum.Parse(typeof(XmlSettingsKey), s);
        } catch (Exception ex) {
            Debug.Log(ex.Message);
            return XmlSettingsKey.NULL;
        }
    }
}