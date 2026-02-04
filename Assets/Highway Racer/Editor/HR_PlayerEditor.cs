using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(HR_PlayerHandler))]
public class HR_PlayerEditor : Editor{

	HR_PlayerHandler prop;

	public override void OnInspectorGUI() {

        serializedObject.Update();
		prop = (HR_PlayerHandler)target;

		DrawDefaultInspector();

		if (PrefabUtility.GetCorrespondingObjectFromSource(prop.gameObject) == null) {

			if (GUILayout.Button("Create Prefab"))
				CreatePrefab();

		} else {

			if (GUILayout.Button("Save Prefab"))
				SavePrefab();

		}

		bool foundPrefab = false;

        for (int i = 0; i < HR_PlayerCars.Instance.cars.Length; i++) {

			if (HR_PlayerCars.Instance.cars[i].playerCar != null) {

				if (prop.transform.name == HR_PlayerCars.Instance.cars[i].playerCar.transform.name) {

					foundPrefab = true;
					break;

				}

			}

        }

		if (!foundPrefab) {

			if (GUILayout.Button("Add Prefab To Player Vehicles List")) {

				if (PrefabUtility.GetCorrespondingObjectFromSource(prop.gameObject) == null) 
					CreatePrefab();
				 else 
					SavePrefab();

				AddToList();

			}

		}

		serializedObject.ApplyModifiedProperties();

    }

	void CreatePrefab() {

		PrefabUtility.SaveAsPrefabAssetAndConnect(prop.gameObject, "Assets/Highway Racer/Resources/Player Vehicles/" + prop.gameObject.name + ".prefab", InteractionMode.UserAction);
		Debug.Log("Created Prefab");

	}

	void SavePrefab() {

		PrefabUtility.SaveAsPrefabAssetAndConnect(prop.gameObject, "Assets/Highway Racer/Resources/Player Vehicles/" + prop.gameObject.name + ".prefab", InteractionMode.UserAction);
		Debug.Log("Saved Prefab");

	}

	void AddToList() {

		List<HR_PlayerCars.Cars> playerCars = new List<HR_PlayerCars.Cars>();

		playerCars.Clear();
		playerCars.AddRange(HR_PlayerCars.Instance.cars);
		HR_PlayerCars.Cars newCar = new HR_PlayerCars.Cars();
		newCar.vehicleName = "New Player Vehicle " + Random.Range(0, 100).ToString();
		newCar.playerCar = PrefabUtility.GetCorrespondingObjectFromSource(prop.gameObject);
		playerCars.Add(newCar);
		HR_PlayerCars.Instance.cars = playerCars.ToArray();
		PlayerPrefs.SetInt("SelectedPlayerCarIndex", 0);
		Selection.activeObject = HR_PlayerCars.Instance;
		
		Debug.Log("Added Prefab To The Player Vehicles List");

	}

}
