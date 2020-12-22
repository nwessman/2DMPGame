using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Player : MonoBehaviour {

    

    private Rigidbody2D rb2d;
    public Animator bottomAnimator;
    public Animator topAnimator;
    public SpriteRenderer bottomRenderer;

    private float speed = 5;
    private Vector2 moveVelocity;
    private float hp = 100;

    float angle;

    private System.Random rnd = new System.Random();
    public GameObject[] bloodArray = new GameObject[7];

    private bool dead = false;


    void Start(){
        rb2d = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        //Input
        float moveHorizontal = Input.GetAxisRaw("Horizontal");
        float moveVertical = Input.GetAxisRaw("Vertical");
        Vector2 movement = new Vector2(moveHorizontal, moveVertical);

        moveVelocity = movement.normalized * speed;
        if (Input.GetMouseButtonDown(0))
            Attack();
    }
    public void gotHit(float hp)
    {
        int r = rnd.Next(0, 7);
        Instantiate(bloodArray[r], transform.position, Quaternion.identity);
        if (hp <= 0) Death();
    }
    void Death()
    {
        topAnimator.SetBool("Dead",true);
        dead = true;
        bottomRenderer.enabled = false;

    }
    void Attack()
    {
        topAnimator.SetTrigger("Attack");
        Main.playerAttack = true;
    }
    void FixedUpdate()
    {
        if (!dead) {
            if (moveVelocity.x != 0 || moveVelocity.y != 00) {
                bottomAnimator.SetBool("Walking", true);
            } else {
                bottomAnimator.SetBool("Walking", false);
            }
            rb2d.MovePosition(rb2d.position + moveVelocity * Time.fixedDeltaTime);

            Vector3 mousePos = Camera.main.ScreenToViewportPoint(Input.mousePosition);

            mousePos.x -= 0.5f;
            mousePos.y -= 0.5f;

            //float angle = Mathf.Atan(mousePos.y / mousePos.x);
            angle = Mathf.Atan(mousePos.y / mousePos.x);
            float angleInDegrees = (180 / Mathf.PI) * angle;

            if (mousePos.x < 0) angleInDegrees = angleInDegrees + 180;
            else if (mousePos.y < 0) angleInDegrees = angleInDegrees + 360;
            angleInDegrees = angleInDegrees - 90;


            transform.eulerAngles = new Vector3(0, 0, angleInDegrees);


            //Debug.Log(angleInDegrees);


            float x2 = Mathf.Cos((Mathf.PI / 180) * (angleInDegrees + 90)) * 3;
            float y2 = Mathf.Sin((Mathf.PI / 180) * (angleInDegrees + 90)) * 3;
            Vector3 forward = new Vector3(x2, y2, 0) * 1;
            //Debug.DrawRay(transform.position, forward, Color.green);
            Debug.DrawLine(transform.position, transform.position + forward, Color.green);

            float x3 = Mathf.Cos((Mathf.PI / 180) * (angleInDegrees + 90 + 70)) * 3;
            float y3 = Mathf.Sin((Mathf.PI / 180) * (angleInDegrees + 90 + 70)) * 3;
            Vector3 left = new Vector3(x3, y3, 0) * 1;
            Debug.DrawRay(transform.position, left, Color.red);

            float x4 = Mathf.Cos((Mathf.PI / 180) * (angleInDegrees + 90 - 45)) * 3;
            float y4 = Mathf.Sin((Mathf.PI / 180) * (angleInDegrees + 90 - 45)) * 3;
            Vector3 right = new Vector3(x4, y4, 0) * 1;
            Debug.DrawRay(transform.position, right, Color.magenta);
        }

    }
}
