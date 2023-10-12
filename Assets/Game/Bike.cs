using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bike : MonoBehaviour
{
    public GameObject crashParticlesPrefab;
    public float HittingAtDistance = 0.5f;

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.name.Contains("cow"))
        {
            // do moo
        }

        if (collision.collider is CapsuleCollider)
        {
            Debug.Log("HIT!");// Collided with :" + collision.collider.gameObject);
            crashParticlesPrefab.transform.position = collision.transform.position + new Vector3(0, 1f, -0.5f);
            crashParticlesPrefab.SetActive(true);
            crashParticlesPrefab.transform.GetChild(1).gameObject.SetActive(true); // Child particle too.
            Game.Inst.PlayOneShot(Game.Inst.BikeCrashClip);
            Game.Inst.BikeStopped();
            StartCoroutine(StopEverything());
        }

    }

    IEnumerator StopEverything()
    {
        yield return null;
        Game.Inst.Restart();

        yield return Game.WaitParticlesStop;
        crashParticlesPrefab.SetActive(false);
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.collider is BoxCollider)
        {
            if(!Game.Inst.IsRestarting)
                Game.Inst.PlayOneShot(Game.Inst.BikeCollectClip);
            Debug.Log("JUMPED OVER!");
        }
    }

    void OnCollisionStay(Collision collision)
    {
        /*
        Vector3 diff = collision.collider.gameObject.transform.position - transform.position;
        if(diff.sqrMagnitude < HittingAtDistance * HittingAtDistance)
        {
            Debug.Log("HITTING!");
            Game.Inst.PlaySound(Game.GameSound.BikeCrash);
        }
        */


        //Debug.Log("Stay Collided with :" + collision.collider.gameObject);



    }

}
