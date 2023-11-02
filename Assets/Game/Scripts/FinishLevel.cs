using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinishLevel : MonoBehaviour
{
    public float FireworksHeightY;
    public float FireworksVariationX;
    public float FireworksVariationY;

    // Start is called before the first frame update
    void OnCollisionEnter(Collision collision)
    {
        Game.Inst.IsFinished = true;
        Game.Inst.BikeStopped();
        Game.Inst.PlayOneShot(Game.Inst.FinishLineClip);
        StartCoroutine(PopPopPop(collision.gameObject)); 
    }

    IEnumerator PopPopPop(GameObject obThatEnteredFinishLine)
    {
        Transform confettiParent = transform.GetChild(3); // Careful: this is a specific child within the Finish scene node.

        List<WaitForSecondsRealtime> waitTimes = Game.WaitTimes1;
        int waitCount = waitTimes.Count;
        int randomWait;
        /*
        float sameZ = confettiParent.GetChild(0).gameObject.transform.position.z; 
        float offsetX = Camera.main.transform.position.x;
        float offsetY = HeightY;
        */
        Vector3 obPos = obThatEnteredFinishLine.transform.position;

        yield return Game.WaitConfettiStart;

        // 1
        Game.Inst.PlayOneShot(Game.Inst.ConfettiBangClip);
        confettiParent.GetChild(0).gameObject.SetActive(true);
        confettiParent.GetChild(0).position = obPos + new Vector3(Random.Range(-FireworksVariationX, +FireworksVariationX), FireworksHeightY + Random.Range(-FireworksVariationY, +FireworksVariationY), 0);
        
        // 2
        randomWait = Random.Range(0, waitCount);
        yield return waitTimes[randomWait];
        Game.Inst.PlayOneShotRandom(Game.Inst.ConfettiPopClips);
        confettiParent.GetChild(1).gameObject.SetActive(true);
        confettiParent.GetChild(1).position = obPos + new Vector3(Random.Range(-FireworksVariationX, +FireworksVariationX), FireworksHeightY + Random.Range(-FireworksVariationY, +FireworksVariationY), 0);
       
        // 3
        randomWait = Random.Range(0, waitCount);
        yield return waitTimes[randomWait];
        Game.Inst.PlayOneShotRandom(Game.Inst.ConfettiPopClips);
        confettiParent.GetChild(2).gameObject.SetActive(true);
        confettiParent.GetChild(2).position = obPos + new Vector3(Random.Range(-FireworksVariationX, +FireworksVariationX), FireworksHeightY + Random.Range(-FireworksVariationY, +FireworksVariationY), 0);

        yield return Game.WaitConfettiStop;

        confettiParent.GetChild(0).gameObject.SetActive(false);
        confettiParent.GetChild(1).gameObject.SetActive(false);
        confettiParent.GetChild(2).gameObject.SetActive(false);

        // Check score and pick here.
        //Game.Inst.PlayOneShot(Game.Inst.FinishedBadClip);
        Game.Inst.PlayOneShot(Game.Inst.FinishedGoodClip);
        //Game.Inst.PlayOneShot(Game.Inst.FinishedWonderfulClip);

        Game.Inst.IsFinished = false;

        // Give a chance to update IsFinished before we exit here. It seems there was an issue with this
        // not getting set to false correctly, and then getting stuck in the update loop by returning early all the time.
        yield return null; 

        Game.Inst.Restart(true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
