using UnityEngine;

public class functionDemo : MonoBehaviour
{
    public int numberOne = 45;
    public int numberTwo = 183;
    public float speed = 0.05f;
    public float scale = 1f;
    public int addNumbers (int addendOne, int addendTwo)
    {
        int sum = addendOne + addendTwo;
        return sum;
    }

    public void moveTransform()
    {
        transform.position = new Vector2(transform.position.x + speed, transform.position.y);
    }

    public void transformScale()
    {
        transform.localScale = new Vector3(transform.localScale.x + scale, transform.localScale.y, transform.localScale.z);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        print(addNumbers(numberOne, numberTwo));

        transformScale();
    }

    // Update is called once per frame
    void Update()
    {
        moveTransform();
        //numberOne = addNumbers(numberOne, numberTwo);
        //print(addNumbers(numberOne, numberTwo));
    }
}
