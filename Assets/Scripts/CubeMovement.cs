using UnityEngine;


public class CubeMovement : MonoBehaviour
{
    Rigidbody rigidbody;
    public Vector3 currentForce;
    public float movementTime = 0;
    // Start is called before the first frame update
    public void Initialize()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    public bool ifDirectionHaveToChange()
    {
        return rigidbody.velocity.magnitude > 30 || movementTime <= 0;
    }
    public Vector3 getVelocity(){
        return rigidbody.velocity;
    }
    public void changeDirection(Vector3 newForce, float newMovementTime){
        currentForce = newForce;
        movementTime = newMovementTime;
    }
    
    void FixedUpdate()
    {
        movementTime = movementTime - 1; 
        rigidbody.AddForce(currentForce.x, 0, currentForce.z);
    }
}
