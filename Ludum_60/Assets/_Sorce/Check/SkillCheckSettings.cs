
using UnityEngine;

[CreateAssetMenu(
    fileName = "SkillCheckSettings",
    menuName = "Game/Skill Check Settings"
)]
public class SkillCheckSettings : ScriptableObject
{
    [Header("Rotation")]
    [Min(1f)]
    public float rotationSpeed = 180f;

    [Header("Zones")]
    [Range(1f, 180f)]
    public float successZoneSize = 50f;

    [Range(1f, 60f)]
    public float greatZoneSize = 12f;

    [Range(0f, 359f)]
    public float zoneCenterAngle = 90f;

    [Header("Input")]
    public KeyCode inputKey = KeyCode.Space;

    [Header("Behaviour")]
    public bool failOnWrongPress = true;
    public bool allowMultipleRotations = false;
}
