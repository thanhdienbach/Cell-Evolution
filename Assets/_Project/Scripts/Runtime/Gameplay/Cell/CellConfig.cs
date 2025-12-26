using UnityEngine;

namespace CellEvol.Data
{
    [CreateAssetMenu(fileName = "CellConfig", menuName = "Config/Cell")]
    public sealed class CellConfig : ScriptableObject
    {
        [Header("Core")]
        [SerializeField, Min(1), Tooltip("Cell tier")]
        private int cellTier = 1;
        public int CellTier => cellTier;

        [SerializeField, Tooltip("Faction")] 
        private Faction faction = Faction.Player;
        public Faction Faction => faction;

        [Header("Variable")]
        [SerializeField, Min(1), Tooltip("Max HP of the cell")]
        private int maxHp = 10;
        public int MaxHp => maxHp;

        [SerializeField, Min(0.1f), Tooltip("HP regenerated per second.")]
        private float regenHpPerSecond = 2;
        public float RegenHpPerSecond => regenHpPerSecond;

        [SerializeField, Min(0.1f), Tooltip("Attack speed")]
        private float attackSpeed = 2;
        public float AttackSpeed => attackSpeed;

        private void OnValidate()
        {
            cellTier = Mathf.Clamp(cellTier, 1, 4);
            maxHp = Mathf.Max(1, maxHp);
            regenHpPerSecond = Mathf.Clamp(regenHpPerSecond, 0.1f, 4);
            attackSpeed = Mathf.Clamp(attackSpeed, 0.1f, 4);
        }
    }
}
