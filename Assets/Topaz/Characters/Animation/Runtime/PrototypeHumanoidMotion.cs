using System;
using Topaz.Combat;
using Topaz.Gameplay;
using Topaz.Player;
using UnityEngine;
using UnityEngine.AI;

namespace Topaz.Characters
{
    /// <summary>Temporary authored humanoid motion. Presentation never moves the actor or resolves hits.</summary>
    public sealed class PrototypeHumanoidMotion : MonoBehaviour
    {
        [Serializable] public struct HeldVisual { public string id; public GameObject model; }
        public HeldVisual[] equipment = Array.Empty<HeldVisual>();
        public string enemyEquipment = "sword";
        Quaternion restRotation;
        HumanPoseHandler handler;
        HumanPose pose;
        PlayerController player;
        PlayerCombat combat;
        WorldSession session;
        EnemyCombatant enemy;
        NavMeshAgent agent;
        CharacterVisual visual;
        int leftArm, rightArm, leftLeg, rightLeg, leftKnee, rightKnee, rightElbow, leftElbow;
        float phase;

        void Awake()
        {
            restRotation = transform.localRotation;
            var animator = GetComponent<Animator>();
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman) { enabled = false; return; }
            animator.enabled = false;
            handler = new HumanPoseHandler(animator.avatar, transform);
            handler.GetHumanPose(ref pose);
            player = GetComponentInParent<PlayerController>();
            combat = GetComponentInParent<PlayerCombat>();
            session = GetComponentInParent<WorldSession>();
            enemy = GetComponentInParent<EnemyCombatant>();
            agent = GetComponentInParent<NavMeshAgent>();
            visual = GetComponent<CharacterVisual>();
            leftArm = Muscle("Left Arm Down-Up"); rightArm = Muscle("Right Arm Down-Up");
            leftLeg = Muscle("Left Upper Leg Front-Back"); rightLeg = Muscle("Right Upper Leg Front-Back");
            leftKnee = Muscle("Left Lower Leg Stretch"); rightKnee = Muscle("Right Lower Leg Stretch");
            rightElbow = Muscle("Right Forearm Stretch"); leftElbow = Muscle("Left Forearm Stretch");
        }
        static int Muscle(string name) => Array.IndexOf(HumanTrait.MuscleName, name);
        void Set(int index, float value) { if (index >= 0) pose.muscles[index] = value; }
        void LateUpdate()
        {
            if (handler == null) return;
            float speed = player != null ? player.PlanarSpeed : agent != null && agent.enabled ? agent.velocity.magnitude : 0;
            float amount = Mathf.Clamp01(speed / 4f);
            phase += Time.deltaTime * 9f * amount;
            Array.Clear(pose.muscles, 0, pose.muscles.Length);
            Set(leftArm, -.8f); Set(rightArm, -.8f);
            Set(leftElbow, .6f); Set(rightElbow, .6f);
            Set(leftLeg, Mathf.Sin(phase) * .35f * amount);
            Set(rightLeg, -Mathf.Sin(phase) * .35f * amount);
            Set(leftKnee, 1f - Mathf.Max(0, -Mathf.Sin(phase)) * .8f * amount);
            Set(rightKnee, 1f - Mathf.Max(0, Mathf.Sin(phase)) * .8f * amount);
            bool attack = combat != null ? combat.IsAttackLocked : enemy != null && enemy.IsAttacking;
            if (attack) { Set(rightArm, -.15f); Set(rightElbow, Mathf.Sin(Time.time * 14f) * .5f); }
            if (combat != null && combat.IsGuarding) { Set(leftArm, -.25f); Set(leftElbow, -.5f); }
            if (player != null && player.IsDodging) { Set(leftLeg, .35f); Set(rightLeg, -.35f); }
            handler.SetHumanPose(ref pose);
            transform.localRotation = restRotation * (enemy != null && enemy.IsDown ? Quaternion.Euler(85,0,0) : Quaternion.identity);
            string selected = combat != null ? combat.EquippedToolId : enemyEquipment;
            foreach (var item in equipment)
                if (item.model != null) item.model.SetActive(item.id == selected && (session == null ||
                    (selected == "axe" ? session.HasAxe : selected == "pickaxe" ? session.HasPickaxe : session.HasWeapon)));
            if (visual != null && visual.Shield != null) visual.Shield.SetActive(session != null && session.HasShield);
        }
        void OnDestroy() { handler?.Dispose(); handler = null; }
    }
}
