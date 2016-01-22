using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour {

    //Public Variables
    
    //Change to private static when final values are decided{
    public float ledgeSpeed = 1f;
    public float walkSpeed = 3f;
    public float runSpeed = 6f;

    private float currentSpeed;

    public float jumpForce = 12;
    public bool isGrabbing, pullUp, badLanding;
    //}

    public MouseLook headCam;
    public float crouchTime = 0.3f;

    public Vector3 grabbedPoint;
    public Vector3 lastGrabForward;
    public bool grabbing
    {
        get
        {
            return isGrabbing;
        }
        set
        {
            if (value)
            {
                GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionY;
            }
            else
            {
                GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
            }

            isGrabbing = value;
        }
    }

    public Texture2D damageImage;

    //Private Variables
    private bool isFalling, jumpLock, crouching, stopCrouch;
    private Vector3 forwardAxis, rightAxis;

    private float damageAlpha;


    // Update is called once per frame
	void Update () 
    {

        //Set forward is player if not grabbing, forward for grabbing set elsewhere
        if (!grabbing || Vector3.Angle(transform.forward, lastGrabForward) > 90)
        {
            forwardAxis = transform.forward;
            rightAxis = transform.right;
        }

        //Calculate isFalling
        if (Physics.Raycast(transform.position, Vector3.down, 1.25f))
        {
            isFalling = false;
            pullUp = false;

            //Calculate Fall Damage
            if(GetComponent<Rigidbody>().velocity.y < -15f)
            {
                badLanding = true;
                StartCrouch(0.5f);

                float amount = (1f / 50f) * -GetComponent<Rigidbody>().velocity.y;
                StartCoroutine("TakeDamage", amount);
            }
        }
        else
            isFalling = true;

        //Reset Jump
        if (Input.GetAxis("Jump") == 0)
            jumpLock = false;

        //Crouching
        if (Input.GetAxis("Crouch") != 0 && !crouching && !grabbing)
        {
            StartCrouch();
        }

        RaycastHit headPain;
        bool hitHead = Physics.SphereCast(transform.position, 0.4f, Vector3.up, out headPain, 1.5f);

        //Stop Croutching
        if (crouching && ((Input.GetAxis("Crouch") == 0 && !hitHead && !pullUp && !badLanding) || grabbing))
        {
            StopCoroutine("startCrouch");
            StartCoroutine("StopCrouch");
        }

        if (isFalling)
        {
            //Climbing Mechanics
            if (Input.GetAxis("Grab") != 0 && !grabbing && GetComponent<Rigidbody>().velocity.y < 0)
            {
                CheckGrab();
            }
        }

        //Grabbing Mechanics
        if (grabbing)
        {
           
            //Cancel Grab
            Vector3 between = grabbedPoint - transform.position;

            //Adjust Y Position
            if (between.y - Time.deltaTime > 1.75f)
            {
                Vector3 tp = transform.position;
                tp.y += Time.deltaTime;
                transform.position = tp;
            }

            if (Input.GetAxis("Grab") == 0)
                grabbing = false;

            const float ledgeDistance = 0.75f;

            if (Mathf.Abs(between.x) > ledgeDistance || Mathf.Abs(between.z) > ledgeDistance)
                CheckGrab();

        }

	
	}

    // Updates at 60FPS
    void FixedUpdate()
    {

        //Calculate input and set up appropriate axis'
        float verti = Input.GetAxis("Vertical");
        float hori = Input.GetAxis("Horizontal");
        bool sprint = (Input.GetAxis("Sprint") != 0);
        bool jump = (Input.GetAxis("Jump") != 0);

        Vector3 input = (hori * rightAxis) + (verti * forwardAxis);
        input.Normalize();

        //Calculate Player Speed
        float newSpeed = 0f;
        if (grabbing || crouching)
        {
            newSpeed = ledgeSpeed;
        }
        else if(sprint)
        {
            newSpeed = runSpeed;
        }
        else
        {
            newSpeed = walkSpeed; 
        }

        currentSpeed = Mathf.Lerp(currentSpeed, newSpeed, Time.deltaTime * 5f);
        input *= currentSpeed;

        // Apply a force that attempts to reach our target velocity
        Vector3 velocity = GetComponent<Rigidbody>().velocity;
        Vector3 velocityChange = (input - velocity);

        velocityChange.x = Mathf.Clamp(velocityChange.x, -currentSpeed, currentSpeed);
        velocityChange.z = Mathf.Clamp(velocityChange.z, -currentSpeed, currentSpeed);
        velocityChange.y = 0;

        GetComponent<Rigidbody>().AddForce(velocityChange, ForceMode.VelocityChange);

        //Perform Jumping
        if (!isFalling || grabbing)
        { 
            if(jump && !jumpLock)
            {
                jumpLock = true;

                grabbing = false;

                Vector3 vel = GetComponent<Rigidbody>().velocity;
                vel.y = jumpForce;

                if (pullUp && Vector3.Angle(transform.forward, lastGrabForward) < 45)
                {
                    StartCrouch();
                }

                GetComponent<Rigidbody>().velocity = vel;

            }
        }
    }

    void OnGUI()
    {
        Color nColour = Color.white;
        nColour.a = damageAlpha;
        GUI.color = nColour;

        if (damageImage != null)
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), damageImage, ScaleMode.StretchToFill);
    }

    //Check
    private void CheckGrab()
    {
        int layerMask = 1 << 8;
        RaycastHit nearbyPoint;

        float searchRadius = 1f;
        float searchHeight = 1f;

        if (Physics.SphereCast(transform.position, searchRadius, Vector3.up, out nearbyPoint, searchRadius + searchHeight, layerMask, QueryTriggerInteraction.Collide))
        {

            grabbedPoint = nearbyPoint.point;

            Vector3 between = grabbedPoint - transform.position;

            if (between.y > 1.75)
            {
                grabbing = true;

                forwardAxis = -(nearbyPoint.transform.forward);
                lastGrabForward = -(nearbyPoint.transform.forward);

                rightAxis = -(nearbyPoint.transform.right);

                if (nearbyPoint.collider.tag == "PullUp")
                    pullUp = true;
                else
                    pullUp = false;
            }
            else
            {
                grabbing = false;
            }

        }
        else
        {
            grabbing = false;
        }
    }

    //Start Crouching
    private void StartCrouch()
    {
        StartCoroutine("startCrouch", 1f);
    }
    private void StartCrouch(float scale)
    {
        StartCoroutine("startCrouch",scale);
    }
    private IEnumerator startCrouch(float scale)
    {
        crouching = true;
        float startTime = Time.time;

        float startHeight = GetComponent<CapsuleCollider>().height;
        float startCamHeight = headCam.transform.localPosition.y;

        float stoppingTime = ((startHeight - 1f) * crouchTime) * scale;
        Debug.Log("scale:" + scale + " stoppingTime:" + stoppingTime);

        while (Time.time - startTime < stoppingTime)
        {
            GetComponent<CapsuleCollider>().height = Mathf.Lerp(startHeight, 1f, (Time.time - startTime) / stoppingTime);

            Vector3 camPos = headCam.transform.localPosition;
            camPos.y = Mathf.Lerp(startCamHeight, 0.25f, (Time.time - startTime) / stoppingTime);
            headCam.transform.localPosition = camPos;

            yield return null;
        }

        GetComponent<CapsuleCollider>().height = 1f;
        Vector3 nCamPos = headCam.transform.localPosition;
        nCamPos.y = 0.25f;
        headCam.transform.localPosition = nCamPos;

        badLanding = false;

    }

    //Stop Crouching
    private IEnumerator StopCrouch()
    {
        float startTime = Time.time;

        float startHeight = GetComponent<CapsuleCollider>().height;
        float startCamHeight = headCam.transform.localPosition.y;

        float stoppingTime = (2f - startHeight) * crouchTime;

        while (Time.time - startTime < stoppingTime)
        {
            GetComponent<CapsuleCollider>().height = Mathf.Lerp(startHeight, 2f, (Time.time - startTime) / stoppingTime);

            Vector3 camPos = headCam.transform.localPosition;
            camPos.y = Mathf.Lerp(startCamHeight, 0.75f, (Time.time - startTime) / stoppingTime);
            headCam.transform.localPosition = camPos;

            yield return null;
        }

        GetComponent<CapsuleCollider>().height = 2f;
        Vector3 nCamPos = headCam.transform.localPosition;
        nCamPos.y = 0.75f;
        headCam.transform.localPosition = nCamPos;

        crouching = false;
    }

    //Take Damage
    private IEnumerator TakeDamage(float percent)
    {  
        float startTime = Time.time;
        float totalTime = 0.25f;

        while (Time.time - startTime < totalTime)
        {
            damageAlpha = Mathf.Lerp(0f, percent, (Time.time - startTime) / totalTime);
            yield return null;
        }
        
        damageAlpha = percent;
        startTime = Time.time;

        while (Time.time - startTime < totalTime)
        {
            damageAlpha = Mathf.Lerp(percent, 0f, (Time.time - startTime) / totalTime);
            yield return null;
        }
        damageAlpha = 0f;


        badLanding = false;

    }
}
