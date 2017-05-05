using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationSoundPlayer : MonoBehaviour {

    private AudioSource _audioFlap;

	// Use this for initialization
	void Start () {
        _audioFlap = GameObject.Find("AudioSourceFlap").GetComponent<AudioSource>();
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    void OnAnimationFlapStart()
    {
        _audioFlap.Play();
    }
}
