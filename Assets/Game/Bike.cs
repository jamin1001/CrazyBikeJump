using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bike : MonoBehaviour
{
    public ParticleSystem crashParticlesPrefab; // world
    public ParticleSystem windParticlesPrefab;  // local to bike
    public float HittingAtDistance = 0.5f;

    public void StartWind()
    {
        windParticlesPrefab.Play();
        //windParticlesPrefab.gameObject.SetActive(true);
    }

    public void StopWind()
    {
        //windParticlesPrefab.gameObject.SetActive(false);
        windParticlesPrefab.Stop();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.gameObject.name.Contains("cow"))
        {
            Game.Inst.PlayOneShot(Game.Inst.CowHitClip);    
        }

        if (collision.collider is CapsuleCollider)
        {
            Debug.Log("HIT!");// Collided with :" + collision.collider.gameObject);
            crashParticlesPrefab.transform.position = collision.transform.position + new Vector3(0, 1f, -0.5f);
            //crashParticlesPrefab.gameObject.SetActive(true);
            crashParticlesPrefab.Play();
            crashParticlesPrefab.transform.GetChild(1).gameObject.SetActive(true); // Child particle too.
            Game.Inst.PlayOneShot(Game.Inst.BikeCrashClip);
            Game.Inst.BikeStopped();
            Debug.Log("Bike Stopped.");
            StartCoroutine(StopEverything());
        }

    }

    IEnumerator StopEverything()
    {
        Debug.Log("- In Stop Everything.");
        yield return null;
        Debug.Log("- After yield null.");
        Game.Inst.Restart();
        Debug.Log("- After Restart.");

        yield return Game.WaitParticlesStop;
        Debug.Log("- After WaitParticlesStop.");
        //crashParticlesPrefab.Stop();//.gameObject.SetActive(false);
        Debug.Log("- After SetActive(false).");
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.collider is BoxCollider)
        {
            if (!Game.Inst.IsRestarting)
            {
                if (collision.collider.gameObject.name.Contains("swerve"))
                {
                    Game.Inst.PlayOneShot(Game.Inst.BikeSwerveClip);

                    if (collision.collider.gameObject.name.Contains("cow"))
                    {
                        Game.Inst.PlayOneShot(Game.Inst.CowSwerveClip);
                    }
                }
                else
                {
                    Game.Inst.PlayOneShot(Game.Inst.BikeCollectClip);
                }

            }
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
