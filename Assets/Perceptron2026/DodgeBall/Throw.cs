using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DodgeBall
{
    public class Throw : MonoBehaviour {

        public GameObject spherePrefab;
        public GameObject cubePrefab;
        public Material green;
        public Material red;

        Perceptron p;

        // Use this for initialization
        void Start () {
            p = GetComponent<Perceptron>();
        }

        // Update is called once per frame
        void Update () {
            // Input System (the legacy Input Manager is disabled in this project)
            Keyboard k = Keyboard.current;
            if(k == null) return;
            if(k.digit1Key.wasPressedThisFrame) ThrowObject(1);
            else if(k.digit2Key.wasPressedThisFrame) ThrowObject(2);
            else if(k.digit3Key.wasPressedThisFrame) ThrowObject(3);
            else if(k.digit4Key.wasPressedThisFrame) ThrowObject(4);
        }

        // 1 red sphere, 2 green sphere, 3 red cube, 4 green cube.
        // Inputs: (shape: 0 sphere / 1 cube, colour: 0 red / 1 green); output 0 = duck.
        public GameObject ThrowObject(int key)
        {
            GameObject g = null;
            if(key == 1)
            {
                g = Instantiate(spherePrefab,Camera.main.transform.position,Camera.main.transform.rotation);
                g.GetComponent<Renderer>().material = red;
                g.GetComponent<Rigidbody>().AddForce(0,0,500);
                p.SendInput(0,0,0);
            }
            else if(key == 2)
            {
                g = Instantiate(spherePrefab,Camera.main.transform.position,Camera.main.transform.rotation);
                g.GetComponent<Renderer>().material = green;
                g.GetComponent<Rigidbody>().AddForce(0,0,500);
                p.SendInput(0,1,1);
            }
            else if(key == 3)
            {
                g = Instantiate(cubePrefab,Camera.main.transform.position,Camera.main.transform.rotation);
                g.GetComponent<Renderer>().material = red;
                g.GetComponent<Rigidbody>().AddForce(0,0,500);
                p.SendInput(1,0,1);
            }
            else if(key == 4)
            {
                g = Instantiate(cubePrefab,Camera.main.transform.position,Camera.main.transform.rotation);
                g.GetComponent<Renderer>().material = green;
                g.GetComponent<Rigidbody>().AddForce(0,0,500);
                p.SendInput(1,1,1);
            }
            return g;
        }
    }
}
