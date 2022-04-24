using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class WebbyBebby : MonoBehaviour
{
    //Camera control variables
    public float cameraSensitivity = 25f;
    private float xRotCamera;
    Vector2 lookValue;

    //Movement variables
    public float moveSpeed;
    Vector2 moveValue;

    //Web variables
    private LineRenderer webstring;
    private Vector3 grapplePoint;
    public LayerMask grappleable;
    public Transform shootPosRef, playerCamera, player, webshooter;
    public float maxWebDistance = 50f;
    private SpringJoint joint;
    bool isSwinging = false;

    //Hand rotation
    private Quaternion desiredRotation;
    private float rotationSpeed = 5f;

    // On awake
    void Awake()
    {
        webstring = GetComponent<LineRenderer>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
 
    private void Update()
    {
        // Camera controls
        Lookaround();        

        // Forward/Backwards movement during swing
        if (moveValue.y != 0 && isSwinging)
            player.gameObject.GetComponent<Rigidbody>().AddForce(playerCamera.forward * moveValue.y * moveSpeed * Time.deltaTime);

        // Left/Right movement during swing            
        if(moveValue.x != 0 && isSwinging)
            player.gameObject.GetComponent<Rigidbody>().AddForce(playerCamera.right * moveValue.x * moveSpeed * Time.deltaTime);

        // Break line if distance is too long
        if (Vector3.Distance(player.position, grapplePoint) > maxWebDistance*1.3f && isSwinging)
            StopWebslinging();

        //Rotate hand towards grapplepoint if there is a spring component, else reset to parents rotation (Zero out)        
        if (joint)
            desiredRotation = Quaternion.LookRotation(grapplePoint - webshooter.position);
        else
            desiredRotation = webshooter.parent.rotation;

        //Update hand rotation
        webshooter.rotation = Quaternion.Lerp(webshooter.rotation, desiredRotation, Time.deltaTime * rotationSpeed);

    }

    // LastUpdate is called once per frame at the end
    void LateUpdate()
    {
        // Calls draw web function below
        DrawWeb();
    }

    // Input value from mouse 
    public void Look(InputAction.CallbackContext context)
    {
        lookValue = new Vector2(context.ReadValue<Vector2>().x, context.ReadValue<Vector2>().y);
    }

    // Input values from WASD
    public void Move(InputAction.CallbackContext context)
    {
        moveValue = new Vector2(context.ReadValue<Vector2>().x, context.ReadValue<Vector2>().y);        
    }

    // Input value from mouse 
    public void Interact(InputAction.CallbackContext context)
    {
        print("yeet");
    }



    private void Lookaround()
    {
        float yRotCamera;
        float mouseX = lookValue.x * cameraSensitivity * Time.fixedDeltaTime;
        float mouseY = lookValue.y * cameraSensitivity * Time.fixedDeltaTime;

        //Find current look rotation
        Vector3 rot = playerCamera.transform.localRotation.eulerAngles;

        // Adds the mouse x value as rotation on the camera (left/right rotation around the cameras Y axis)
        yRotCamera = rot.y + mouseX;

        //Rotate, and also make sure we dont over- or under-rotate.
        xRotCamera -= mouseY;
        //Clamps the up and down rotation between -90 and 90 degrees to avoid 
        xRotCamera = Mathf.Clamp(xRotCamera, -90f, 90f);

        //Rotate camera 
        playerCamera.transform.localRotation = Quaternion.Euler(xRotCamera, yRotCamera, 0);
        transform.localRotation = Quaternion.Euler(0, yRotCamera, 0);
    }



    // Input method for calling start and stop on press and release of mouse button
    public void Swing(InputAction.CallbackContext context)
    {        
        if (context.started)
            StartWebslinging();
        else if (context.canceled)
            StopWebslinging();
    }

    // Initiate swinging
    void StartWebslinging()
    {
        Debug.Log("Start Swing");        

        RaycastHit hit;
        // Check if hit anything with raycast from web shooting transform 
        if(Physics.Raycast(shootPosRef.position, shootPosRef.forward, out hit, maxWebDistance))
        {            

            grapplePoint = hit.point;
            joint = player.gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = shootPosRef.position;
            joint.connectedAnchor = grapplePoint;
            gameObject.GetComponent<AudioSource>().Play();
            
            //Spring Strength
            joint.spring = 0;
            joint.damper = 0;
            joint.massScale = 3f;

            //set position amount, one for line render start and end           
            webstring.positionCount = 2;
            isSwinging = true;
        }
    }

    // Draw web
    void DrawWeb()
    {
        if (!joint) return;
        webstring.SetPosition(0, shootPosRef.position);
        webstring.SetPosition(1, grapplePoint);
    }

    // Stop Webslinging
    void StopWebslinging()
    {        
        Debug.Log("Stop Swing");
        webstring.positionCount = 0;
        Destroy(joint);
        isSwinging = false;
    }

    //Reset game
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Respawn"))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }    
}
