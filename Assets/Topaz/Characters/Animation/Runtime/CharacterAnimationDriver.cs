using Topaz.Combat;
using Topaz.Player;
using UnityEngine;
using UnityEngine.AI;

namespace Topaz.Characters
{
    /// <summary>Shows gameplay state with configured animation clips; gameplay owns all movement and hit timing.</summary>
    [RequireComponent(typeof(Animator))]
    public sealed class CharacterAnimationDriver : MonoBehaviour
    {
        enum Pose
        {
            None, Locomotion, DodgeForward, DodgeBackward, DodgeLeft, DodgeRight,
            Jump, Sword, Axe, Pickaxe, CombatAxe, Staff, Crossbow, CrossbowReload, Hit, Attack, Death, Guard
        }

        [SerializeField] PlayerController player;
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
        [SerializeField] AnimationClip pickaxeClip;
        [SerializeField] AnimationClip combatAxeClip;
        [SerializeField] AnimationClip staffClip;
        [SerializeField] AnimationClip crossbowClip;
        [SerializeField] AnimationClip reloadClip;
        [SerializeField] AnimationClip hitClip;
        [SerializeField] AnimationClip enemyAttackClip;
        [SerializeField] GameObject swordVisual;
        [SerializeField] GameObject axeVisual;
        [SerializeField] GameObject pickaxeVisual;
        [SerializeField] GameObject combatAxeVisual;
        [SerializeField] GameObject staffVisual;
        [SerializeField] GameObject crossbowVisual;
        [SerializeField] GameObject shieldVisual;

        static readonly int Move = Animator.StringToHash("Move");
        static readonly int MoveX = Animator.StringToHash("MoveX");
        static readonly int MoveY = Animator.StringToHash("MoveY");
        // The Skeleton punch reaches full hand extension halfway through the source clip.
        const float EnemyAttackContact = 0.5f;
        Animator _animator;
        Pose _pose;
        Transform leftFoot, rightFoot;
        PlayerVitality vitality;

