using UnityEngine;
using System.Collections;

public class ExperimentManager : MonoBehaviour
{
    public Material StimulusLeft;
    public Material StimulusRight;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("Experiment has Started");
        StimulusLeft.SetFloat("_Contrast", 0);
        StimulusRight.SetFloat("_Contrast", 1); 

        StartCoroutine(IncreaseContrast(15));

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    IEnumerator IncreaseContrast(float duration)
    {
        float t = 0; 
        while (t < duration)
        {
            float c = t / duration; 
            
            StimulusLeft.SetFloat("_Contrast", c);
            t += Time.deltaTime; 
            yield return null;
        }
    StimulusLeft.SetFloat("_Contrast",1);
    }
     
}
