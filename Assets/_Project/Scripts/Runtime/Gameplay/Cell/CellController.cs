using UnityEngine;
using CellEvol.Data;
using System;


namespace CellEvol.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CellController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private CellConfig cellConfig;

        [Header("Runtime (Readonly)")]
        [SerializeField] private int currentHp = 1;

        public CellConfig Config => cellConfig;
        public int CurrentHp => currentHp;
        public int MaxHp => cellConfig != null ? cellConfig.MaxHp : 0;
        private float _regenAccum;

        [Header("Referent")]
        [SerializeField] CellHpTextView cellHpTextView;

        public event Action<int, int> HpChanged; // (currentHp, maxHp)


        private void Awake()
        {
            if (cellConfig == null)
            {
                Debug.LogError($"{nameof(CellController)}: Missing CellConfig reference.", this);
                enabled = false;
                return;
            }

            SetHp(1);
            _regenAccum = 0;
        }

        private void Update()
        {
            RegenHP(Time.deltaTime);
        }
        private void RegenHP(float deltaTime)
        {
            if (currentHp <= 0) return;
            if (currentHp >= cellConfig.MaxHp) return;
            if (cellConfig.RegenHpPerSecond <= 0) return ;

            _regenAccum += cellConfig.RegenHpPerSecond * deltaTime;

            if (_regenAccum < 1f) return ;

            int add = Mathf.FloorToInt(_regenAccum);
            _regenAccum -= add;

            SetHp(currentHp + add);
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0) return;
            if (currentHp <= 0) return;

            SetHp(currentHp - amount);

            if (currentHp <= 0)
            {
                currentHp = 0;
                // Todo: Gọi hàm die ở đây
            }
        }

        public void Heal(int amount)
        {
            if (amount <= 0) return;
            if (currentHp >= cellConfig.MaxHp) return;

            SetHp(currentHp + amount);

            if (currentHp == cellConfig.MaxHp)
            {
                //Todo: Gọi hàm UpCell tại đây
            }
        }

        private void SetHp(int newHp)
        {
            newHp = Mathf.Clamp(newHp, 0, cellConfig.MaxHp);
            if (newHp == currentHp) return;

            currentHp = newHp;

            HpChanged?.Invoke(currentHp, cellConfig.MaxHp);
        }
    }
}
