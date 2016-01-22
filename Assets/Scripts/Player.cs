using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{

    public float ledgeSpeed = 1f;
    public float walkSpeed = 2f;
    public float runSpeed = 7f;

    private Vector3 moveForward;
    private Vector3 moveRight;

    private float currentSpeed = 0f;
    private float speedChange = 3.5f;

    public float jumpForce = 10f;
    private bool jumpLock = false;

    private bool jumpOwed;
    private bool pullUpNeeded;

    public bool isFalling = true;
    private bool pullingUp;

    public float crouchTime = 2f;
    private bool crouching = false;
    private bool stoppingCrouch = false;

    public float maxLedgeDistance = 0.7f; //Maximum distance a player can be from a ledge point.

    public float maxLedgeHeight = 1f; //How high a ledge can be before it'll be grabbed - 2f
    public float searchRadius = 1f; //How big the search sphere is
    public float hangHeight = 1.25f; //Height that the play hangs from

    public Transform armsAnimator;

    public Transform leftHandTransform;
    private Arm left;
    private Vector3 lastLeftHand;

    public Transform rightHandTransform;
    private Arm right;
    private Vector3 lastRightHand;

    public MouseLook headCam;
    public bool grabbing
    {
        get
        {
            return vGrabbing;
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

                left.CancelHand();
                right.CancelHand();
            }

            vGrabbing = value;
        }
    }
    private bool vGrabbing = false;
    public Vector3 grabbedPoint;

    // Use this for initialization
    void Start()
    {
        left = new Arm(leftHandTransform, true);
        right = new Arm(rightHandTransform, false);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //Get Input
        float hori = Input.GetAxis("Horizontal");
        float verti = Input.GetAxis("Vertical");

        Vector3 input = new Vector3(hori, 0, verti);

        if (!grabbing || Vector3.Angle(moveForward, transform.forward) > 45)
        {
            input = transform.TransformDirection(input);
        }
        else
        {
            input = (hori * moveRight) + (verti * moveForward);
        }

        input.Normalize();

        RaycastHit hit;
        bool hitSomething = Physics.Raycast(transform.position, input, out hit, 1f);

        //Determine if the player is running or not
        float moveSpeed = 0;
        if (grabbing || crouching)
            moveSpeed = ledgeSpeed;
        else if (Input.GetAxis("Sprint") != 0 && !hitSomething)
            moveSpeed = runSpeed;
        else
            moveSpeed = walkSpeed;

        /*currentSpeed = Mathf.Lerp(currentSpeed, moveSpeed, Time.fixedDeltaTime * speedChange);

        //If input is not (0,0,0)
        if (hori != 0 || verti != 0)
            input *= currentSpeed * Time.fixedDeltaTime;
        else
        {
            Vector3 StopVar = new Vector3(0, GetComponent<Rigidbody>().velocity.y, 0);
            GetComponent<Rigidbody>().velocity = StopVar; //Lazy fix to stop sliding
        }

        GetComponent<Rigidbody>().MovePosition(transform.position + input);
        */

        input *= moveSpeed;

        // Apply a force that attempts to reach our target velocity
        Vector3 velocity = GetComponent<Rigidbody>().velocity;
        Vector3 velocityChange = (input - velocity);
        velocityChange.x = Mathf.Clamp(velocityChange.x, -moveSpeed, moveSpeed);
        velocityChange.z = Mathf.Clamp(velocityChange.z, -moveSpeed, moveSpeed);
        velocityChange.y = 0;
        GetComponent<Rigidbody>().AddForce(velocityChange, ForceMode.VelocityChange);

        if (!isFalling || jumpOwed)
        {
            //Jump
            if ((Input.GetAxis("Jump") != 0 || jumpOwed) && !jumpLock)
            {
                jumpLock = true;

                Vector3 vel = GetComponent<Rigidbody>().velocity;
                vel.y = jumpForce;

                if (pullUpNeeded)
                {
                   // vel.y *= 1.15f;
                    StartCoroutine("StartCrouch");
                }


                GetComponent<Rigidbody>().velocity = vel;
                jumpOwed = false;
                pullUpNeeded = false;
            }
        }

    }

    void Update()
    {
        Debug.DrawLine(transform.position, transform.position + (moveForward * 2f), Color.red);
        Debug.DrawLine(transform.position, transform.position + (moveRight * 2f), Color.yellow);
        //Do Arms
        left.Update();
        right.Update();

        left.SetHandRotation(Quaternion.LookRotation(Vector3.up, -moveForward));
        right.SetHandRotation(Quaternion.LookRotation(Vector3.up, -moveForward));

        //Calculate isFalling
        if (Physics.Raycast(transform.position, Vector3.down, 1.25f))
        {
            isFalling = false;
            pullingUp = false;
        }
        else
        {
            isFalling = true;
        }

        //Falling
        if (isFalling && !jumpOwed)
        {
            //Climbing Mechanics
            if (Input.GetAxis("Grab") != 0 && !grabbing && GetComponent<Rigidbody>().velocity.y < 0)
            {
                CheckGrab();
            }
        }

        if (Input.GetAxis("Jump") == 0)
            jumpLock = false;

        //Crouching
        if (Input.GetAxis("Crouch") != 0 && !crouching && !grabbing)
        {
            StartCoroutine("StartCrouch");
        }

        RaycastHit headPain;
        bool hitHead = Physics.SphereCast(transform.position, 0.4f, Vector3.up, out headPain, 1.5f);

       //Stop Croutching
        if (crouching && ((Input.GetAxis("Crouch") == 0 && !hitHead && !stoppingCrouch && !pullingUp) || grabbing))
        {
            StopCoroutine("StartCrouch");
            StartCoroutine("StopCrouch");
        }

        //Grabbing Mechanics
        if (grabbing)
        {
            armsAnimator.parent = null;
            armsAnimator.position = transform.position + new Vector3(0f, 0.6f, 0f);
            armsAnimator.rotation = Quaternion.LookRotation(moveForward,Vector3.up);

            //Update Hand Positions
            UpdateHands();

            //Cancel Grab
            Vector3 between = grabbedPoint - transform.position;

            //Adjust Y Position
            if (between.y - Time.deltaTime > hangHeight)
            {
                Vector3 tp = transform.position;
                tp.y += Time.deltaTime;
                transform.position = tp;
            }

            if (Input.GetAxis("Grab") == 0)
                grabbing = false;

            if (Mathf.Abs(between.x) > maxLedgeDistance || Mathf.Abs(between.z) > maxLedgeDistance)
            {
                CheckGrab();
            }

            if (Input.GetAxis("Jump") != 0 && !jumpLock)
            {
                grabbing = false;
                pullingUp = true;
                jumpOwed = true;

            }
        }
        else
        {
            armsAnimator.parent = transform;
            armsAnimator.rotation = Quaternion.Lerp(armsAnimator.rotation,transform.rotation,Time.deltaTime * 5f);

            moveForward = transform.forward;
            moveRight = transform.right;

        }

    }

    private void UpdateHands()
    {
        int layerMask = 1 << 8;
        //RaycastHit[] nearbyPoints = Physics.SphereCastAll(transform.position, searchRadius, Vector3.up, maxLedgeHeight, layerMask, QueryTriggerInteraction.Collide);

        //if (nearbyPoints != null && nearbyPoints.Length > 0)
       // {
        Vector3 halfRight = moveRight / 2f;
        Vector3 handSize = moveRight / 10f;
        RaycastHit chosenLeft, chosenRight;

        bool leftCatch, rightCatch;

        leftCatch = Physics.SphereCast(transform.position - halfRight, searchRadius, Vector3.up, out chosenLeft, maxLedgeHeight, layerMask, QueryTriggerInteraction.Collide);
        rightCatch = Physics.SphereCast(transform.position + halfRight, searchRadius, Vector3.up, out chosenRight, maxLedgeHeight, layerMask, QueryTriggerInteraction.Collide);

        if(leftCatch)
        {
            lastLeftHand = chosenLeft.point;
            left.SetHandPosition(chosenLeft.point - handSize);
        }
        else
        {
            if(rightCatch)
            {
                lastLeftHand = chosenRight.point;
                left.SetHandPosition(chosenRight.point - handSize);
            }
        }
        
        if (rightCatch)
        {
            lastRightHand = chosenRight.point;
            right.SetHandPosition(chosenRight.point + handSize);
        }
        else
        {
            if (leftCatch)
            {
                lastRightHand = chosenLeft.point;
                right.SetHandPosition(chosenLeft.point + handSize);
            }
        }

        //RaycastHit chosenLeft = NearestPoint(nearbyPoints, transform.position - halfRight);
        //RaycastHit chosenRight = NearestPoint(nearbyPoints, transform.position + halfRight);

       // }
    }

    private void CheckGrab()
    {
        int layerMask = 1 << 8;
        RaycastHit[] nearbyPoints = new RaycastHit[1];

        if (Physics.SphereCast(transform.position, searchRadius, Vector3.up, out nearbyPoints[0], maxLedgeHeight, layerMask, QueryTriggerInteraction.Collide))
        {
            RaycastHit chosen = NearestPoint(nearbyPoints, transform.position);

            grabbedPoint = chosen.point;

            Vector3 between = chosen.point - transform.position;
            if ((Mathf.Abs(between.x) < maxLedgeDistance && Mathf.Abs(between.z) < maxLedgeDistance) && between.y > hangHeight)
            {
                grabbing = true;
                moveForward = -(chosen.transform.forward);
                moveRight = -(chosen.transform.right);

                if (chosen.collider.tag == "PullUp")
                    pullUpNeeded = true;
                else
                    pullUpNeeded = false;

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

    private RaycastHit NearestPoint(RaycastHit[] nearbyPoints, Vector3 checkPoint)
    {
        RaycastHit chosen = nearbyPoints[0];
        Vector3 between = chosen.point - checkPoint;
        float minDistance = between.sqrMagnitude;

        for (int i = 1; i < nearbyPoints.Length; i++)
        {
            Vector3 nVector = nearbyPoints[i].point - checkPoint;
            if (nVector.sqrMagnitude < minDistance)
            {
                chosen = nearbyPoints[i];
                minDistance = nVector.sqrMagnitude;
            }
        }

        return chosen;
    }

    private IEnumerator StartCrouch()
    {
        crouching = true;
        float startTime = Time.time;

        float startHeight = GetComponent<CapsuleCollider>().height;
        float startCamHeight = headCam.transform.localPosition.y;

        float stoppingTime = (startHeight - 1f) * crouchTime;

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

    }

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
        stoppingCrouch = false;
    }


}

public class Arm
{
    private Transform hand;
    private Transform foreArm; //Connect to Hand and Up Arm
    private Transform bicep;//Connect to Low Arm and Player

    private Transform parent;

    private Vector3 newHandPosition;
    private Quaternion newHandRotation;
    
    private static float handSpeed = 10f;
    private bool left;
    private bool goHome;

    public Arm(Transform nHand, bool leftQuestion)
    {
        hand = nHand;

        left = leftQuestion;
        parent = nHand.parent;

        newHandRotation = hand.rotation;

        CancelHand();
    }
    public void SetHandPosition(Vector3 position)
    {
        newHandPosition = position;     
        goHome = false;
    }

    public void SetHandRotation(Quaternion rotation)
    {
        newHandRotation = rotation;
    }

    public void CancelHand()
    {
        goHome = true;
    }

    public void Update()
    {
        if (!goHome)
        {
            hand.position = Vector3.Lerp(hand.position, newHandPosition, Time.deltaTime * handSpeed);
            hand.parent = null;
        }
        else
        {
            hand.parent = parent;

            float modifier = 1f;
            if (left)
                modifier = -1f;

            hand.localPosition = Vector3.Lerp(hand.localPosition, new Vector3(0.6f * modifier, 0.5f, -0.25f), Time.deltaTime * handSpeed);

        }

        hand.rotation = Quaternion.Lerp(hand.rotation, newHandRotation, Time.deltaTime * handSpeed);
    }
}
