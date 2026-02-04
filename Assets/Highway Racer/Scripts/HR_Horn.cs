//----------------------------------------------
//           	   Highway Racer
//
// Copyright © 2014 - 2021 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HR_Horn : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {

    bool isPressing = false;

    void OnEnable(){

        if (RCC_Settings.Instance.controllerType != RCC_Settings.ControllerType.Mobile) {

            gameObject.SetActive(false);
            return;

        }
        
    }

    void Update(){

        if (isPressing)
            RCC_SceneManager.Instance.activePlayerVehicle.highBeamHeadLightsOn = true;
        else
            RCC_SceneManager.Instance.activePlayerVehicle.highBeamHeadLightsOn = false;
        
    }

    public void OnPointerDown(PointerEventData eventData) {

        isPressing = true;

    }

    public void OnPointerUp(PointerEventData eventData) {

        isPressing = false;

    }

}
