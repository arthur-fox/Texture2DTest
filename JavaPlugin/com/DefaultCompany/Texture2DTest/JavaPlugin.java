package com.DefaultCompany.Texture2DTest;
 
import com.unity3d.player.UnityPlayerActivity;
import java.io.File;
import java.nio.ByteBuffer;

import javax.microedition.khronos.egl.EGLConfig;
import javax.microedition.khronos.opengles.GL10;

import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.opengl.GLES20;
import android.opengl.GLSurfaceView;
import android.opengl.GLUtils;
import android.os.Bundle;
import android.os.Environment;
import android.util.Log;

public class JavaPlugin extends UnityPlayerActivity
{
	// --- Set up for an OpenGL context ---
	public class MyGLRenderer implements GLSurfaceView.Renderer 
	{
		@Override
	    public void onSurfaceCreated(GL10 unused, EGLConfig config) 
	    {
	        // Set the background frame color
	        GLES20.glClearColor(0.0f, 0.0f, 0.0f, 1.0f);
	    }

		@Override
	    public void onDrawFrame(GL10 unused) 
	    {
	        // Redraw background color
	        GLES20.glClear(GLES20.GL_COLOR_BUFFER_BIT);
	    }

		@Override
	    public void onSurfaceChanged(GL10 unused, int width, int height) 
	    {
	        GLES20.glViewport(0, 0, width, height);
	    }
	}
	
	class MyGLSurfaceView extends GLSurfaceView 
	{
	    private final MyGLRenderer m_Renderer;

	    public MyGLSurfaceView(Context context)
	    {
	        super(context);

	        // Create an OpenGL ES 2.0 context
	        setEGLContextClientVersion(2);

	        m_Renderer = new MyGLRenderer();

	        // Set the Renderer for drawing on the GLSurfaceView
	        setRenderer(m_Renderer);
	        
	        // Render the view only when there is a change in the drawing data
	        //setRenderMode(GLSurfaceView.RENDERMODE_WHEN_DIRTY);
	    }
	}
	// --- Set up for an OpenGL context ---
	
	
    private static final String TAG = "JavaPlugin";    
    private GLSurfaceView m_GLView;
    
    @Override
    protected void onCreate(Bundle myBundle) 
    {
        super.onCreate(myBundle);
        
        // Create a GLSurfaceView instance and set it
        // as the ContentView for this Activity.
        m_GLView = new MyGLSurfaceView(this);
        setContentView(m_GLView);
    }
    
    @Override
    protected void onResume() 
    {    	
    	Log.d(TAG, "onResume");        
        
        super.onResume();
    }
    
    @Override
    protected void onPause()
    {
        super.onPause();
    }
    
    @Override
    protected void onStop() 
    {
    	Log.d(TAG, "onStop");
    	
        super.onStop();
    }
    
    public int LoadImageReturnTexturePtr(String imagePath) 
    {
    	//TODO: Get glGenTextures() returning an address!!
    	
    	// No logging seems to work from plugins =/    	
    	Log.d(TAG, "LoadImageReturnTexturePtr() called with imagePath: " + imagePath);
    	Log.v(TAG, "LoadImageReturnTexturePtr() called with imagePath: " + imagePath);
    	Log.i(TAG, "LoadImageReturnTexturePtr() called with imagePath: " + imagePath);
    	Log.wtf(TAG, "LoadImageReturnTexturePtr() called with imagePath: " + imagePath);

    	Bitmap bitmap = BitmapFactory.decodeFile(imagePath);
    	Log.d(TAG, "Bitmap is: " + bitmap);	

    	ByteBuffer buffer = ByteBuffer.allocate(bitmap.getByteCount());
    	bitmap.copyPixelsToBuffer(buffer);

    	int textures[] = new int[1];
    	GLES20.glGenTextures(1, textures, 0);
    	int textureId = textures[0]; 	

    	GLES20.glActiveTexture(GLES20.GL_TEXTURE0);
    	GLES20.glBindTexture(GLES20.GL_TEXTURE_2D, textureId);
    	
    	//GLES20.glTexImage2D(GLES20.GL_TEXTURE_2D, 0, GLES20.GL_RGBA, 1920, 1080, 0, GLES20.GL_RGBA, GLES20.GL_UNSIGNED_BYTE, buffer);
    	GLUtils.texImage2D(GLES20.GL_TEXTURE_2D, 0, bitmap, 0);
    	
    	GLES20.glTexParameteri(GLES20.GL_TEXTURE_2D, GLES20.GL_TEXTURE_MIN_FILTER, GLES20.GL_LINEAR);
    	GLES20.glTexParameteri(GLES20.GL_TEXTURE_2D, GLES20.GL_TEXTURE_MAG_FILTER, GLES20.GL_LINEAR);
    	GLES20.glTexParameteri(GLES20.GL_TEXTURE_2D, GLES20.GL_TEXTURE_WRAP_S, GLES20.GL_CLAMP_TO_EDGE);
    	GLES20.glTexParameteri(GLES20.GL_TEXTURE_2D, GLES20.GL_TEXTURE_WRAP_T, GLES20.GL_CLAMP_TO_EDGE);
    	
    	GLES20.glBindTexture(GLES20.GL_TEXTURE_2D, 0);
    	Log.d(TAG, "Texture id returned: " + textureId);

    	return textureId;
    } 
    
    public static String GetAndroidImagesPath()
    {
    	String path = Environment.getExternalStoragePublicDirectory(
    					Environment.DIRECTORY_DCIM).getAbsolutePath();
    	return path;
    }
    
    public static float CalcAspectRatio(String path)
    {   
		BitmapFactory.Options options = new BitmapFactory.Options();
		options.inJustDecodeBounds = true;
		BitmapFactory.decodeFile(new File(path).getAbsolutePath(), options);
		int imageWidth = options.outWidth;
		int imageHeight = options.outHeight;		

		/*
		path = String.format("{0}:{1}", 
				imageWidth/GCD(imageWidth,imageHeight), 
				imageHeight/GCD(imageWidth,imageHeight));
		Log.d(TAG, path);
		*/
        return imageWidth/ (float) imageHeight; // casting to float in order to ensure float output
    }
    
    /*
    private static int GCD(int a, int b) // greatest common divisor - currently unused
    {
	    int remainder;
	
	    while( b != 0 )
	    {
	    	remainder = a % b;
	        a = b;
	        b = remainder;
	    }
	
	    return a;
    }
    */
}