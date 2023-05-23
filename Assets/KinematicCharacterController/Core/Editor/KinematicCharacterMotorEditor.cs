using UnityEditor;
using UnityEngine;

namespace KinematicCharacterController.Core.Editor {
    [CustomEditor(typeof(KinematicCharacterMotor))]
    public class KinematicCharacterMotorEditor : UnityEditor.Editor {
        protected virtual void OnSceneGUI() {
            KinematicCharacterMotor motor = target as KinematicCharacterMotor;

            if (motor) {
                Vector3 characterBottom = motor.transform.position + (motor.capsule.center + -Vector3.up * (motor.capsule.height * 0.5f));

                Handles.color = Color.yellow;

                Handles.CircleHandleCap(
                    0,
                    characterBottom + motor.transform.up * motor.maxStepHeight,
                    Quaternion.LookRotation(motor.transform.up, motor.transform.forward),
                    motor.capsule.radius + 0.1f,
                    EventType.Repaint);
            }
        }
    }
}