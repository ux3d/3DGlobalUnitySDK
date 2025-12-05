# 3DGlobalUnitySDK

This repository provides a Unity SDK for 3D Global autostereo displays.

## Functionality

In general it renders two or more camera views per frame and combines them into one final image. The final image is then displayed on the 3D Global autostereo display.

Autostereo displays use lenses to created individual areas infront of the display (views) that are only visible from certain angles. Each of these views only shows a subset of the displays total pixels. As such one can show seperate images per view by only rendering an image to the pixels that are visible from this view.

The plugin renders the scene from newly created cameras (usually one for each eye; except when multiview is enabled) and combines the images into one final image.
The camera the script is attached to is used as the main camera and displays the final image, but does not render anything itself.
The new (autostereo) cameras are created as children of the main camera at runtime.

The created autostereo cameras all shift their FOV such that they overlap on the focus plane. The focus plane is the virtual plane in the scene where the real life monitor is located. Objects on this plane appear at the same depth as the display. Objects in front of this plane appear to pop out of the display and objects behind it appear to be behind the display.

![Annotations](./documentation/annotations.png)

This plugin creates helper gizmos in the scene view to visualize the focus plane and the camera positions. The size of the gizmos can be adjusted in the script parameters. The spheres next to the main camera represent the base position of the autostereo cameras. The blue plane represents the focus plane. The blue view frustums represents the FOV of the autostereo cameras.

When using diorama mode the autostereo cameras follow the same movement pattern relative to the focus plane, as the head performs infront of the display. In addition the FOV of the cameras is adjusted such that they always overlap on the focus plane.

Example for shifted FOV in diorama mode:
| | |
| -------------------------------------------- | -------------------------------------------- |
| ![Annotations](./documentation/diorama2.png) | ![Annotations](./documentation/diorama3.png) |

## How to use

### Installation

1. Either clone the repository to your local machine and import it in Unity using the Package Manager or use the Package Manager to load the package directly from the github repository.
2. Add the "G3DCamera" script to your main camera. Or if you want to display mosaic videos add the "G3DCameraMosaicMultiview" script to your main camera.

# Parameters

| Name                    | Description                                                                       |
| ----------------------- | --------------------------------------------------------------------------------- |
| Mode                    | Switch between Diorama and Multiview modes.                                       |
| Calibration file        | The calibration file used to calibrate multiview mode.                            |
| Scene scale factor      | Scales display calibration to fit larger scenes.                                  |
| Dolly zoom              | Mimics a dolly zoom effect.                                                       |
| View offset scale       | Scales the view disparity (e.g pushes 3d cameras closer together/ further apart). |
| Render resolution scale | Size of the individual autostereo camera render textures in percent.              |
| Generate Views      | If set to true, only the outer most and the middle view will be rendered. The rest is generated based on these.                      |
| Fill holes          | If set to true, small holes in the generated views will be filled.                                                                   |
| Hole filling radius | How far around the missing pixels the algorithm searches for a usable pixel. Larger values result in better results but take longer. |

### Calibration file

This takes an ini file (the same ini files as can be found in the displays calibration folder) that contains the calibration data for the display. This is only needed in multiview mode. In diorama mode the calibration files are read from the default calibration folder of 3D Global. You can still provide a calibration file in diorama mode. It will only be used for drawing the helper gizmos in the scene view. During play it will not be used (in diorama mode).

Unfortunatly for now unity does not support "\*.ini" files as TextAsset. Therefore you have to rename the file extension to "\*.txt" to be able to use it in Unity.

### Scene scale factor

The plugin is build to work in a 1:1 scale with the display. Meaning it expects the focus plane to be of equal distance from the viewer as the display is from the viewer. The Scene scale factor can be used to scale the plugin to fit larger scenes. It scales the camera position, the field of view of the cameras, etc.

### Dolly zoom

Mimics a dolly zoom effect by scaling the camera position and field of view. This is useful to scale the field of view to fit larger scenes without actually scaling the camera.

## Advanced parameters

| Name                | Description                                                                   |
| ------------------- | ----------------------------------------------------------------------------- |
| Mirror views        | Mirrors the individual views horizontally (e.g. needed for Holobox displays). |
| Head tracking scale | Scales the strength of head tracking. (Only shown in diorama mode.)           |
| Show gizmos         | Render helper gizmos in scene.                                                |
| Gizmo size          | Size of the gizmos.                                                           |

### Render resolution scale

100% corresponds to the native resolution of the display. i.e. each camera renders at the native resolution of the display.

This value can be used to increase performance by reducing the resolution of the individual camera render textures. It does not affect the resolution of the final image displayed on the 3D Global autostereo display.

## Modes explained

### Diorama mode

This mode uses a head tracking camera integrated into a 3d Global display to adjust the view based on the viewer's head position. It only works properly if the display has a head tracking camera.

#### Diorama Calibration files

It requires the displays calibration files to be copied to "C:\Users\Public\Documents\3D Global\calibrations" (Windows). Each individual physical display has its own calibration files. These can be found on the display. The calibration for one display is contained in a folder named after the used head tracking camera (e.g "HimaxD2XX#DK0HLRO3"). Inside this folder are two files (one ini file and one image file). Copy this entire folder to the calibration folder mentioned earlier.

### Multiview mode

In this mode the plugin renders multiple views without any head tracking and spreads them equally accross the displays available views. This mode works on all 3D Global displays. This mode only requires general calibration information contained in the calibration.ini file. Therefore you can reuse the same calibration file on multiple displays of the same type.

# Performance

As each view only shows a subset of the displays pixels, you can use the "Render resolution scale" parameter to reduce the resolution of the individual camera render textures. This can significantly increase performance while barely impacting quality.

# View Generation

When this is enabled only the outer most and the middle view are rendered. The other views are generated based on these two views.

## Anti Aliasing

Only FXAA and SMAA work when view generation is enabled. When the cameras anti aliasing is set to TAA no anti aliasing is applied to the rendered views.

## Bloom

Bloom does not work correctly when view generation is enabled. Bloom results in weird halo like effects around objects in the generated views. It is recommended to disable bloom when using view generation.

# Switching render pipelines

If you switch between render pipelines (e.g. from URP to built-in) you need to ensure the old render pipeline package is removed from the project. Otherwise this plugin will show wrong behavior.
Additionally you have to update the "Scripting define symbols" to match your new pipeline. (Project settings -> Player -> Other settings -> Scripting define symbols)

- URP: "G3D_URP"
- HDRP: "G3D_HDRP"
- Built-in: nothing

Reimporting this package also updates the "Scripting define symbols" automatically.

# Known issues

If you have more than one render pipeline installed this plugin cant know which one is the active one and might not work correctly. Removing the unused render pipeline package resolves this issue. Currently the plugin only checks the installed render pipeline after installation. So if you switch the render pipeline you have to remove the plugin and reimport it.

Switching mode during playback (multiview to diorama and vice versa) is currently not supported. You have to stop the playback and start it again to switch between modes.
