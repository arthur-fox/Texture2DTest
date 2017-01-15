# Texture2DTest
Test repo that solves non-blocking loading of large images constructed as Texture2Ds in Unity 3D for Android.

How it works:
 - There is a single scene called Texture2DTest with a single Button for loading and setting the Texture2D on the flat plane.
 - Running the scene and clicking on the “LOAD” text will call a function from the DeviceGallery.cs script.
 - The script grabs the first image it can find on an Android device to create the Texture2D from an Android device, uses the C++ plugin to create the underlying OpenGL texture, then sets it on the flat plane through the “SelectImage” script.
 - A counter on the top left shows the frame-rate, so you can see that it doesn’t really dip during the Loading process.


Note:
 - The project will only build and run correctly on an Android Device (not in the Editor), because we’re making use of a C++ plugin built for Android to solve the problem.
 - Download the free open-source software Blender in order for the the project to build correctly: https://www.blender.org/download/


C++ Plugin:
 - To edit the C++ code first open the CppPlugin folder with Android Studio 2.2, edit the "androidcppnative.cpp" file in the IDE, and then hit the build button at the top. This will create a few shared object files that live in "/Texture2DTest/CppPlugin/app/build/intermediates/cmake/debug/obj”, you can then just copy over the appropriate "libandroidcppnative.so" into the correct folder within "/Texture2DTest/Assets/Plugins/Android/libs".