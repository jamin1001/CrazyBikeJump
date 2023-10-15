using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bike : MonoBehaviour
{
    public ParticleSystem crashParticlesPrefab; // world
    public ParticleSystem swerveParticlesPrefab; // world
    public ParticleSystem collectParticlesPrefab; // world
    public ParticleSystem windParticlesPrefab;  // local to bike
    public float HittingAtDistance = 0.5f;

    public void StartWind()
    {
        windParticlesPrefab.Play();
    }

    public void StopWind()
    {
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
            crashParticlesPrefab.Play();
            crashParticlesPrefab.transform.GetChild(1).gameObject.SetActive(true); // Child particle too.
            Game.Inst.PlayOneShot(Game.Inst.BikeCrashClip);
            Game.Inst.BikeStopped();
            StartCoroutine(StopEverything());
        }
    }

    IEnumerator StopEverything()
    {
        //Debug.Log("- In Stop Everything.");
        yield return null;
        //Debug.Log("- After yield null.");
        Game.Inst.Restart();
        //Debug.Log("- After Restart.");

        yield return Game.WaitParticlesStop;
        //Debug.Log("- After WaitParticlesStop.");
        //crashParticlesPrefab.Stop();//.gameObject.SetActive(false);
        //Debug.Log("- After SetActive(false).");
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
                    swerveParticlesPrefab.transform.localPosition = transform.localPosition + new Vector3(0, 0.5f, 0); // add a little height
                    swerveParticlesPrefab.Play();

                    if (collision.collider.gameObject.name.Contains("cow"))
                    {
                        Game.Inst.PlayOneShot(Game.Inst.CowSwerveClip);
                    }
                }
                else
                {
                    Game.Inst.PlayOneShot(Game.Inst.BikeCollectClip);
                    collectParticlesPrefab.transform.localPosition = transform.localPosition + new Vector3(0, 0.5f, 0); // add a little height
                    collectParticlesPrefab.Play();
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