        void Awake()
        {
            player = GetComponentInParent<PlayerController>();
            playerCombat = GetComponentInParent<PlayerCombat>();
            vitality=GetComponentInParent<PlayerVitality>();
            enemy = GetComponentInParent<EnemyCombatant>();
            enemyAgent = enemy != null ? enemy.GetComponent<NavMeshAgent>() : null;
            _animator = GetComponent<Animator>();
            // Standalone visual prefabs also serve menu previews, without gameplay owners.
            if (player == null && enemy == null) { enabled = false; return; }
            _animator.applyRootMotion = false;
            leftFoot=_animator.GetBoneTransform(HumanBodyBones.LeftFoot);rightFoot=_animator.GetBoneTransform(HumanBodyBones.RightFoot);
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
                if (vitality!=null && vitality.CurrentHealth==0)
                    Show(Pose.Death);
                else if (player.IsDodging)
                    ShowDodge();
                else if (playerCombat != null && playerCombat.IsAttackLocked)
                {
                    string tool = playerCombat.EquippedToolId;
                    if (tool == "crossbow")
                        Show(playerCombat.IsCrossbowAiming ? Pose.Crossbow : Pose.CrossbowReload,
                            playerCombat.IsCrossbowAiming
                                ? crossbowClip
                                : reloadClip,
                            playerCombat.AttackAnimationSeconds);
                    else Show(tool == "pickaxe" ? Pose.Pickaxe : tool == "axe" ? Pose.Axe : tool == "combat-axe" ?
                            Pose.CombatAxe : tool == "staff" ? Pose.Staff : Pose.Sword,
                        tool == "pickaxe" ? pickaxeClip : tool == "axe" ? axeClip :
                            tool == "combat-axe" ? combatAxeClip : tool == "staff" ? staffClip : swordClip,
                        playerCombat.AttackAnimationSeconds);
                }
                else if (player.HasHitReaction)
                    Show(Pose.Hit, hitClip, player.HitReactionSeconds);
                else if (player.IsDodgeVisualActive)
                    ShowDodge();
                else if (player.IsAirborne)
                    Show(Pose.Jump, jumpClip, player.JumpSeconds);
                else if (playerCombat != null && playerCombat.IsGuarding)
                    Show(Pose.Guard);
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

        void OnAnimatorIK(int layer)
        {
            if(_animator==null || (player==null && enemy==null))return;
            float speed=player!=null?player.PlanarSpeed:enemyAgent!=null&&enemyAgent.enabled?enemyAgent.velocity.magnitude:0;
            float weight=(_pose==Pose.Locomotion || _pose==Pose.Guard) && (player==null || !player.IsAirborne) ? (speed<.2f?.9f:.35f):0;
            GroundFoot(AvatarIKGoal.LeftFoot,leftFoot,weight);GroundFoot(AvatarIKGoal.RightFoot,rightFoot,weight);
        }
        void GroundFoot(AvatarIKGoal goal,Transform bone,float weight)
        {
            _animator.SetIKPositionWeight(goal,0);
            if(weight<=0 || bone==null)return;
            if(Physics.Raycast(bone.position+Vector3.up*.4f,Vector3.down,out var hit,.85f,~(1<<2),QueryTriggerInteraction.Ignore))
            {_animator.SetIKPosition(goal,hit.point+Vector3.up*.035f);_animator.SetIKPositionWeight(goal,weight);}
        }

        void ShowEnemyAttack()
        {
            if(enemy.LootRole==SkeletonLootRole.Mage)
            { Show(Pose.Staff,staffClip,enemy.AttackAnimationSeconds); return; }
            if(enemy.LootRole==SkeletonLootRole.Rogue)
            { Show(enemy.IsWindingUp?Pose.Crossbow:Pose.CrossbowReload,enemy.IsWindingUp?crossbowClip:reloadClip,enemy.IsWindingUp?enemy.AttackWindupSeconds:enemy.AttackRecoverySeconds); return; }
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
                case PlayerController.DodgeFacing.Backward:
                    Show(Pose.DodgeBackward, dodgeBackwardClip, player.DodgeVisualSeconds);
                    break;
                case PlayerController.DodgeFacing.Left:
                    Show(Pose.DodgeLeft, dodgeLeftClip, player.DodgeVisualSeconds);
                    break;
                case PlayerController.DodgeFacing.Right:
                    Show(Pose.DodgeRight, dodgeRightClip, player.DodgeVisualSeconds);
                    break;
                default:
                    Show(Pose.DodgeForward, dodgeForwardClip, player.DodgeVisualSeconds);
                    break;
            }
        }

        void SyncHeldTool()
        {
            string tool = playerCombat != null ? playerCombat.EquippedToolId : enemy != null
                ? enemy.LootRole == SkeletonLootRole.Mage ? "staff" : enemy.LootRole == SkeletonLootRole.Rogue ? "crossbow" : "sword" : "";
            bool sword = tool == "sword", axe = tool == "axe", pickaxe = tool == "pickaxe";
            bool combatAxe = tool == "combat-axe", staff = tool == "staff", crossbow = tool == "crossbow";
            if (swordVisual != null && swordVisual.activeSelf != sword) swordVisual.SetActive(sword);
            if (axeVisual != null && axeVisual.activeSelf != axe) axeVisual.SetActive(axe);
            if (pickaxeVisual != null && pickaxeVisual.activeSelf != pickaxe)
                pickaxeVisual.SetActive(pickaxe);
            if (combatAxeVisual != null && combatAxeVisual.activeSelf != combatAxe)
                combatAxeVisual.SetActive(combatAxe);
            if (staffVisual != null && staffVisual.activeSelf != staff)
                staffVisual.SetActive(staff);
            if (crossbowVisual != null && crossbowVisual.activeSelf != crossbow)
                crossbowVisual.SetActive(crossbow);
            bool shield = player != null && playerCombat != null && playerCombat.HasShield;
            if (shieldVisual != null && shieldVisual.activeSelf != shield)
                shieldVisual.SetActive(shield);
        }

        void Show(Pose pose, AnimationClip clip = null, float duration = 0f)
        {
            if (_pose == pose) return;
            _pose = pose;
            _animator.speed = clip != null && duration > 0.01f
                ? clip.length / duration : pose == Pose.Locomotion ? (player != null ? player.TravelSpeed : enemy.TravelSpeed) / 4f : 1f;
            _animator.CrossFadeInFixedTime(pose.ToString(), pose == Pose.Death ? 0.02f : 0.06f);
        }
    }
}
