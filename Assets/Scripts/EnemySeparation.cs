using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySeparation : MonoBehaviour
{
    GameObject[] enemyObjects;
    public float spaceBetween = 1f;

    // Start is called before the first frame update
    void Start()
    {
        //enemyObjects = GameObject.FindGameObjectsWithTag("Enemy Warrior Base Object");    
        enemyObjects = GameObject.FindGameObjectsWithTag("Enemy Warrior");
    }

   public void RemoveDestroyedEnemy(GameObject deadEnemy)
    {
        enemyObjects = GameObject.FindGameObjectsWithTag("Enemy Warrior");
        //enemyObjects = GameObject.FindGameObjectsWithTag("Enemy Warrior Base Object");

        // find and remove a dead enemy from this list before destroying elsewhere
        int i = 0;

        foreach (GameObject go in enemyObjects)
        {
            if (go == deadEnemy)
            {
                enemyObjects[i] = null;
            }
            else
            {
                i++;
            }
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        enemyObjects = GameObject.FindGameObjectsWithTag("Enemy Warrior");
        //enemyObjects = GameObject.FindGameObjectsWithTag("Enemy Warrior Base Object");
        
        // keep the spacing between other enemy objects
        foreach (GameObject go in enemyObjects)
        {
            if (go != gameObject && go !=null)
            {
                float distance = Vector3.Distance(go.transform.position , this.transform.position);
                
                if (distance <= spaceBetween)
                {
                    Vector3 direction = transform.position - go.transform.position;
                    direction.y = 0.0f;
                    transform.Translate(direction * Time.deltaTime);
                }
            }
        }
    }
}
