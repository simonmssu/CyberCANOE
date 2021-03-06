using UnityEngine;

/* 
This is an omnidirectional navigation script for CyberCANOE that only uses a single wand to navigate and orient in space.
To use it add this script to the CC_CANOE game object. 
Make sure that the CCaux_Navigator script is not also enabled as the two will conflict with each other.
  
To actually use the navigation scheme you press and hold the chosen wand button. Then as you move the wand in space you will
travel in the direction of that movement. The farther away from the initial point in space where you pressed the button, the faster you move.
Likewise if you tilt your wand in any of the 3 axes, you will begin to incrementally rotate.
  
By pressing and holding the chosen reset navigation button, navigation will gradually return to the initial position and orientation. 
Initial positions and orientations can be set manually via the scripting interface.
 
Note: this script depends on the Character Controller being present in CC_CANOE so don't remove it.
You may also want to turn off gravity in CC_CANOE if you want to be able to go up.

CyberCANOE Virtual Reality API for Unity3D
(C) 2016 Ryan Theriot, Jason Leigh, Laboratory for Advanced Visualization & Applications, University of Hawaii at Manoa.
Version: 1.13, May 17th, 2017.
 */

public class CCaux_OmniNavigator : MonoBehaviour
{

    public enum rotationLock { None, X, Y, Z };

    [Header("Navigation Settings")]
    [Tooltip("Enable Navigation")]
    public bool enableNavigation = true;
    [Tooltip("Disable Movement in X.")]
    public bool disableNavigationX = false;
    [Tooltip("Disable Movement in Y.")]
    public bool disableNavigationY = false;
    [Tooltip("Disable Movement in Z.")]
    public bool disableNavigationZ = false;
    [Tooltip("Lock rotation about an axis.")]
    public rotationLock lockRotation = rotationLock.None;
    [Tooltip("Wand to use.")]
    public Wand wandToUse = Wand.Left;
    [Tooltip("The button you wish to navigate with.")]
    public WandButton navButton;
    [Tooltip("Enable this if you wish to navigate with the wand trigger. This overrides the button selection above.")]
    public bool navWithTrigger;
    [Tooltip("Movement speed.")]
    public float moveSpeed = 5f;
    [Tooltip("Rotation speed.")]
    public float rotateSpeed = 5f;
    [Tooltip("The button to reset navigate with (do not choose same button as the one for setting).")]
    public WandButton resetButton;
    [Tooltip("Reset navigation speed.")]
    public float resetSpeed = 0.5f;
    [Tooltip("Reset navigation position.")]
    public Vector3 resetPosition = new Vector3(0, 0, 0);
    [Tooltip("Reset navigation rotation.")]
    public Vector3 resetRotation = new Vector3(0, 0, 0);
    [Tooltip("Object to show (attached to wand) when in navigation mode.")]
    public GameObject cursor;
    private CC_CANOE.WandModel savedWandModel;

    bool doNav = false;
    Vector3 startPosition = new Vector3();
    Quaternion startRotation = Quaternion.identity;
    bool doneNav = false;
    private CharacterController charCont;
    Quaternion resetAngle;



    void Start()
    {
        charCont = CC_CANOE.CanoeCharacterController();
        resetAngle = Quaternion.Euler(resetRotation);
        savedWandModel = CC_CANOE.CanoeGameObject().GetComponent<CC_CANOE>().wandModel;
    }

    void Update()
    {
        if (!enableNavigation)
            return;

        //If we are using the trigger button to navigate check if it is pressed.
        if (navWithTrigger)
        {
            if (CC_INPUT.GetAxis(wandToUse, WandAxis.Trigger) > 0.0f)
                doNav = true;
            else
                doNav = false;
        }
        else
        {           //Otherwise check the chosen wand button
            doNav = CC_INPUT.GetButtonPress(wandToUse, navButton);
        }

        if ((resetButton == navButton) && (!navWithTrigger))
        {
            print("CCaux_OmniNavigator Warning: Chosen Navigation and Reset Navigation buttons are the same.");
        }

        if (CC_INPUT.GetButtonPress(wandToUse, resetButton))
        {
            charCont.transform.position = Vector3.Slerp(charCont.transform.position, resetPosition, resetSpeed * Time.deltaTime);
            charCont.transform.rotation = Quaternion.Slerp(charCont.transform.rotation, resetAngle, resetSpeed * Time.deltaTime);
        }

        if (!doNav)
        {
            doneNav = false;
            CC_CANOE.CanoeGameObject().GetComponent<CC_CANOE>().wandModel = savedWandModel;
            cursor.SetActive(false);
        }

        if (doNav)
        {
            // If wand button pressed the first time then record the starting position and orientation of the wand
            if (doneNav == false)
            {
                startPosition = CC_CANOE.WandGameObject(wandToUse).transform.localPosition;

                doneNav = true;
                startRotation = CC_CANOE.WandGameObject(wandToUse).transform.localRotation;

            }
            else
            {

                // Then at each time check the difference between new and old wand position as well as new and old wand orientation.
                // Apply that difference to the character controller to effect navigation.
                Vector3 movement = CC_CANOE.WandGameObject(wandToUse).transform.localPosition - startPosition;

                // If disable navigation in a particular axis is enabled then set movement values to zero.
                if (disableNavigationX) movement.x = 0;
                if (disableNavigationY) movement.y = 0;
                if (disableNavigationZ) movement.z = 0;

                movement = gameObject.transform.localRotation * movement; // Movement must take into account current orientation of CyberCANOE


                charCont.Move(movement * Time.deltaTime * moveSpeed);

                Quaternion newRotation = CC_CANOE.WandGameObject(wandToUse).transform.localRotation;

                // Check if a rotation lock is enabled and handle it
                float axisLockAngle;
                Quaternion rotator = new Quaternion();

                switch (lockRotation)
                {
                    case rotationLock.X:
                        axisLockAngle = newRotation.eulerAngles.x;
                        rotator.eulerAngles = new Vector3(axisLockAngle, 0, 0);
                        startRotation.eulerAngles = new Vector3(startRotation.eulerAngles.x, 0, 0);
                        break;
                    case rotationLock.Y:
                        axisLockAngle = newRotation.eulerAngles.y;
                        rotator.eulerAngles = new Vector3(0, axisLockAngle, 0);
                        startRotation.eulerAngles = new Vector3(0, startRotation.eulerAngles.y, 0);
                        break;
                    case rotationLock.Z:
                        axisLockAngle = newRotation.eulerAngles.z;
                        rotator.eulerAngles = new Vector3(0, 0, axisLockAngle);
                        startRotation.eulerAngles = new Vector3(0, 0, startRotation.eulerAngles.z);
                        break;
                    default:
                        rotator = newRotation;
                        break;
                }

                charCont.transform.localRotation = charCont.transform.localRotation * Quaternion.Slerp(Quaternion.identity, Quaternion.Inverse(startRotation * Quaternion.Inverse(rotator)), Time.deltaTime * rotateSpeed);

                // If there is a cursor object then orient it with the wand position.
                if (cursor)
                {
                    CC_CANOE.CanoeGameObject().GetComponent<CC_CANOE>().wandModel = CC_CANOE.WandModel.None;
                    cursor.SetActive(true);
                    cursor.transform.position = CC_CANOE.WandGameObject(wandToUse).transform.position;
                    cursor.transform.rotation = CC_CANOE.WandGameObject(wandToUse).transform.rotation;
                }

            }
        }


    }
}
