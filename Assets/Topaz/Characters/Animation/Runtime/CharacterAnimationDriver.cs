using Topaz.CombatStudy;
using Topaz.FeelStudy;
using UnityEngine;
using UnityEngine.AI;

namespace Topaz.AnimationStudy
{
    /// <summary>Shows gameplay state with KayKit clips; gameplay owns all movement and hit timing.</summary>
    [RequireComponent(typeof(Animator))]
    public sealed class CharacterAnimationDriver : MonoBehaviour
    {
        enum Pose
        {
            None, Locomotion, DodgeForward, DodgeBackward, DodgeLeft, DodgeRight,
            Jump, Sword, Axe, Hit, Attack, Death
        }

        [SerializeField] FeelStudyPlayer player;
        [SerializeField] PlayerCombat playerCombat;
        [SerializeField] EnemyCombatant enemy;
        [SerializeField] NavMeshAgent enemyAgent;
        [SerializeField] AnimationClip dodgeForwardClip;
        [SerializeField] AnimationClip dodgeBackwardClip;
        [SerializeField] AnimationClip dodgeLeftClip;
        [SerializeField] AnimationClip dodgeRightClip;
        [SerializeField] AnimationClip jumpClip;
        [SerializeField] AnimationClip swordClip;
        [SerializeField] AnimationClip axeClip;
        [SerializeField] AnimationClip hitClip;
        [SerializeField] AnimationClip enemyAttackClip;
        [SerializeField] GameObject swordVisual;
        [SerializeField] GameObject axeVisual;

        static readonly int Move = Animator.StringToHash("Move");
        static readonly int MoveX = Animator.StringToHash("MoveX");
        static readonly int MoveY = Animator.StringToHash("MoveY");
        // The Skeleton punch reaches full hand extension halfway through the source clip.
        const float EnemyAttackContact = 0.5f;
        Animator _animator;
        Pose _pose;

        public void ConfigurePlayerFrom(CharacterAnimationDriver source, FeelStudyPlayer mover,
            PlayerCombat combat, GameObject sword, GameObject axe)
        {
            player = mover;
            playerCombat = combat;
            enemy = null;
            enemyAgent = null;
            dodgeForwardClip = source.dodgeForwardClip;
            dodgeBackwardClip = source.dodgeBackwardClip;
            dodgeLeftClip = source.dodgeLeftClip;
            dodgeRightClip = source.dodgeRightClip;
            jumpClip = source.jumpClip;
            swordClip = source.swordClip;
            axeClip = source.axeClip;
            hitClip = source.hitClip;
            swordVisual = sword;
            axeVisual = axe;
        }

        void Awake()
        {
            _animator = GetComponent<Animator>();
            _animator.applyRootMotion = false;
            SyncHeldTool();
            if (_animator.runtimeAnimatorController == null ||
                (player == null && enemy == null))
            {
                Debug.LogError("Character animation is missing its controller or gameplay reference.", this);
                enabled = false;
            }
        }

        void OnDisable()
        {
            if (_animator != null) _animator.speed = 1f;
            _pose = Pose.None;
        }

        void LateUpdate()
        {
            if (player != null)
            {
                if (player.IsDodging)
                    ShowDodge();
                else if (playerCombat != null && playerCombat.IsAttackLocked)
                    Show(playerCombat.EquippedToolId == "axe" ? Pose.Axe : Pose.Sword,
                        playerCombat.EquippedToolId == "axe" ? axeClip : swordClip,
                        playerCombat.AttackAnimationSeconds);
                else if (player.HasHitReaction)
                    Show(Pose.Hit, hitClip, player.HitReactionSeconds);
                else if (player.IsDodgeVisualActive)
                    ShowDodge();
                else if (player.IsAirborne)
                    Show(Pose.Jump, jumpClip, player.JumpSeconds);
                else
                    Show(Pose.Locomotion);
                Vector3 right = Vector3.Cross(Vector3.up, player.AimDirection);
                float speedScale = 1f / player.TravelSpeed;
                _animator.SetFloat(MoveX,
                    Mathf.Clamp(Vector3.Dot(player.PlanarVelocity, right) * speedScale, -1f, 1f),
                    0.08f, Time.deltaTime);
                _animator.SetFloat(MoveY,
                    Mathf.Clamp(Vector3.Dot(player.PlanarVelocity, player.AimDirection) * speedScale, -1f, 1f),
                    0.08f, Time.deltaTime);
                SyncHeldTool();
                return;
            }

            if (!enemy.IsAlive)
                Show(Pose.Death);
            else if (enemy.IsAttacking)
                ShowEnemyAttack();
            else if (enemy.HasHitReaction)
                Show(Pose.Hit, hitClip, enemy.HitReactionSeconds);
            else
                Show(Pose.Locomotion);
            float speed = enemyAgent != null && enemyAgent.enabled
                ? enemyAgent.velocity.magnitude : 0f;
            _animator.SetFloat(Move, Mathf.Clamp01(speed / enemy.TravelSpeed),
                0.08f, Time.deltaTime);
        }

        void ShowEnemyAttack()
        {
            Show(Pose.Attack, enemyAttackClip, enemy.AttackAnimationSeconds);
            if (enemyAttackClip == null) return;
            float section = enemy.IsWindingUp ? EnemyAttackContact : 1f - EnemyAttackContact;
            float seconds = enemy.IsWindingUp ? enemy.AttackWindupSeconds : enemy.AttackRecoverySeconds;
            _animator.speed = enemyAttackClip.length * section / seconds;
        }

        void ShowDodge()
        {
            switch (player.LastDodgeFacing)
            {
                case FeelStudyPlayer.DodgeFacing.Backward:
                    Show(Pose.DodgeBackward, dodgeBackwardClip, player.DodgeVisualSeconds);
                    break;
                case FeelStudyPlayer.DodgeFacing.Left:
                    Show(Pose.DodgeLeft, dodgeLeftClip, player.DodgeVisualSeconds);
                    break;
                case FeelStudyPlayer.DodgeFacing.Right:
                    Show(Pose.DodgeRight, dodgeRightClip, player.DodgeVisualSeconds);
                    break;
                default:
                    Show(Pose.DodgeForward, dodgeForwardClip, player.DodgeVisualSeconds);
                    break;
            }
        }

        void SyncHeldTool()
        {
            bool sword = player != null && playerCombat != null && playerCombat.EquippedToolId == "sword";
            bool axe = player != null && playerCombat != null && playerCombat.EquippedToolId == "axe";
            if (swordVisual != null && swordVisual.activeSelf != sword) swordVisual.SetActive(sword);
            if (axeVisual != null && axeVisual.activeSelf != axe) axeVisual.SetActive(axe);
        }

        void Show(Pose pose, AnimationClip clip = null, float duration = 0f)
        {
            if (_pose == pose) return;
            _pose = pose;
            _animator.speed = clip != null && duration > 0.01f
                ? clip.length / duration : 1f;
            _animator.CrossFadeInFixedTime(pose.ToString(), pose == Pose.Death ? 0.02f : 0.06f);
        }
    }
}
