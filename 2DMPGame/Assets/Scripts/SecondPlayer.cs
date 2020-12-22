using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecondPlayer : MonoBehaviour
{

    private float hp = 100;
    private System.Random rnd = new System.Random();


    public GameObject[] bloodArray = new GameObject[7];

    public Animator topAnimator;
    public SpriteRenderer bottomRenderer;



    private Transform tform;
    // Start is called before the first frame update
    void Start()
    {
        //tform = GetComponent<Transform>();

    }
    public void Death()
    {
        topAnimator.SetBool("Dead", true);
        bottomRenderer.enabled = false;

    }
    public void gotHit(float hp)
    {
        Debug.Log("OUCH");
        int r = rnd.Next(0, 7);
        //randomInt = (Random.Range(0, 6));
        // Instantiate(bloodArray[0], tform.position, Quaternion.identity);
        Instantiate(bloodArray[r],transform.position, Quaternion.identity);
        if (hp <= 0) Death();
    }

}
