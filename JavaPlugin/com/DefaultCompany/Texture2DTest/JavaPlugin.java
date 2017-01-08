package com.DefaultCompany.Texture2DTest;
 
import com.unity3d.player.UnityPlayerActivity;
import java.io.File;
import android.graphics.BitmapFactory;
import android.os.Bundle;
import android.os.Environment;
//import android.content.Context;
import android.util.Log;
public class JavaPlugin extends UnityPlayerActivity
{
    private static final String TAG = "JavaPlugin";    
 
    @Override
    protected void onCreate(Bundle myBundle) 
    {
        super.onCreate(myBundle);
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