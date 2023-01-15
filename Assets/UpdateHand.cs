using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateHand : MonoBehaviour
{
    //GameObject indexPatch;

    // Start is called before the first frame update
    void Start()
    {



    }

    // Update is called once per frame
    void Update()
    {
        // var indexLeft = GameObject.Find("HaptxHandLeft(Clone)");
        // var indexRight = GameObject.Find("HaptxHandRight(Clone)");
        //Vector3 indexLeftPos = indexLeft.transform.position;
        //Vector3 indexRightPos = indexRight.transform.position;
        //Vector3 indexLeftLook = indexLeft.transform.TransformDirection(Vector3.forward);
        //Vector3 indexRightLook = indexRight.transform.TransformDirection(Vector3.forward);
        var isActive = true;
        /// indexPatch = GameObject.Find("HaptxHandLeft(Clone)");
        var hand = GameObject.Find("Cube");
        // var gO = new GameObject();
        // hand.transform.parent = indexPatch.transform;
        hand.transform.localPosition = Vector3.zero;
        // add script as component 
        // mono behaviours


        var hxWaveSpatialEffect = hand.AddComponent<HxWaveSpatialEffect>();



        var hxBoxBoundingVolume = hand.AddComponent<HxBoxBoundingVolume>();

        hxWaveSpatialEffect.BoundingVolume = hxBoxBoundingVolume;

        Vector3 minima = new Vector3(-0.1f, -0.1f, -0.1f);
        Vector3 maxima = new Vector3(0.5f, 0.5f, 0.5f);

        hxBoxBoundingVolume.SetExtremaM(minima, maxima);
        SetEffect(isActive, hxWaveSpatialEffect);
        // send the firebase 
    }
    void SetEffect(bool isActive, HxWaveSpatialEffect spatialEffect)
    {
        if (isActive)
        {
            // activate the effect
            spatialEffect.amplitudeN = 150.0f;
            spatialEffect.frequencyHz = 20.0f;
        }
        else
        {
            // reset the effect 
            spatialEffect.amplitudeN = 0.0f;
            spatialEffect.frequencyHz = 0.0f;
        }
    }
}
