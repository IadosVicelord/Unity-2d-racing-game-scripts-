using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;
using UnityEngine.SceneManagement;
using Assets.Scripts;

public class ForwardHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public GameObject forwardButton;
    public GameObject backButton;

    public Button restartButton;

    public GameObject controller;
    LineDraw controllerScript;
    JointMotor2D joint;

    Dictionary<Vector2, GameObject> wheels;

    bool isForwardDown = false;
    bool isBackDown = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.pointerEnter == forwardButton)
        {
            joint.motorSpeed = 600;
            isForwardDown = true;
        }
        if (eventData.pointerEnter == backButton)
        {
            joint.motorSpeed = -400;
            isBackDown = true;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isForwardDown = false;
        isBackDown = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        joint = new JointMotor2D();
        joint.motorSpeed = 600;
        joint.maxMotorTorque = 1000;
        controllerScript = controller.GetComponent<LineDraw>();
        wheels = controllerScript.AdditionalObjects;

        restartButton.onClick.AddListener(() => RestartScene());
    }

    void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (isForwardDown)
        {
            if (joint.motorSpeed < 1000)
                joint.motorSpeed += 2;
            if (controllerScript.AdditionalObjects.Count != 0 && controllerScript.IsRaceStarted)
                foreach (var w in wheels.Values)
                {
                    w.GetComponent<WheelJoint2D>().useMotor = true;
                    w.GetComponent<WheelJoint2D>().motor = joint;
                }
        }
        else 
        if (isBackDown)
        {
            if (joint.motorSpeed > -500)
                joint.motorSpeed -= 2;
            if (controllerScript.AdditionalObjects.Count != 0 && controllerScript.IsRaceStarted)
                foreach (var w in wheels.Values)
                {
                    w.GetComponent<WheelJoint2D>().useMotor = true;
                    w.GetComponent<WheelJoint2D>().motor = joint;
                }
        }
        else
        {
            if (controllerScript.AdditionalObjects.Values.Count != 0 && controllerScript.IsRaceStarted)
            {
                if (controllerScript.AdditionalObjects.First().Value.GetComponent<WheelJoint2D>().useMotor != false)
                {
                    foreach (var w in wheels.Values)
                    {
                        joint.motorSpeed = 0;
                        w.GetComponent<WheelJoint2D>().motor = joint;
                        w.GetComponent<WheelJoint2D>().useMotor = false;
                    }
                }
            }
        }
    }
}
