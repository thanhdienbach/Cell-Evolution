using UnityEngine;
using TMPro;

namespace CellEvol.Gameplay
{
    [DisallowMultipleComponent]
    public class CellHpTextView : MonoBehaviour
    {
        [Header("Referents")]
        [SerializeField] private CellController cellController;
        [SerializeField] private TMP_Text hpText;

        private int lastHp = int.MinValue;

        private void Reset()
        {
            hpText = GetComponent<TMP_Text>();
            cellController = GetComponentInParent<CellController>();
        }

        private void OnEnable()
        {
            if (hpText == null) hpText = GetComponent<TMP_Text>();
            if (cellController == null) cellController = GetComponentInParent<CellController>();

            if (hpText == null)
            {
                Debug.LogError($"{nameof(CellHpTextView)}: Missing TMP_Text.", this);
                enabled = false;
                return;
            }

            if (cellController == null)
            {
                Debug.LogError($"{nameof(CellHpTextView)}: Missing CellController in parent.", this);
                enabled = false;
                return;
            }

            cellController.HpChanged += OnHpChanged;

            OnHpChanged(cellController.CurrentHp, cellController.MaxHp);
        }

        private void OnHpChanged(int currentHp, int maxHp)
        {
            hpText.text = currentHp.ToString();
        }

        private void OnDisable()
        {
            if (cellController != null) cellController.HpChanged -= OnHpChanged;
        }
    }
}
