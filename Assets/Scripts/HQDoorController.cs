using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HQDoorController : MonoBehaviour
{
    Animator gateAnimator;

    // Start is called before the first frame update
    void Start()
    {
        gateAnimator = GetComponent<Animator>(); // get the animator    
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            //gateAnimator.SetTrigger("HQ Gate Open");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            //gateAnimator.enabled = true;
        }
    }

    void PauseAnimationEvent()
    {
        //gateAnimator.enabled = false;
    }

}
