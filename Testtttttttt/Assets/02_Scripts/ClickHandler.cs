using Kirurobo;
using UnityEngine;
using static Kirurobo.UniWindowController; // UniWindowController 사용 시

public class ClickHandler : MonoBehaviour
{
    public UniWindowController uwc;

    public ChzzkAuthManager data;
    void Start()
    {
      
    }

    void Update()
    {
        // 마우스가 큐브 위에 있는지 확인
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit) && hit.transform == this.transform)
        {
            // 큐브 위에 마우스가 있으면 클릭 관통 끔 (클릭 가능)
            uwc.isClickThrough = false;
        }
        else
        {
            // 큐브 밖이면 다시 클릭 관통 켬 (뒤쪽 화면 클릭 가능)
            uwc.isClickThrough = true;
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
          
        }
        if (Input.GetKey(KeyCode.A))
        {
            this.transform.position += (-Vector3.right)*Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.D))
        {
            this.transform.position += Vector3.right * Time.deltaTime;
        }
    } 

    void OnMouseDown()
    {
        // 큐브 클릭 시 색상 변경
       
    }  
}  