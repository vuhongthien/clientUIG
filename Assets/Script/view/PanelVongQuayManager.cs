using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PanelVongQuayManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject panelVongQuay;
    public GameObject panelKhamHT;
    
    [Header("Buttons")]
    public Button btnEvent4;
    public Button btnClosePanelVongQuay;
    public Button btnOpenPanelHT;
    public Button btnClosePanelKhamHT;

    void Start()
    {
        // Ẩn các panel khi bắt đầu
        panelVongQuay.SetActive(false);
        panelKhamHT.SetActive(false);
        
        // Gán sự kiện cho các button
        btnEvent4.onClick.AddListener(OpenPanelVongQuay);
        btnClosePanelVongQuay.onClick.AddListener(ClosePanelVongQuay);
        btnOpenPanelHT.onClick.AddListener(OpenPanelKhamHT);
        btnClosePanelKhamHT.onClick.AddListener(ClosePanelKhamHT);
    }

    void OpenPanelVongQuay()
    {
        panelVongQuay.SetActive(true);
    }

    void ClosePanelVongQuay()
    {
        panelVongQuay.SetActive(false);
        
        // Đảm bảo đóng PanelKhamHT nếu nó đang mở
        if (panelKhamHT.activeSelf)
        {
            panelKhamHT.SetActive(false);
        }
    }

    void OpenPanelKhamHT()
    {
        panelKhamHT.SetActive(true);
    }

    void ClosePanelKhamHT()
    {
        panelKhamHT.SetActive(false);
    }

    void OnDestroy()
    {
        // Hủy các listener khi destroy object
        if (btnEvent4 != null) btnEvent4.onClick.RemoveListener(OpenPanelVongQuay);
        if (btnClosePanelVongQuay != null) btnClosePanelVongQuay.onClick.RemoveListener(ClosePanelVongQuay);
        if (btnOpenPanelHT != null) btnOpenPanelHT.onClick.RemoveListener(OpenPanelKhamHT);
        if (btnClosePanelKhamHT != null) btnClosePanelKhamHT.onClick.RemoveListener(ClosePanelKhamHT);
    }
}