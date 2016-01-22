using UnityEngine;
using System.Collections;

[AddComponentMenu("Camera-Control/Mouse Look")] //Allows script to be found easily

public class MouseLook : MonoBehaviour
{

    public enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 } //Used to seperate axes that can be rotated
    public RotationAxes axes = RotationAxes.MouseXAndY;

    public static float sensitivity = 3f;//Sensitivity of rotation, static so it is the same across all scripts

    public float minimumX = -360;
    public float maximumX = 360;

    public float minimumY = -60;
    public float maximumY = 60;

    static float rotationX = 0;
    static float rotationY = 0;

    private Quaternion startRotation;

    public static bool hideCursor = true;
    public bool lockInput;

    // Use this for initialization
    void Start()
    {
        startRotation = transform.localRotation;
    }

    // Update is called once per frame
    void Update()
    {
        Cursor.visible = !hideCursor;

        if (hideCursor)
            Cursor.lockState = CursorLockMode.Locked;
        else
            Cursor.lockState = CursorLockMode.None;

        float inputX = 0;
        float inputY = 0;

        inputX += Input.GetAxis("Mouse X");
        inputX += Input.GetAxis("RSHorizontal") * Time.deltaTime * 50f;

        inputY += Input.GetAxis("Mouse Y");
        inputY += Input.GetAxis("RSVertical") * Time.deltaTime * 50f;

        if (hideCursor)
        {
            if (axes == RotationAxes.MouseXAndY)
            {
                if (!lockInput)
                {
                    rotationX += inputX * sensitivity;
                    rotationY += inputY * sensitivity;
                }

                rotationX = ClampAngle(rotationX, minimumX, maximumX);
                rotationY = ClampAngle(rotationY, minimumY, maximumY);

                Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
                Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, Vector3.left);

                transform.localRotation = startRotation * xQuaternion * yQuaternion;
            }
            else if (axes == RotationAxes.MouseX)
            {
                if (!lockInput)
                    rotationX += inputX * sensitivity;

                rotationX = ClampAngle(rotationX, minimumX, maximumX);

                Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
                transform.localRotation = startRotation * xQuaternion;
            }
            else
            {
                if (!lockInput)
                    rotationY += inputY * sensitivity;

                rotationY = ClampAngle(rotationY, minimumY, maximumY);

                Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, Vector3.left);
                transform.localRotation = startRotation * yQuaternion;
            }


        }

    }

    static float ClampAngle(float angle, float min, float max)
    {
        while(angle < -360f)
            angle += 360f;
        while(angle > 360f)
            angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }
}
