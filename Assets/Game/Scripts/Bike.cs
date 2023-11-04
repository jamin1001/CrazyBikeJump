using System.Collections;
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
    public ParticleSystem AtmParticlesPrefab;

    public ParticleSystem WindParticlesPrefab;  // local to bike

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
        GameObject otherOb = collision.collider.gameObject;
        string obName = otherOb.name;

        if (obName.Contains("cow"))
        {
            Game.Inst.PlayAnimal(Game.Inst.CowHitClip);    
        }

        // HIT
        if (collision.collider is CapsuleCollider)
        {
            // Looks better for a flagpole if the crash explosion is sitting directly in line with the pole.
            float xOffset = 0;
            if (collision.gameObject.tag.Contains("Flag"))
                if (transform.position.x < collision.transform.position.x)
                    xOffset = -1f;
                else
                    xOffset = 1f;
            if (collision.gameObject.tag.Contains("Atm"))
                xOffset = -1f;

            

            CrashParticlesPrefab.transform.position = collision.transform.position + new Vector3(xOffset, 1f, -0.5f);
            CrashParticlesPrefab.Play();
            CrashParticlesPrefab.transform.GetChild(1).gameObject.SetActive(true); // Child particle too.
            Game.Inst.PlayOneShot(Game.Inst.BikeCrashClip);
            Game.Inst.BikeCrashed();
            StartCoroutine(StopEverything());
        }
        else if(collision.collider is BoxCollider)
        {
            if(obName.Contains("Atm"))
            {
                Debug.LogWarning("Starting ATM...");
                Game.Inst.StartAtm();
            }
            // EXCEPT FOR FLAGS
            else if (obName.Contains("Flag"))
            {
                Game.Inst.PlayOneShot(Game.Inst.BikeCollectFlagClip);
                Vector3 flagFxPosition = transform.localPosition + new Vector3(0, 1.7f, 0); // add a little height

                if (obName.Contains("Yellow"))
                {
                    CollectFlagYellowParticlesPrefab.transform.localPosition = flagFxPosition;
                    CollectFlagYellowParticlesPrefab.Play();

                    // Disable flag and hit box now that we've collected.
                    otherOb.transform.GetChild(0).GetChild(2).gameObject.SetActive(false);
                    otherOb.GetComponent<BoxCollider>().enabled = false;

                    Game.Inst.CollectFlag(0);
                }
                else if (obName.Contains("Red"))
                {
                    CollectFlagRedParticlesPrefab.transform.localPosition = flagFxPosition;
                    CollectFlagRedParticlesPrefab.Play();

                    otherOb.transform.GetChild(0).GetChild(2).gameObject.SetActive(false);
                    otherOb.GetComponent<BoxCollider>().enabled = false;

                    Game.Inst.CollectFlag(1);
                }
                else if (obName.Contains("Blue"))
                {
                    CollectFlagBlueParticlesPrefab.transform.localPosition = flagFxPosition;
                    CollectFlagBlueParticlesPrefab.Play();
                    
                    otherOb.transform.GetChild(0).GetChild(2).gameObject.SetActive(false);
                    otherOb.GetComponent<BoxCollider>().enabled = false;

                    Game.Inst.CollectFlag(2);
                }
            }
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
            GameObject otherOb = collision.collider.gameObject;

            if (!Game.Inst.IsRestarting)
            {
                string obName = otherOb.name;

                // BOXES ARE SWERVE AROUND
                if (obName.Contains("swerve"))
                {
                    Game.Inst.PlayOneShot(Game.Inst.BikeSwerveClip);
                    SwerveParticlesPrefab.Play();
                    Game.Inst.Swerve();

                    if (obName.Contains("cow"))
                    {
                        Game.Inst.PlayAnimal(Game.Inst.CowSwerveClip);
                    }

                    Game.Inst.PlayOnOff(Game.Inst.BikeSailClip, 0.1f);
                }
                
                // AND EXCEPT FOR PRIZE BOXES
                else if(!obName.Contains("Atm") && !obName.Contains("Flag"))
                {
                    Game.Inst.PlayOneShot(Game.Inst.BikeCollectStarClip);
                    Vector3 starFxPosition = transform.localPosition + new Vector3(0, 0.5f, 0); // add a little height

                    if (Game.Inst.JumpCount == 1)
                    {
                        CollectBronzeParticlesPrefab.transform.localPosition = starFxPosition;
                        CollectBronzeParticlesPrefab.Play();
                        Game.Inst.CollectStar(0);
                    }
                    else if(Game.Inst.JumpCount == 2)
                    {
                        CollectSilverParticlesPrefab.transform.localPosition = starFxPosition;
                        CollectSilverParticlesPrefab.Play();
                        Game.Inst.CollectStar(1);
                    }
                    else if(Game.Inst.JumpCount == 3)
                    {
                        CollectGoldParticlesPrefab.transform.localPosition = starFxPosition;
                        CollectGoldParticlesPrefab.Play();
                        Game.Inst.CollectStar(2);
                    }

                    
                }

            }
        }
    }

    void OnCollisionStay(Collision collision)
    {

    }

}
