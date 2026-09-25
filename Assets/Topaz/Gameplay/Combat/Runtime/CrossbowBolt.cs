using System;
using Topaz.LoopStudy;
using UnityEngine;

namespace Topaz.CombatStudy
{
    /// <summary>A visible, swept bolt shared by the Rogue and player crossbow.</summary>
    public sealed class CrossbowBolt : MonoBehaviour
    {
        CrossbowAttackDefinition _attack;
        EnemyCombatant _enemyOwner;
        WorldSession _playerOwner;
        Vector3 _direction;
        Vector3 _source;
        float _remaining;
        int _damage;
        bool _pierceEnabled;
        bool _pierced;
        EnemyCombatant _firstVictim;

        public static bool Pierces(float randomValue) => randomValue < .2f;

        public void Launch(CrossbowAttackDefinition attack, Vector3 origin, Vector3 direction,
            int damage, EnemyCombatant enemyOwner = null, WorldSession playerOwner = null,
            bool pierceEnabled = false, float bonusRange = 0f)
        {
            if (attack == null || (enemyOwner == null) == (playerOwner == null))
                throw new ArgumentException("A bolt needs one attack and exactly one owner.");
            _attack = attack;
            _enemyOwner = enemyOwner;
            _playerOwner = playerOwner;
            _source = origin;
            _direction = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            if (_direction.sqrMagnitude < .01f) _direction = Vector3.forward;
            _remaining = attack.Range + bonusRange;
            _damage = damage;
            _pierceEnabled = pierceEnabled;
            transform.SetPositionAndRotation(origin, Quaternion.LookRotation(_direction));
        }

        void Update()
        {
            if (_attack == null) return;
            if (_enemyOwner == null && _playerOwner == null)
            {
                Destroy(gameObject);
                return;
            }
            float step = Mathf.Min(_remaining, _attack.BoltSpeed * Time.deltaTime);
            Vector3 start = transform.position;
            RaycastHit[] hits = Physics.SphereCastAll(start, .07f, _direction, step,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.IsChildOf(_enemyOwner != null
                        ? _enemyOwner.transform : _playerOwner.transform)) continue;
                EnemyCombatant enemy = hit.collider.GetComponentInParent<EnemyCombatant>();
                PlayerVitality player = hit.collider.GetComponentInParent<PlayerVitality>();
                if (_enemyOwner != null && enemy != null) continue; // No enemy friendly fire.
                if (_playerOwner != null && player != null) continue;
                transform.position = start + _direction * hit.distance;
                if (_enemyOwner != null && player != null)
                {
                    player.TryTakeRangedDamage(_damage, _enemyOwner.transform.position,
                        _enemyOwner);
                    Destroy(gameObject);
                    return;
                }
                if (_playerOwner != null && enemy != null)
                {
                    if (enemy == _firstVictim) continue;
                    int before = enemy.CurrentHealth;
                    enemy.TakeDirectedDamage(_damage, _source);
                    int dealt = before - enemy.CurrentHealth;
                    if (dealt > 0)
                        _playerOwner.RecordWeaponHit(WeaponSkill.Crossbows, dealt,
                            enemy.SourceLevel);
                    if (dealt > 0 && _attack.ImpactClip != null)
                        AudioSource.PlayClipAtPoint(_attack.ImpactClip, hit.point, .35f);
                    if (!_pierced && dealt > 0 && _pierceEnabled && Pierces(UnityEngine.Random.value))
                    {
                        _pierced = true;
                        _firstVictim = enemy;
                        continue;
                    }
                }
                Destroy(gameObject);
                return;
            }
            transform.position = start + _direction * step;
            _remaining -= step;
            if (_remaining <= 0f) Destroy(gameObject);
        }
    }
}
