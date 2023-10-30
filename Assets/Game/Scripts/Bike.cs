using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bike : MonoBehaviour
{
    public ParticleSystem CrashParticlesPrefab; // world
    public ParticleSystem SwerveParticlesPrefab; // world
    public ParticleSystem CollectBronzeParticlesPrefab; // world
    public ParticleSystem CollectSilverParticlesPrefab; // world
    public ParticleSystem CollectGoldParticlesPrefab; // world
    public ParticleSystem CollectFlagYellowParticlesPrefab; // world
    public ParticleSystem CollectFlagRedParticlesPrefab; // world
    public ParticleSystem CollectFlagBlueParticlesPrefab; // world

    public ParticleSystem WindParticlesPrefab;  // local to bike
    public float HittingAtDistance = 0.5f;

    public void StartWind()
    {
        WindParticlesPrefab.Play();
    }

    public void StopWind()
    {
        WindParticlesPrefab.Stop();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.gameObject.name.Contains("cow"))
        {
            Game.Inst.PlayOneShot(Game.Inst.CowHitClip);    
        }

        if (collision.collider is CapsuleCollider)
        {
            //Debug.Log("HIT!");// Collided with :" + collision.collider.gameObject);

            // Looks better for a flagpole if the crash explosion is sitting directly in line with the pole.
            float xOffset = 0;
            if (collision.gameObject.tag.Contains("Flag"))
                if (transform.position.x < collision.transform.position.x)
                    xOffset = -1f;
                else
                    xOffset = 1f;

            CrashParticlesPrefab.transform.position = collision.transform.position + new Vector3(xOffset, 1f, -0.5f);
            CrashParticlesPrefab.Play();
            CrashParticlesPrefab.transform.GetChild(1).gameObject.SetActive(true); // Child particle too.
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
        Game.Inst.Restart(false);
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
                    SwerveParticlesPrefab.transform.localPosition = transform.localPosition + new Vector3(0, 0.5f, 0); // add a little height
                    SwerveParticlesPrefab.Play();

                    if (collision.collider.gameObject.name.Contains("cow"))
                    {
                        Game.Inst.PlayOneShot(Game.Inst.CowSwerveClip);
                    }

                    Game.Inst.PlayOnOff(Game.Inst.BikeSailClip, 0.1f);
                    StartWind();
                }
                else
                {
                    Game.Inst.PlayOneShot(Game.Inst.BikeCollectClip);
                    CollectGoldParticlesPrefab.transform.localPosition = transform.localPosition + new Vector3(0, 0.5f, 0); // add a little height
                    CollectGoldParticlesPrefab.Play();
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
