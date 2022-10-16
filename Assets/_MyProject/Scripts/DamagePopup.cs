using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    private float _startTime;
    private float _endTime;

    // Start is called before the first frame update
    void Start()
    {
        _startTime = Time.time + 0.4f;      // Start after the attack animation
        _endTime = _startTime + 1;
    }

    // Update is called once per frame
    void Update()
    {
        if (_endTime <= Time.time)
        {
            Destroy(gameObject);
        }
        else if (_startTime <= Time.time)
        {
            transform.Translate(Vector3.up * Time.deltaTime);
        }
    }
}
