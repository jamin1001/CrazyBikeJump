using MoreMountains.Tools;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class Bike : MonoBehaviour
{
    public AudioSource PedalAudio;
    public AudioSource SwerveAudio;
    public AudioSource Jump1Audio;
    public AudioSource Jump2Audio;
    public AudioSource Jump3Audio;
    public AudioSource JumpFallAudio;
    public AudioSource ScreechAudio;
    public AudioSource LandThudAudio;
    public AudioSource WheelThud0Audio;
    public AudioSource WheelThud1Audio;
    public AudioSource WheelThud2Audio;
    public AudioSource WheelThud3Audio;
    public AudioSource WindAudio;
 
    public float BikeCyclePitchScale = 0.2f;
    public float BikeCyclePitchMinSpeed = 0.4f;

    public ParticleSystem CrashParticlesPrefab; // world
    public ParticleSystem CrashWallParticlesPrefab; // world
    public ParticleSystem SwerveParticlesPrefab; // world
    public ParticleSystem CollectBronzeParticlesPrefab; // world
    public ParticleSystem CollectSilverParticlesPrefab; // world
    public ParticleSystem CollectGoldParticlesPrefab; // world
    public ParticleSystem CollectFlagYellowParticlesPrefab; // world
    public ParticleSystem CollectFlagRedParticlesPrefab; // world
    public ParticleSystem CollectFlagBlueParticlesPrefab; // world
    public ParticleSystem CollectFlagCoinParticlesPrefab; // world
    public ParticleSystem WindParticlesPrefab;  // local to bike

    public float CarAccel = 20f;
    public float CarStartSpeed = 5f;

    private List<GameObject> movingCars = new();
    private List<float> movingCarsSpeed = new();
    private List<Vector3> originalCarPositions = new();

    public void Jumped1()
    {
        Jump1Audio.Play();
    }

    public void Jumped2()
    {
        Jump2Audio.Play();
    }

    public void Jumped3()
    {
        Jump3Audio.Play();
    }

    public void JumpedFall()
    {
        JumpFallAudio.Play();
    }

    public void Screech()
    {
        if(!ScreechAudio.isPlaying)
            ScreechAudio.Play();
    }

    public void LandThud()
    {
        LandThudAudio.Play();
    }
    public void WheelThud(int wheelLevel)
    {
        switch(wheelLevel)
        {
            case 0:
                WheelThud0Audio.Play();
                break;
            case 1:
                WheelThud1Audio.Play();
                break;
            case 2:
                WheelThud2Audio.Play();
                break;
            case 3:
                WheelThud3Audio.Play();
                break;
        }
   
    }

    public void Pedaled(float bikeSpeed)
    {
        if (!PedalAudio.isPlaying)
        {
            if (bikeSpeed > BikeCyclePitchMinSpeed)
            {
                PedalAudio.pitch = BikeCyclePitchScale * bikeSpeed;
                PedalAudio.Play();
            }
        }
    }

    public void ResetMovingCars()
    {
        for(int c = 0; c < originalCarPositions.Count; c++)
            movingCars[c].transform.position = originalCarPositions[c];

        movingCars.Clear();
        movingCarsSpeed.Clear();
        originalCarPositions.Clear();
    }

    public void StartWind()
    {
        WindParticlesPrefab.Play();
    }

    public void StopWind()
    {
        WindParticlesPrefab.Stop();
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        for (int c = 0; c < movingCars.Count; c++)
        {
            GameObject car = movingCars[c];
            float carSpeed = movingCarsSpeed[c];
            carSpeed += CarAccel * dt;
            movingCarsSpeed[c] = carSpeed;

            car.transform.position = new Vector3(
               car.transform.position.x,
               car.transform.position.y,
               car.transform.position.z - carSpeed * dt);
        }
       
    }

    void OnCollisionEnter(Collision collision)
    {
        // If already crashed, don't be adding other things.
        if (Game.Inst.IsBikeCrashed)
            return;

        GameObject otherOb = collision.collider.gameObject;
        string obName = otherOb.name;

        // CAPSULE
        if (collision.collider is CapsuleCollider)
        {
            // HIT!!!!!!!!!
            
            // Looks better for a flagpole if the crash explosion is sitting directly in line with the pole.
            float xOffset = 0;
            if (collision.gameObject.tag.Contains("Flag"))
                if (transform.position.x < collision.transform.position.x)
                    xOffset = -1f;
                else
                    xOffset = 1f;
            if (collision.gameObject.tag.Contains("Atm"))
                xOffset = -1f;

            // Object sounds from being hit!
            if (obName.Contains("cow") || obName.Contains("horse") || obName.Contains("sheep"))
                otherOb.GetComponent<AudioSource>().Play();
                
            CrashParticlesPrefab.transform.position = collision.transform.position + new Vector3(xOffset, 1f, -0.5f);
            CrashParticlesPrefab.Play();
            CrashParticlesPrefab.transform.GetChild(1).gameObject.SetActive(true); // Child particle too.
            CrashParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();
            Game.Inst.BikeCrashed();
            StartCoroutine(StopEverything());
        }
        // BOX
        else if(collision.collider is BoxCollider)
        {
            // HIT WALL
            if(obName.Contains("race_track"))
            {
                float xOffset = 0;
                if (transform.position.x < collision.transform.position.x)
                    xOffset = -0.7f;
                else
                    xOffset = 0.7f;

                CrashWallParticlesPrefab.transform.position = transform.position + new Vector3(xOffset, 0, 0);
                CrashWallParticlesPrefab.Play();
                CrashWallParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();
                Game.Inst.BikeCrashed();
                StartCoroutine(StopEverything());
            }
            // ATM PANEL
            else if(obName.Contains("Atm"))
            {
                Debug.LogWarning("Starting ATM...");
                Game.Inst.StartAtm();
            }
            // FLAG COLLECT
            else if (obName.Contains("Flag"))
            {
                Vector3 flagFxPosition = transform.localPosition + new Vector3(0, 1.7f, 0); // add a little height

                if (obName.Contains("Yellow"))
                {
                    CollectFlagYellowParticlesPrefab.transform.localPosition = flagFxPosition;
                    CollectFlagYellowParticlesPrefab.Play();
                    CollectFlagYellowParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();

                    // Disable flag and hit box now that we've collected.
                    otherOb.transform.GetChild(0).GetChild(2).gameObject.SetActive(false);
                    otherOb.GetComponent<BoxCollider>().enabled = false;

                    Game.Inst.CollectFlag(0);
                }
                else if (obName.Contains("Red"))
                {
                    CollectFlagRedParticlesPrefab.transform.localPosition = flagFxPosition;
                    CollectFlagRedParticlesPrefab.Play();
                    CollectFlagRedParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();

                    otherOb.transform.GetChild(0).GetChild(2).gameObject.SetActive(false);
                    otherOb.GetComponent<BoxCollider>().enabled = false;

                    Game.Inst.CollectFlag(1);
                }
                else if (obName.Contains("Blue"))
                {
                    CollectFlagBlueParticlesPrefab.transform.localPosition = flagFxPosition;
                    CollectFlagBlueParticlesPrefab.Play();
                    CollectFlagBlueParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();

                    otherOb.transform.GetChild(0).GetChild(2).gameObject.SetActive(false);
                    otherOb.GetComponent<BoxCollider>().enabled = false;

                    Game.Inst.CollectFlag(2);
                }
                else if (obName.Contains("Coin"))
                {
                    CollectFlagCoinParticlesPrefab.transform.localPosition = flagFxPosition;
                    CollectFlagCoinParticlesPrefab.Play();
                    CollectFlagCoinParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();

                    otherOb.transform.GetChild(0).GetChild(2).gameObject.SetActive(false);
                    otherOb.GetComponent<BoxCollider>().enabled = false;

                    Game.Inst.StartAtm();
                }
            }
            // OTHER OBJECTS
            else if(obName.Contains("carDanger"))
            {
                GameObject carOb = otherOb.transform.parent.gameObject;

                movingCars.Add(carOb);
                movingCarsSpeed.Add(CarStartSpeed);
                originalCarPositions.Add(carOb.transform.position);

                carOb.GetComponent<AudioSource>().Play();
            }
            
        }    
    }

    IEnumerator StopEverything()
    {
        Game.Inst.Restart(false);
        yield return Game.Inst.WaitParticlesStop;

    }

    void DoTheSquish()
    {

    }

    void DoTheSpinout()
    {
       
    }

    void OnCollisionExit(Collision collision)
    {
        if (Game.Inst.IsBikeCrashed)
            return;

        if (collision.collider is BoxCollider)
        {
            GameObject otherOb = collision.collider.gameObject;

            if (!Game.Inst.IsBikeRestarting)
            {
                string obName = otherOb.name;

                // BOXES ARE SWERVE AROUND
                if (obName.Contains("swerve"))
                {

                    if (transform.position.z > otherOb.transform.position.z) // Ignores the reset case where the bike ends up at the start and so is BEHIND the swerved object.
                    {
                        string parentName = otherOb.transform.parent.gameObject.name;

                        if (parentName.Contains("cone"))
                        {
                            otherOb.transform.parent.GetChild(2).GetComponent<MMPathMovement>().enabled = true;

                            // Turn off all collisions too.
                            otherOb.transform.parent.GetChild(2).GetChild(0).GetChild(0).GetComponent<BoxCollider>().enabled = false; // model
                            otherOb.transform.parent.GetChild(2).GetChild(0).GetChild(0).GetComponent<CapsuleCollider>().enabled = false; // model
                            otherOb.transform.parent.GetChild(0).GetComponent<BoxCollider>().enabled = false; // left swerve
                            otherOb.transform.parent.GetChild(1).GetComponent<BoxCollider>().enabled = false; // right swerve
                        }

                        /*
                        SwerveAudio.Play();
                        SwerveParticlesPrefab.Play();
                        Game.Inst.Swerve();

                        string parentName = otherOb.transform.parent.gameObject.name;

                        if (parentName.Contains("cow") || parentName.Contains("horse") || parentName.Contains("sheep"))
                            otherOb.GetComponent<AudioSource>().Play();
                        */
                    }
                    
                }

                // AND EXCEPT FOR PRIZE BOXES
                else if (!obName.Contains("Atm") && !obName.Contains("Flag") && !obName.Contains("carDanger"))
                {
                    Vector3 starFxPosition = transform.localPosition + new Vector3(0, 0.5f, 0); // add a little height

                    // In this approach, the type of object jumped gets the corresponding reward.
                    // Hack for now, later use tag lookup.
                    if (
                        obName.Contains("sheep") ||
                        obName.Contains("cone") ||
                        obName.Contains("passenger"))
                    {
                        CollectBronzeParticlesPrefab.transform.localPosition = starFxPosition;
                        CollectBronzeParticlesPrefab.Play();
                        CollectBronzeParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();
                        Game.Inst.CollectStar(0);

                        ParticleSystem.Burst burst = new ParticleSystem.Burst(0, 1, 1, 1, 0.01f);
                        CollectBronzeParticlesPrefab.emission.SetBursts(new ParticleSystem.Burst[] { burst });

                        if(obName.Contains("cone")) {
                            //otherOb.GetComponent<Renderer>().material = Game.Inst.DissolveMaterials[0];
                            //otherOb.transform.GetChild(2).GetComponent<MMPathMovement>().enabled = true;
                            //otherOb.transform.parent.parent.GetComponent<MMPathMovement>().enabled = true;

                            // Spin it outta here.

                            // Move up and out (and left/right random).

                            MMPathMovement mpath = otherOb.transform.GetComponent<MMPathMovement>();//.enabled = true;
                            mpath.enabled = true;
                            mpath.PathElements[1].PathElementPosition.x = Random.Range(-5f, 5f);

                            // Twirl around.
                            otherOb.transform.GetComponent<MMAutoRotate>().enabled = true;

                            // This locks rotation so make sure we clear it.
                            otherOb.transform.parent.GetComponent<MMSquashAndStretch>().enabled = false;

                            // Turn off all collisions too.
                            otherOb.transform.GetComponent<BoxCollider>().enabled = false; // model
                            otherOb.transform.GetComponent<CapsuleCollider>().enabled = false; // model
                            otherOb.transform.parent.parent.parent.GetChild(0).GetComponent<BoxCollider>().enabled = false; // left swerve
                            otherOb.transform.parent.parent.parent.GetChild(1).GetComponent<BoxCollider>().enabled = false; // right swerve
                        }


                    }
                        else if (
                        obName.Contains("horse") ||
                        obName.Contains("tires") || obName.Contains("barrel") ||
                        obName.Contains("police") || obName.Contains("hippie"))
                    {
                        CollectSilverParticlesPrefab.transform.localPosition = starFxPosition;
                        CollectSilverParticlesPrefab.Play();
                        CollectSilverParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();
                        Game.Inst.CollectStar(1);

                        ParticleSystem.Burst burst = new ParticleSystem.Burst(0, 1, 1, 1, 0.01f);
                        CollectSilverParticlesPrefab.emission.SetBursts(new ParticleSystem.Burst[] { burst });

                        //otherOb.GetComponent<Renderer>().material = Game.Inst.DissolveMaterials[0];

                    }
                    else if(
                        obName.Contains("cow") ||
                        obName.Contains("concrete") ||
                        obName.Contains("formula") || obName.Contains("truck"))
                    {
                        CollectGoldParticlesPrefab.transform.localPosition = starFxPosition;
                        CollectGoldParticlesPrefab.Play();
                        CollectGoldParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();
                        Game.Inst.CollectStar(2);

                        ParticleSystem.Burst burst = new ParticleSystem.Burst(0, 1, 1, 1, 0.01f);
                        CollectGoldParticlesPrefab.emission.SetBursts(new ParticleSystem.Burst[] { burst });

                        // otherOb.GetComponent<Renderer>().material = Game.Inst.DissolveMaterials[0];
                    }

                    // This approach plays the fixed star count embedded in the particle object. Higher stars for more jumps.
                    /*
                    if (Game.Inst.JumpCount == 1)
                    {
                        CollectBronzeParticlesPrefab.transform.localPosition = starFxPosition;
                        CollectBronzeParticlesPrefab.Play();
                        CollectBronzeParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();
                        Game.Inst.CollectStar(0);
                    }
                    else if(Game.Inst.JumpCount == 2)
                    {
                        CollectSilverParticlesPrefab.transform.localPosition = starFxPosition;
                        CollectSilverParticlesPrefab.Play();
                        CollectSilverParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();
                        Game.Inst.CollectStar(1);
                    }
                    else if(Game.Inst.JumpCount == 3)
                    {
                        CollectGoldParticlesPrefab.transform.localPosition = starFxPosition;
                        CollectGoldParticlesPrefab.Play();
                        CollectGoldParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();
                        Game.Inst.CollectStar(2);
                    }
                    */
                    
                }

            }
        }
    }

}
