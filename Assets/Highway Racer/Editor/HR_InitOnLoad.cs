//----------------------------------------------
//           	   Highway Racer
//
// Copyright © 2014 - 2021 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------

using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class HR_InitOnLoad {
	
	static HR_InitOnLoad(){
		
		if(!EditorPrefs.HasKey(HR_Version.version)){
			
			EditorPrefs.SetInt(HR_Version.version, 1);
			EditorUtility.DisplayDialog("Regards from BoneCracker Games", "Thank you for purchasing Highway Racer Complete Project. Please read the documentation before use. Also check out the online documentation for updated info. Have fun :)", "Let's get started");
			EditorUtility.DisplayDialog("Current Controller Type", "Current controller type is ''Mobile''. You can swith it from Highway Racer --> Switch to Keyboard / Mobile.", "Ok");
			Selection.activeObject = HR_HighwayRacerProperties.Instance;

		}

	}

}