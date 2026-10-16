
using UnityEngine;

public class GeneratorInteraction : MonoBehaviour
{
    [SerializeField] private SkillCheckController skillCheck;
    [SerializeField] private SkillCheckSettings settings;

    public void StartRepairSkillCheck()
    {
        skillCheck.StartSkillCheck(
            settings,
            onSuccess: () =>
            {
                Debug.Log("Успех! Генератор получает прогресс.");
            },
            onGreat: () =>
            {
                Debug.Log("Отлично! Дополнительный прогресс.");
            },
            onFail: () =>
            {
                Debug.Log("Провал! Применяем штраф.");
            },
            onMiss: () =>
            {
                Debug.Log("Проверка пропущена.");
            }
        );
    }
}
