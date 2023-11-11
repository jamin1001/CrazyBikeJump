using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using UnityEngine;

public class Bike : MonoBehaviour
{
    public AudioSource PedalAudio;
    public AudioSource SwerveAudio;
    public AudioSource Jump1Audio;
    public AudioSource Jump2Audio;
    public AudioSource Jump3Audio;
    public AudioSource WindAudio;
 
    public float BikeCyclePitchScale = 2.0f;

    public ParticleSystem CrashParticlesPrefab; // world
    public ParticleSystem CrashWallParticlesPrefab; // world
    public ParticleSystem SwerveParticlesPrefab; // world
    public ParticleSystem CollectBronzeParticlesPrefab; // world
    public ParticleSystem CollectSilverParticlesPrefab; // world
    public ParticleSystem CollectGoldParticlesPrefab; // world
    public ParticleSystem CollectFlagYellowParticlesPrefab; // world
    public ParticleSystem CollectFlagRedParticlesPrefab; // world
    public ParticleSystem CollectFlagBlueParticlesPrefab; // world
    public ParticleSystem WindParticlesPrefab;  // local to bike

    public float CarAccel = 20f;
    public float CarStartSpeed = 5f;

    private List<GameObject> movingCars = new();
    private List<float> movingCarsSpeed = new();


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

    public void Pedaled(float bikeSpeed)
    {
        if (!PedalAudio.isPlaying)
        {
            PedalAudio.pitch = BikeCyclePitchScale * bikeSpeed;
            PedalAudio.Play();
        }
    }

    public void ResetMovingCars()
    {
        movingCars.Clear();
        movingCarsSpeed.Clear();
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
        if (Game.Inst.IsCrashed)
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
            }
            // OTHER OBJECTS
            else if(obName.Contains("carDanger"))
            {
                GameObject carOb = otherOb.transform.parent.gameObject;

                movingCars.Add(carOb);
                movingCarsSpeed.Add(CarStartSpeed);

                carOb.GetComponent<AudioSource>().Play();
            }
            
        }    
    }

    IEnumerator StopEverything()
    {
        Game.Inst.Restart(false);
        yield return Game.Inst.WaitParticlesStop;

    }

    void OnCollisionExit(Collision collision)
    {
        if (Game.Inst.IsCrashed)
            return;

        if (collision.collider is BoxCollider)
        {
            GameObject otherOb = collision.collider.gameObject;

            if (!Game.Inst.IsRestarting)
            {
                string obName = otherOb.name;

                // BOXES ARE SWERVE AROUND
                if (obName.Contains("swerve"))
                {
                    SwerveAudio.Play();
                    SwerveParticlesPrefab.Play();
                    Game.Inst.Swerve();

                    string parentName = otherOb.transform.parent.gameObject.name;

                    if (parentName.Contains("cow") || parentName.Contains("horse") || parentName.Contains("sheep"))
                        otherOb.GetComponent<AudioSource>().Play();
                }
                
                // AND EXCEPT FOR PRIZE BOXES
                else if(!obName.Contains("Atm") && !obName.Contains("Flag") && !obName.Contains("carDanger"))
                {
                    Vector3 starFxPosition = transform.localPosition + new Vector3(0, 0.5f, 0); // add a little height

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

                    
                }

            }
        }
    }

}
