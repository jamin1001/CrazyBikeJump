using MoreMountains.Tools;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Unity.VisualScripting;
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


    //////////////// COLLISION ENTER
 
    void OnCollisionEnter(Collision collision)
    {
        // If already crashed, don't be adding other things.
        if (Game.Inst.IsBikeCrashed)
            return;

        GameObject otherOb = collision.collider.gameObject;
        GameObject parentOfOtherOb = otherOb.gameObject.transform.parent.gameObject;
        string obName = otherOb.name;
        string parentName = parentOfOtherOb.name;

        // CAPSULE
        if (collision.collider is CapsuleCollider)
        {
            // HIT!!!!!!!!!

            // Looks better for a flagpole if the crash explosion is sitting directly in line with the pole.
            float xOffset = 0;
            /* TODO address this without using tags.
            if (collision.gameObject.tag.Contains("Flag"))
                if (transform.position.x < collision.transform.position.x)
                    xOffset = -1f;
                else
                    xOffset = 1f;
            */

            // Object sounds from being hit!
            if (parentOfOtherOb.name.Contains("animal"))
                parentOfOtherOb.GetComponent<AudioSource>().PlayDelayed(0.3f);
                
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
    
        // MESH COLLIDER - Since BikeCenter (hitter) is subobject of rigidbody in question, the collision
        // shape will make it into this method against the collider (hittee).
        else if(collision.collider is MeshCollider)
        {
            // Hitting the hexring means we have errored on this one, so turn red.
            if (otherOb.name.Contains("hexring"))
            {
                // If the hexpanel is still turned off and we hit the border, then it is a fail.
                if (parentOfOtherOb.transform.GetChild(1).gameObject.active)
                {
                    otherOb.GetComponent<AudioSource>().PlayDelayed(0.2f);

                    // Turn off hex panel and turn on error hex panel.
                    parentOfOtherOb.transform.GetChild(1).gameObject.SetActive(false);
                    parentOfOtherOb.transform.GetChild(2).gameObject.SetActive(true);
                }

            }
            else if (otherOb.name.Contains("hexpanel"))
            {
                // If the error hexpanel is still turned off and we hit the panel, then it is a success.
                if (!parentOfOtherOb.transform.GetChild(2).gameObject.active) {

                    parentOfOtherOb.GetComponent<AudioSource>().PlayDelayed(0.2f);

                    // Turn off the hex panel, it has been "collected".
                    otherOb.SetActive(false);

                    // Hacky: Copied from below.
                    Vector3 starFxPosition = transform.localPosition + new Vector3(0, 0.5f, 0); // add a little height
                    ParticleSystem.Burst burst = new ParticleSystem.Burst(0, 1, 1, 1, 0.01f);

                    if (parentName.Contains("1"))
                    {
                        CollectBronzeParticlesPrefab.transform.localPosition = starFxPosition;
                        CollectBronzeParticlesPrefab.Play();
                        CollectBronzeParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();
                        CollectBronzeParticlesPrefab.emission.SetBursts(new ParticleSystem.Burst[] { burst });
                        Game.Inst.CollectStar(0);
                    }
                    else if (parentName.Contains("2"))
                    {
                        CollectSilverParticlesPrefab.transform.localPosition = starFxPosition;
                        CollectSilverParticlesPrefab.Play();
                        CollectSilverParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();
                        CollectSilverParticlesPrefab.emission.SetBursts(new ParticleSystem.Burst[] { burst });
                        Game.Inst.CollectStar(1);
                    }
                    else if (parentName.Contains("3"))
                    {
                        CollectGoldParticlesPrefab.transform.localPosition = starFxPosition;
                        CollectGoldParticlesPrefab.Play();
                        CollectGoldParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();
                        CollectGoldParticlesPrefab.emission.SetBursts(new ParticleSystem.Burst[] { burst });
                        Game.Inst.CollectStar(2);
                    }
                }
            }
        }
    }

    IEnumerator StopEverything()
    {
        Game.Inst.Restart(false);
        yield return Game.Inst.WaitParticlesStop;

    }

    //////////////// COLLISION EXIT

    void OnCollisionExit(Collision collision)
    {
        if (Game.Inst.IsBikeCrashed)
            return;

        if (collision.collider is BoxCollider)
        {
            // Object that has the box component you are colliding into.
            GameObject boxOb = collision.collider.gameObject;

            if (!Game.Inst.IsBikeRestarting)
            {
                string boxObName = boxOb.name;

                // SWERVE BOXES
                if (boxObName.Contains("swerve"))
                {
                    if (transform.position.z > boxOb.transform.position.z) // Ignores the reset case where the bike ends up at the start and so is BEHIND the swerved object.
                    {
                        string parentName = boxOb.transform.parent.gameObject.name;

                        // Barriers get squishy when passed.
                        if (parentName.Contains("barrier"))
                        {
                            // Turn on crazy move.
                            //boxOb.transform.parent.GetChild(0).GetComponent<MMPathMovement>().enabled = true;
                            boxOb.transform.parent.GetChild(0).GetChild(0).GetComponent<MMSquashAndStretch>().enabled = true;

                            // Turn off all collisions too.
                            boxOb.transform.parent.GetChild(0).GetChild(0).GetChild(0).GetComponent<BoxCollider>().enabled = false; // model
                            boxOb.transform.parent.GetChild(0).GetChild(0).GetChild(0).GetComponent<CapsuleCollider>().enabled = false; // model
                            boxOb.transform.parent.GetChild(1).GetComponent<BoxCollider>().enabled = false; // left swerve
                            boxOb.transform.parent.GetChild(2).GetComponent<BoxCollider>().enabled = false; // right swerve
                        }

                        // When in swerve mode, these continue it.
                        SwerveAudio.Play();
                        SwerveParticlesPrefab.Play();
                        Game.Inst.Swerve();

                        if (parentName.Contains("animal"))
                        {
                            // Play swerve sound.
                            boxOb.GetComponent<AudioSource>().PlayDelayed(0.3f);
                        }
                    }
                    
                }

                // NORMAL BOXES (NOT PRIZE OR DANGER BOXES)
                else if (!boxObName.Contains("Flag") && !boxObName.Contains("carDanger"))
                {
                    Vector3 starFxPosition = transform.localPosition + new Vector3(0, 0.5f, 0); // add a little height

                    // In this approach, the type of object jumped gets the corresponding reward.
                    if (boxObName.Contains("action"))
                    {
                        // DO ACTION

                        // Spin away barriers.
                        if (boxObName.Contains("barrier"))
                        {
                            // Spin it outta here.

                            // Move up and out (and left/right random).
                            MMPathMovement mpath = boxOb.transform.GetComponent<MMPathMovement>();
                            mpath.enabled = true;

                            if (boxObName.Contains("1")) {
                                mpath.PathElements[1].PathElementPosition.x = Random.Range(-1f, 1f);
                            } else if(boxObName.Contains("2")) {
                                mpath.PathElements[1].PathElementPosition.x = Random.Range(-2f, 2f);
                            }
                            else //if (boxObName.Contains("3"))
                            {
                                mpath.PathElements[1].PathElementPosition.x = Random.Range(-4f, 4f);
                            }

                            // Twirl around.
                            boxOb.transform.GetComponent<MMAutoRotate>().enabled = true;

                            // This locks rotation so make sure we clear it.
                            boxOb.transform.parent.GetComponent<MMSquashAndStretch>().enabled = false;

                            // Turn off all collisions too.
                            boxOb.transform.GetComponent<BoxCollider>().enabled = false; // model
                            boxOb.transform.GetComponent<CapsuleCollider>().enabled = false; // model
                            boxOb.transform.parent.parent.parent.GetChild(1).GetComponent<BoxCollider>().enabled = false; // left swerve
                            boxOb.transform.parent.parent.parent.GetChild(2).GetComponent<BoxCollider>().enabled = false; // right swerve
                        }
                        // Jump small animals.
                        else if(boxObName.Contains("1animal"))
                        {
                            // This enables wiggling which needs to be turned off if the level is reset.
                            MMPathMovement mpath = boxOb.transform.GetComponent<MMPathMovement>();
                            mpath.enabled = true;
                            mpath.PathElements[0].PathElementPosition.y = 0.2f;
                            mpath.PathElements[1].PathElementPosition.y = -0.1f;
                        }

                        if(boxObName.Contains("animal")) {
                            // Play the squeak sound or whatever.
                            boxOb.GetComponent<AudioSource>().Play();
                        }

                        // AWARD STAR

                        ParticleSystem.Burst burst = new ParticleSystem.Burst(0, 1, 1, 1, 0.01f);
                       
                        if (boxObName.Contains("1"))
                        {
                            CollectBronzeParticlesPrefab.transform.localPosition = starFxPosition;
                            CollectBronzeParticlesPrefab.Play();
                            CollectBronzeParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();
                            CollectBronzeParticlesPrefab.emission.SetBursts(new ParticleSystem.Burst[] { burst });
                            Game.Inst.CollectStar(0);
                        }
                        else if(boxObName.Contains("2"))
                        {
                            CollectSilverParticlesPrefab.transform.localPosition = starFxPosition;
                            CollectSilverParticlesPrefab.Play();
                            CollectSilverParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();
                            CollectSilverParticlesPrefab.emission.SetBursts(new ParticleSystem.Burst[] { burst });
                            Game.Inst.CollectStar(1);
                        }
                        else if (boxObName.Contains("3"))
                        {
                            CollectGoldParticlesPrefab.transform.localPosition = starFxPosition;
                            CollectGoldParticlesPrefab.Play();
                            CollectGoldParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();
                            CollectGoldParticlesPrefab.emission.SetBursts(new ParticleSystem.Burst[] { burst });
                            Game.Inst.CollectStar(2);
                        }

                    }
                    
                }

            }
        }
    }

}
