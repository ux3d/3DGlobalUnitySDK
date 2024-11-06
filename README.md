# 3DGlobalUnitySDK

This repository provides a Unity SDK for 3D Global autostereo displays.

## Basic function

In general it renders two or more camera views per frame and combines them into one final image. The final image is then displayed on the 3D Global autostereo display.

## How to use

### Installation
1. Either clone the repository to your local machine and import it in Unity using the Package Manager or use the Package Manager to load the package directly from the github repository.
2. If you use the standard render pipeline you are now done. If you use either the URP or HDRP you need to set a scripting define symbol.
    - Go to `Edit -> Project Settings -> Player`
    - In the `Scripting Define Symbols` field add `URP` for URP or `HDRP` for HDRP.
3. Add the "G3DCamera" script to your main camera.
4. Set the calibration and config path to the folder containing your calibration and config files.
5. Set the config file name to match the config file you want to use.
6. You are done with the installation. Next you have to match your scene to the autostereo display.

### Basic usage
The plugin is written wo work with real life units. i.e. the eye separation is in meters and should correspond to the real life eye separation (roughly 6 centimeter). In addition the cameras field of view should match the real life field of view of the autostereo display. For a 24 inch display viewed at the otimal viewing distance of 70 centimeters this is roughly 13 degrees (when the display is in landscape orientation). 