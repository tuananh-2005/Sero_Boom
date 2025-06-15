using UnityEngine;

public class CloudMover : MonoBehaviour
{
    private float speedX;
    private float lifeTime;

    public void Init(float speedX, float lifeTime)
    {
        this.speedX = speedX;
        this.lifeTime = lifeTime;

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(Vector3.right * speedX * Time.deltaTime);
    }
}

