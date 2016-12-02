# Texture2DTest
Test repo demonstrating synchronous block on the main thread in Unity, when constructing a large Texture2D for a 360 image.

How it works:
 - There is a single scene called Texture2DTest with two Buttons, and an inside-out sphere for loading and setting the 360-image onto.
 - Running the scene and clicking on the text of either button will call a function. For the “S3” button the function will be within the AWSS3Client.cs script, and for the “Device” button it will call to the DeviceGallery.cs script.
 - Both of these scripts essentially grab the data for a Texture2D from somewhere (either an S3 bucket, or a hardcoded path on the Android device respectively), then ask the sphere to construct the Texture2D and set its texture through the “SelectImage” script.
 - Clicking on the button will also start rotating the Main Camera, this servers no purpose other than to make it incredibly obvious when the frame-out occurs - to remove this functionality comment out the CameraController::Update() function.
 - In order to experience the block again, you’ll need to restart the application and press the appropriate button again.


Note:
 - Download the free open-source software Blender in order for the the project to build correctly: https://www.blender.org/download/
 - In order to have the “Device” button work correctly you will need to build an Android version of the project and point the hardcoded path to one that exists on your device (search for “HARDCODED” in “DeviceGallery.cs” to replace the path).
 - The two cases of offending code that construct a Texture2D and hence block the main thread can be found in the “SelectImage” script. Simply search for “BLOCK” and you’ll find them. Stick a breakpoint on both lines, and when it hits you’ll be able to trace back up the call stack to see what the loading code is doing if needed.