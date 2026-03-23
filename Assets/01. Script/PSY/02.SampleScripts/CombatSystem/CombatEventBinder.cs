/*
using UnityEngine;

// [주의] 이 파일은 3D RPG 프로젝트의 샘플 코드로, 현재 프로젝트의 클래스들과 충돌을 방지하기 위해 전체 주석 처리되었습니다.
// 필요한 로직은 01.Scripts 폴더 내의 Cleanup 시스템을 참고해 주세요.

[System.Serializable]
public class CombatEffectData
{
    public GameObject hpHealEffect;
    public GameObject mpHealEffect;
    public GameObject guardEffect;
}

public class CombatEventBinder
{
    private CombatEffectData effectData;

    public void Initialize(CombatEffectData data)
    {
        effectData = data;
    }

    public void Enable()
    {
        if (CombatSystem.Instance != null)
        {
            CombatSystem.Instance.Subscribe.OnSomeoneTakeDamage += OnSomeoneTakeDamage;
            CombatSystem.Instance.Subscribe.OnSomeoneHeal += OnSomeoneHeal;
            CombatSystem.Instance.Subscribe.OnSomeoneGuard += OnSomeoneGuard;
            CombatSystem.Instance.Subscribe.OnSomeoneCastSkill += OnSomeoneCastSkill;
        }
    }

    public void Disable()
    {
        if (CombatSystem.IsInitialized == false) return;

        var system = CombatSystem.Instance;
        if (system != null && system.Subscribe != null)
        {
            system.Subscribe.OnSomeoneTakeDamage -= OnSomeoneTakeDamage;
            system.Subscribe.OnSomeoneHeal -= OnSomeoneHeal;
            system.Subscribe.OnSomeoneGuard -= OnSomeoneGuard;
            system.Subscribe.OnSomeoneCastSkill -= OnSomeoneCastSkill;
        }
    }
    
    private void OnSomeoneTakeDamage(CombatEvent combatEvent)
    {
    }

    private void OnSomeoneHeal(CombatEvent combatEvent)
    {
    }

    private void OnSomeoneGuard(CombatEvent combatEvent)
    {
    }

    private void OnSomeoneCastSkill(CombatEvent combatEvent, Skill skill)
    {
    }
}
*/
