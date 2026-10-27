using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ninefold.Presentation
{
    /// <summary>
    /// Optional visual-only interface to a runtime prefab's Animator.
    /// No animator event may drive damage, positioning, turn end or mission state.
    /// No MonoBehaviour is attached to the prefab or battle token.
    ///
    /// Optional Animator contract:
    ///   bool Moving
    ///   trigger Attack
    ///   trigger Hit
    ///   trigger Heal
    ///   trigger Defeat
    /// Missing parameters are intentionally ignored, including static art.
    /// </summary>
    internal sealed class FrontiersBattleModelMotion
    {
        private readonly Driver[] drivers;
        private static readonly int Moving=Animator.StringToHash("Moving");
        private static readonly int Attack=Animator.StringToHash("Attack");
        private static readonly int Hit=Animator.StringToHash("Hit");
        private static readonly int Heal=Animator.StringToHash("Heal");
        private static readonly int Defeat=Animator.StringToHash("Defeat");

        private sealed class Driver
        {
            public Animator Animator;
            public readonly HashSet<int> Bools=new HashSet<int>();
            public readonly HashSet<int> Triggers=new HashSet<int>();
        }

        public FrontiersBattleModelMotion(GameObject modelRoot)
        {
            var found=new List<Driver>();
            if(modelRoot!=null)
            {
                foreach(var animator in modelRoot.GetComponentsInChildren<Animator>(true))
                {
                    if(animator==null || animator.runtimeAnimatorController==null) continue;
                    animator.applyRootMotion=false;
                    var driver=new Driver { Animator=animator };
                    foreach(var parameter in animator.parameters)
                    {
                        if(parameter.type==AnimatorControllerParameterType.Bool)
                            driver.Bools.Add(parameter.nameHash);
                        else if(parameter.type==AnimatorControllerParameterType.Trigger)
                            driver.Triggers.Add(parameter.nameHash);
                    }
                    found.Add(driver);
                }
            }
            drivers=found.ToArray();
        }

        public void BeginMovement() => SetMoving(true);
        public void EndMovement() => SetMoving(false);
        public void AttackTarget() => Fire(Attack);
        public void ReceiveHit() => Fire(Hit);
        public void ReceiveHeal() => Fire(Heal);
        public void Fall() => Fire(Defeat);

        private void SetMoving(bool value)
        {
            foreach(var driver in drivers)
            {
                var animator=driver.Animator;
                if(animator!=null && animator.isActiveAndEnabled &&
                    driver.Bools.Contains(Moving))
                    animator.SetBool(Moving,value);
            }
        }

        private void Fire(int parameter)
        {
            foreach(var driver in drivers)
            {
                var animator=driver.Animator;
                if(animator!=null && animator.isActiveAndEnabled &&
                    driver.Triggers.Contains(parameter))
                    animator.SetTrigger(parameter);
            }
        }
    }
}
