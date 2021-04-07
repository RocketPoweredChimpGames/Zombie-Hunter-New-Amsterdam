using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillTheBomb : MonoBehaviour
{
    private ParticleSystem ps;

    // Start is called before the first frame update
    void Start()
    {
        float time = GetComponent<ParticleSystem>().duration;

        Destroy(gameObject, GetComponent<ParticleSystem>().duration);
    }

    // Update is called once per frame
    void Update()
    {
        // needed as bomb particle would never die!
        if (ps)
        {
            if (!ps.IsAlive())
            {
                Destroy(gameObject);
            }
        }
    }
}
