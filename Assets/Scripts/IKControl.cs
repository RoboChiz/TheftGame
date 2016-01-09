using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))] 

public class IKControl : MonoBehaviour {

    protected Animator animator;

    public bool ikActive = false;
    public Transform leftTarget = null;
    public Transform rightTarget = null;
	void Start () 
    {
        animator = GetComponent<Animator>();
    }
    
    //a callback for calculating IK
    void OnAnimatorIK()
    {
        if (animator)
        {
            //if the IK is active, set the position and rotation directly to the goal. 
            if (ikActive)
            {
                // Set the right hand target position and rotation, if one has been assigned
                if (leftTarget != null)
                {
                    animator.SetIKPositionWeight(AvatarIKGoal.LeftHand,1);
                    animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
                    animator.SetIKPosition(AvatarIKGoal.LeftHand, leftTarget.position);
                    animator.SetIKRotation(AvatarIKGoal.LeftHand, leftTarget.rotation);
                }

                // Set the right hand target position and rotation, if one has been assigned
                if (rightTarget != null)
                {
                    animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
                    animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
                    animator.SetIKPosition(AvatarIKGoal.RightHand, rightTarget.position);
                    animator.SetIKRotation(AvatarIKGoal.RightHand, rightTarget.rotation);
                }     
                
            }
            
            //if the IK is not active, set the position and rotation of the hand and head back to the original position
            else 
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0); 
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand,0);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand,0); 
                animator.SetLookAtWeight(0);
            }
        }
    }
}
