using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Hider : MonoBehaviour
{
    public Button button;
    public Button collapsedButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        collapsedButton.onClick.AddListener(Expand);
        button.onClick.AddListener(Collapse);
    }

    // Update is called once per frame
    void Collapse()
    {
        button.gameObject.SetActive(false);
        collapsedButton.gameObject.SetActive(true);
    }

    void Expand()
    {
        button.gameObject.SetActive(true);
        collapsedButton.gameObject.SetActive(false);
    }
}
