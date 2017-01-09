# Texture2DTest
Test repo demonstrating synchronous block on the main thread in Unity, when constructing a large Texture2D for a 360 image.

How it works:
 - There is a single scene called Texture2DTest with two Buttons, and an inside-out sphere for loading and setting the 360-image onto.
 - Running the scene and clicking on the text of either button will call a function. For the “S3” button the function will be within the AWSS3Client.cs script, and for the “Device” button it will call to the DeviceGallery.cs script.
 - Both of these scripts essentially grab the data for a Texture2D from somewhere (either an S3 bucket, or a hardcoded path on the Android device respectively), then ask the sphere to construct the Texture2D and set its texture through the “SelectImage” script.
 - A counter on the top left shows the frame-rate, however you can also set the CameraController::RightRate to a non-zero value to begin rotating the Main Camera (which servers no purpose other than to make it incredibly obvious when the frame-out occurs).
 - In order to experience the block again, you’ll need to press the appropriate button again.


Note:
 - The project will only build and run correctly on an Android Device (not in the Editor), because we’re making use of a C++ plugin built for Android to try and solve the problem.
 - Download the free open-source software Blender in order for the the project to build correctly: https://www.blender.org/download/
 - In order to have the “Device” button work correctly you will need to build an Android version of the project and point the hardcoded path to one that exists on your device (search for “HARDCODED” in “DeviceGallery.cs” to replace the path).


C++ Plugin:
 - To edit the C++ code first open the CppPlugin folder with Android Studio 2.2, edit the "androidcppnative.cpp" file in the IDE, and then hit the build button at the top. This will create a few shared object files that live in "/Texture2DTest/CppPlugin/app/build/intermediates/cmake/debug/obj”, you can then just copy over the appropriate "libandroidcppnative.so" into the correct folder within "/Texture2DTest/Assets/Plugins/Android/libs".


Java Plugin:
 - To Edit the Java Plugin you will need to setup an Eclipse project with the following steps:
(1) Open Eclipse and select File > New > Other > Java Project from Existing Ant Buildfile, then point it to JavaPlugin/Build.xml 
(2) In the new Eclipse project you'll need to link against Unity's "classes.jar" file in Properties > Java Build Path > Libraries 
(3) If the file for any reason has not been imported, then just drag and drop JavaPlugin.java into the Project, such that it lives in a package called "com.DefaultCompany.Texture2DTest" 
(4) When building select Run > External Tools > External Tools Configuration, then select New Launch Configuration, and point the Buildfile to JavaPlugin/Build.xml, as well as selecting "build-jar" and "clean-post-jar" in the Targets tab. 
(5) Finally, the build must be against JRE 1.7 for Unity to parse it correctly (for whatever reason) so you need the following: 
- (A) Build.xml needs to have the parameter “target=1.7” (should already be set) 
- (B) Preferences > Java Compiler needs to be set to 1.7 
- (C) Run > External Tools > External Tools Configuration > JRE needs to be set to 1.7 
(6) Build and Run, and place the outputted JavaPlugin.jar inside the folder Assets/Plugins/Android/
