using UnityEngine;
using UnityEngine.UI;

public class UI_ArrowButton : MonoBehaviour
{
    public string direction; // nutDown, nutLeft...

    public void Press()
    {
        FindObjectOfType<DotSkillManager>().OnButtonPress(direction);
    }
}
