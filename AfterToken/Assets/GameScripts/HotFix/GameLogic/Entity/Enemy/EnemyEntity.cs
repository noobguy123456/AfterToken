using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 敌人实体。
    /// </summary>
    public class EnemyEntity : MonoBehaviour
    {
        [SerializeField] private int _configId;
        [SerializeField] private int _maxHp = 50;
        [SerializeField] private int _hp = 50;

        public int ConfigId => _configId;
        public int Hp => _hp;
        public int MaxHp => _maxHp;
        public bool IsDead => _hp <= 0;

        public void Initialize(int configId, int maxHp)
        {
            _configId = configId;
            _maxHp = maxHp;
            _hp = maxHp;
        }

        public void TakeDamage(int damage, Vector2 hitDirection)
        {
            if (IsDead) return;

            _hp -= damage;
            if (_hp < 0) _hp = 0;

            if (_hp <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            GameEvent.Get<IEnemyEvent>().OnEnemyDied(GetInstanceID());
            Destroy(gameObject);
        }
    }
}
