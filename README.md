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
The plugin is written wo work with real life units. i.e. the eye separation is in meters and should correspond to the real life eye separation (roughly 6 centimeter).
In addition the cameras field of view should match the real life field of view of the autostereo display.
For a 24 inch display viewed at the optimal viewing distance of 70 centimeters this is roughly 13 degrees (when the display is in landscape orientation).

You can enable helper gizmos in the editor to help you match your scene to the autostereo display.
This adds two blue spheres where the autostereo cameras will be placed and a green sphere showing you where the focus plane is (if it is enabled).

The focus plane is the plane where the autostereo effect is null. Elements on this plane appear neither in front of nor behind the display.
Additionally all camera movements caused by the head tracking are in relation to this plane (and not the main camera the script is attached to).


### Tips
You can decrease the resolution the individual cameras are rendered at to increase performance.
This will decrease the quality of the autostereo effect but might be necessary on lower end hardware.
Usually you can set it to something like 70 percent or even lower without noticing a big difference in quality.

18 seems to be a good value for the head position filter (for each of the three values). Lower values produce snappier results but also more jitter.


# Functionality
The plugin renders the scene from newly created cameras (usually one for each eye; except when multiview is enabled) and combines the images into one final image.
The camera the script is attached to is used as the main camera, displays the final image, but does not render anything itself.
The new cameras are created as children of the main camera at runtime.
