Automatic Installation:

1) 	place the Setup.prefab in your scene

2) 	set the parameters of the attached script of that object according to your needs
	(please be patient with pipeline switches: recompilation takes a while - progress can be seen on the bottom right)

3)	tick the "on" checkbox

4)	remove the setup object from your scene or disable it

Note: The methods used to install the plugin this way are not entirely officially supported by unity and might break under certain circumstances. 
	  Therefore, instructions for manual installation follow down below



Manual Plugin Installation:

1) 	move the G3D folder in your unity project

2) 	add Scripts/G3DCamera.cs to your main camera in the scene

3) 	add Resources/G3DCameraUi prefab to your scene
		(F1 is the default button to show the UI (change it on the script attached to the gameobject))
		(Enable/Disable the child "Gui" to change the visibility at start)

4) 	Edit > Project Settings > Graphics

	Default Pipeline:
		Add all shaders without URP or HDRP postfix from G3D/Shaders to always included shaders (by increasing size first and then selecting it)
	URP:
		Add all shaders with URP postfix from G3D/Shaders to always included shaders (by increasing size first and then selecting it)
	HDRP:
		Add all shaders with HDRP postfix from G3D/Shaders to always included shaders (by increasing size first and then selecting it)

5)
	Default Pipeline:
		Nothing (The camera script will take care of the rendering)

	URP: 
		5.1)
			go to "Edit > Project Settings > Player > Other Settings > Scripting Define Symbols" and add "URP" 
			(press Apply: recompilation will be started by this)
			("Scripting Define Symbols" is a UI element for lists and contains +/- on the right)

		5.2) 
			add "G3DRendererFeature" to the features of your pipeline.
			The "Universal Rendering Pipeline" will be represented by an asset.
			To find it, go to "Edit > Project Settings > Graphics" and click on the selected pipeline. This should 
			highlight the pipeline in the assets. The pipeline should have a renderer attached (as seen in a list in
			the inspector tab). Clicking on the renderer will highlight the file in the assets. Click on this 
			renderer and add the G3DRendererFeature by pressing the "Add Renderer Feature" button in the inspector tab
			of the renderer.

	HDRP:
		5.1)
			go to "Edit > Project Settings > Player > Other Settings > Scripting Define Symbols" and add "HDRP" 
			(press Apply: recompilation will be started by this)
			("Scripting Define Symbols" is a UI element for lists and contains +/- on the right)
	
		5.2)
			Go to "Edit > Project Settings > Graphics > HDRP Global Settings".
			5.2.1) Click on "Add Override" for the "Volume Profile" and add G3DPostProcessingHDRP.
			5.2.2) Scroll down to the "Custom Post Process Orders" and add G3DPostProcessingHDRP to "After Post Process".
