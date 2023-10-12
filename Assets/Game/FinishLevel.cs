using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinishLevel : MonoBehaviour
{
    public float HeightY;
    public float VariationX;
    public float VariationY;

    // Start is called before the first frame update
    void OnCollisionEnter(Collision collision)
    {
        Game.Inst.BikeStopped();
        Game.Inst.IsFinished = true;
        StartCoroutine(PopPopPop()); 
    }

    IEnumerator PopPopPop()
    {
        Transform confettiParent = transform.GetChild(0);

        List<WaitForSecondsRealtime> waitTimes = Game.WaitTimes1;
        int waitCount = waitTimes.Count;
        int randomWait;
        float sameZ = confettiParent.GetChild(0).gameObject.transform.position.z;
        float offsetX = Camera.main.transform.position.x;
        float offsetY = HeightY;

        Game.Inst.PlayOneShot(Game.Inst.ConfettiBangClip);
        //randomWait = Random.Range(0, waitCount);
        //yield return Game.WaitList2[randomWait];

        Game.Inst.PlayOneShotRandom(Game.Inst.ConfettiPopClips);
        confettiParent.GetChild(0).gameObject.SetActive(true);
        confettiParent.GetChild(0).position = new Vector3(offsetX + Random.Range(-VariationX, +VariationX), offsetY + Random.Range(-VariationY, +VariationY), sameZ);
        
        Debug.Log("Pop 1");

        randomWait = Random.Range(0, waitCount);
        yield return waitTimes[randomWait];
        Game.Inst.PlayOneShotRandom(Game.Inst.ConfettiPopClips);
        confettiParent.GetChild(1).gameObject.SetActive(true);
        confettiParent.GetChild(1).position = new Vector3(offsetX + Random.Range(-VariationX, +VariationX), offsetY + Random.Range(-VariationY, +VariationY), sameZ);
        
        Debug.Log("Pop 2");

        randomWait = Random.Range(0, waitCount);
        yield return waitTimes[randomWait];
        Game.Inst.PlayOneShot(Game.Inst.ConfettiBangClip);
        confettiParent.GetChild(2).gameObject.SetActive(true);
        confettiParent.GetChild(2).position = new Vector3(offsetX + Random.Range(-VariationX, +VariationX), offsetY + Random.Range(-VariationY, +VariationY), sameZ);

        Debug.Log("Pop 3");

        yield return Game.WaitConfettiStop;

        Debug.Log("Confetti stopped");

        confettiParent.GetChild(0).gameObject.SetActive(false);
        confettiParent.GetChild(1).gameObject.SetActive(false);
        confettiParent.GetChild(2).gameObject.SetActive(false);

        Debug.Log("Turned off confettis");


        Game.Inst.IsFinished = false;
        Game.Inst.Restart();

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
