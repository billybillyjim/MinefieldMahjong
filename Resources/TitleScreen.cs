using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleScreen : MonoBehaviour {

    public GameObject titleScreen;
    private Vector3 startingPos;
	// Use this for initialization
	void Start () {
        startingPos = transform.position;
	}
	
	// Update is called once per frame
	void Update () {
        WobbleTitle();
	}
    private void WobbleTitle()
    {
        transform.position = startingPos + new Vector3(0, Mathf.Sin(Time.time) * .2f, 0);
    }
}
