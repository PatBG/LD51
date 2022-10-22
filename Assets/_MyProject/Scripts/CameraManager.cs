using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class CameraManager : MonoBehaviour
{
    public GameObject Camera;
    public float SpeedMovement = 1;
    public float SpeedMovementAuto = 0.5f;
    public float MinHeight = 1;
    public float MaxHeight = 10;
    public float SpeedZoom = 1;

    private static Vector3 _target;
    private static bool _hasTarget = false;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 movement = new(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        if (movement != Vector3.zero)
        {
            transform.Translate(SpeedMovement * Time.deltaTime * movement);
            _hasTarget = false;                 // Cancel target when manually move
        }
        else if (_hasTarget)
        {
            transform.position = Vector3.MoveTowards(transform.position, _target, (SpeedMovementAuto * Time.deltaTime));
            if (transform.position == _target)
            {
                _hasTarget = false;             // Canceltarget when its position is reached
            }
        }

        float zoom = Input.GetAxis("Mouse ScrollWheel");
        Vector3 zoomVector3 = SpeedZoom * Time.deltaTime * zoom * Vector3.forward;
        Camera.transform.Translate(zoomVector3);
        if (Camera.transform.position.y < MinHeight || Camera.transform.position.y > MaxHeight)
        {
            Camera.transform.Translate(-zoomVector3);
        }
    }

    public static void SetTileTarget(Tile tile)
    {
        _target = tile.GetPosition();
        _hasTarget = true;
        //Debug.Log("Automatic camera go to tile: " + tile + "\r\n");
    }
}
