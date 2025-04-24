# 3DGlobalUnitySDK

This repository provides a Unity SDK for 3D Global autostereo displays.

## Basic function

In general it renders two or more camera views per frame and combines them into one final image. The final image is then displayed on the 3D Global autostereo display.

## How to use

### Installation

1. Either clone the repository to your local machine and import it in Unity using the Package Manager or use the Package Manager to load the package directly from the github repository.
2. Add the "G3DCamera" script to your main camera.

### Tips

You can decrease the resolution the individual cameras are rendered at to increase performance.
This will decrease the quality of the autostereo effect but might be necessary on lower end hardware.
Usually you can set it to something like 70 percent or even lower without noticing a big difference in quality.

# Functionality

The plugin renders the scene from newly created cameras (usually one for each eye; except when multiview is enabled) and combines the images into one final image.
The camera the script is attached to is used as the main camera, displays the final image, but does not render anything itself.
The new cameras are created as children of the main camera at runtime.

# Switching renderpiplines

If you switch between render pipelines (e.g. from URP to built-in) you need to ensure the old renderpipeline package is removed from the project. Otherwise this plugin will show wrong behaviour.
Additionally you have to update the "Scripting define symbols" to match your new pipeline. (Project settings -> Player -> Other settings -> Scriüting define symbols)

- URP: "G3D_URP"
- HDRP: "G3D_HDRP"
- Built-in: nothing

# Known issues

If you have more than one render pipeline installed this plugin cant know which one is the active one and will not work correctly. Removing the unused render pipeline package resolves this issue.

Switching mode during playback is currently not supported. You have to stop the playback and start it again to switch between modes.
