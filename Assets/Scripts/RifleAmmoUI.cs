using TMPro; // remove this and use UnityEngine.UI.Text if you use legacy Text
using UnityEngine;

public class RifleAmmoUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Rifle rifle;

    [SerializeField]
    private TextMeshProUGUI ammoText; // or Text if not using TMP

    private void Awake()
    {
        if (!rifle)
            rifle = Object.FindFirstObjectByType<Rifle>();
    }

    private void OnEnable()
    {
        if (rifle != null)
        {
            rifle.OnAmmoChanged += RefreshAmmoText;
        }

        RefreshAmmoText(); // make sure it shows correct value immediately
    }

    private void OnDisable()
    {
        if (rifle != null)
        {
            rifle.OnAmmoChanged -= RefreshAmmoText;
        }
    }

    private void RefreshAmmoText()
    {
        if (rifle == null || ammoText == null)
            return;

        int mag = rifle.AmmoInMag;
        int reserve = rifle.ReserveAmmo;

        // thresholds (tune these)
        const int magLowThreshold = 5;
        const int reserveLowThreshold = 20;

        // bright green default, bright red when low
        string brightGreen = "#00FF6A";
        string brightRed = "#FF4040";

        string magColor = mag <= magLowThreshold ? brightRed : brightGreen;
        string reserveColor = reserve <= reserveLowThreshold ? brightRed : brightGreen;

        ammoText.text =
            "<align=right>"
            + $"<size=200%><b><color={magColor}>{mag:00}</color></b></size>"
            + $"<size=100%> / <color={reserveColor}>{reserve:000}</color></size>";
    }
}
