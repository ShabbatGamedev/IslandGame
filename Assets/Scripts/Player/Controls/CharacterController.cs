using System;
using System.Collections.Generic;
using KinematicCharacterController.Core;
using UnityEngine;

namespace Player.Controls {
    public enum CharacterState {
        Default
    }

    public enum OrientationMethod {
        TowardsCamera,
        TowardsMovement
    }

    public struct PlayerCharacterInputs {
        public Quaternion _cameraRotation;
        public Vector2 _moveAxes;
        public bool _jump;
        public bool _crouching;
    }

    public enum BonusOrientationMethod {
        TowardsGravity,
        TowardsGroundSlopeAndGravity
    }
    
    public class CharacterController : MonoBehaviour, ICharacterController {
        #region Fields

        public KinematicCharacterMotor motor;
        
        [Header("Stable Movement")] 
        public float maxStableMoveSpeed = 10f;
        public float stableMovementSharpness = 15f;
        public float orientationSharpness = 10f;
        public OrientationMethod orientationMethod = OrientationMethod.TowardsCamera;

        [Header("Air Movement")]
        public float maxAirMoveSpeed = 15f;
        public float airAccelerationSpeed = 15f;
        public float drag = 0.1f;

        [Header("Jumping")] 
        public bool allowJumpingWhenSliding;
        public float jumpUpSpeed = 10f;
        public float jumpScalableForwardSpeed = 10f;
        public float jumpPreGroundingGraceTime;
        public float jumpPostGroundingGraceTime;

        [Header("Misc")] 
        public List<Collider> ignoredColliders = new();
        public BonusOrientationMethod bonusOrientationMethod = BonusOrientationMethod.TowardsGravity;
        public float bonusOrientationSharpness = 10f;
        public Vector3 gravity = new(0, -30f, 0);
        public Transform meshRoot;
        public Transform cameraFollowPoint;
        public float crouchedCapsuleHeight = 1f;

        readonly Collider[] _probedColliders = new Collider[8];
        Vector3 _internalVelocityAdd = Vector3.zero;
        bool _isCrouching;
        bool _jumpConsumed;
        bool _jumpedThisFrame;
        bool _jumpRequested;
        Vector3 _lookInputVector;
        Vector3 _moveInputVector;
        RaycastHit[] _probedHits = new RaycastHit[8];
        bool _shouldBeCrouching;
        float _timeSinceJumpRequested = Mathf.Infinity;
        float _timeSinceLastAbleToJump;

        Quaternion _tmpTransientRot;

        Vector3 _lastInnerNormal = Vector3.zero;
        Vector3 _lastOuterNormal = Vector3.zero;

        public CharacterState CurrentCharacterState { get; private set; }
        
        #endregion

        void Awake() {
            // Handle initial state
            TransitionToState(CharacterState.Default);

            // Assign the characterController to the motor
            motor._characterController = this;
        }
        
        /// <summary>
        /// This is called every frame by ExamplePlayer in order to tell the character what its inputs are
        /// </summary>
        public void SetInputs(ref PlayerCharacterInputs inputs) {
            // Clamp input
            Vector3 moveInputVector = Vector3.ClampMagnitude(new Vector3(inputs._moveAxes.x, 0, inputs._moveAxes.y), 1f);

            // Calculate camera direction and rotation on the character plane
            Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(inputs._cameraRotation * Vector3.forward, motor.CharacterUp).normalized;

            if (cameraPlanarDirection.sqrMagnitude == 0f) 
                cameraPlanarDirection = Vector3.ProjectOnPlane(inputs._cameraRotation * Vector3.up, motor.CharacterUp).normalized;

            Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, motor.CharacterUp);

            switch (CurrentCharacterState) {
                case CharacterState.Default: {
                    // Move and look inputs
                    _moveInputVector = cameraPlanarRotation * moveInputVector;

                    _lookInputVector = orientationMethod switch {
                        OrientationMethod.TowardsCamera => cameraPlanarDirection,
                        OrientationMethod.TowardsMovement => _moveInputVector.normalized,
                        _ => _lookInputVector
                    };

                    // Jumping input
                    if (inputs._jump) {
                        _timeSinceJumpRequested = 0f;
                        _jumpRequested = true;
                    }

                    // Crouching input
                    if (inputs._crouching) {
                        _shouldBeCrouching = true;

                        if (!_isCrouching) {
                            _isCrouching = true;
                            motor.SetCapsuleDimensions(0.5f, crouchedCapsuleHeight, crouchedCapsuleHeight * 0.5f);
                            meshRoot.localScale = new Vector3(1f, 0.5f, 1f);
                        }
                    } else _shouldBeCrouching = false;

                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        /// <summary>
        /// Handles movement state transitions and enter/exit callbacks
        /// </summary>
        void TransitionToState(CharacterState newState) {
            CharacterState tmpInitialState = CurrentCharacterState;
            OnStateExit(tmpInitialState, newState);
            CurrentCharacterState = newState;
            OnStateEnter(newState, tmpInitialState);
        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {
            switch (CurrentCharacterState) {
                case CharacterState.Default: {
                    if (_lookInputVector.sqrMagnitude > 0f && orientationSharpness > 0f) {
                        // Smoothly interpolate from current to target look direction
                        Vector3 smoothedLookInputDirection = Vector3.Slerp(motor.CharacterForward, _lookInputVector, 1 - Mathf.Exp(-orientationSharpness * deltaTime)).normalized;

                        // Set the current rotation (which will be used by the KinematicCharacterMotor)
                        currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, motor.CharacterUp);
                    }

                    Vector3 currentUp = currentRotation * Vector3.up;

                    switch (bonusOrientationMethod) {
                        case BonusOrientationMethod.TowardsGravity: {
                            // Rotate from current up to invert gravity
                            Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, -gravity.normalized, 1 - Mathf.Exp(-bonusOrientationSharpness * deltaTime));
                            currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
                            break;
                        }

                        case BonusOrientationMethod.TowardsGroundSlopeAndGravity when motor._groundingStatus._isStableOnGround: {
                            Vector3 initialCharacterBottomHemiCenter = motor.TransientPosition + currentUp * motor.capsule.radius;

                            Vector3 smoothedGroundNormal = Vector3.Slerp(motor.CharacterUp, motor._groundingStatus._groundNormal, 1 - Mathf.Exp(-bonusOrientationSharpness * deltaTime));
                            currentRotation = Quaternion.FromToRotation(currentUp, smoothedGroundNormal) * currentRotation;

                            // Move the position to create a rotation around the bottom hemi center instead of around the pivot
                            motor.SetTransientPosition(initialCharacterBottomHemiCenter + currentRotation * Vector3.down * motor.capsule.radius);
                            break;
                        }

                        case BonusOrientationMethod.TowardsGroundSlopeAndGravity: {
                            Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, -gravity.normalized, 1 - Mathf.Exp(-bonusOrientationSharpness * deltaTime));
                            currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
                            break;
                        }

                        default: {
                            Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, Vector3.up, 1 - Mathf.Exp(-bonusOrientationSharpness * deltaTime));
                            currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
                            break;
                        }
                    }

                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) {
                        switch (CurrentCharacterState) {
                case CharacterState.Default: {
                    // Ground movement
                    if (motor._groundingStatus._isStableOnGround) {
                        float currentVelocityMagnitude = currentVelocity.magnitude;

                        Vector3 effectiveGroundNormal = motor._groundingStatus._groundNormal;

                        // Reorient velocity on slope
                        currentVelocity = motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentVelocityMagnitude;

                        // Calculate target velocity
                        Vector3 inputRight = Vector3.Cross(_moveInputVector, motor.CharacterUp);
                        Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * _moveInputVector.magnitude;
                        Vector3 targetMovementVelocity = reorientedInput * maxStableMoveSpeed;

                        // Smooth movement Velocity
                        currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-stableMovementSharpness * deltaTime));
                    } else { // Air movement
                        // Add move input
                        if (_moveInputVector.sqrMagnitude > 0f) {
                            Vector3 addedVelocity = _moveInputVector * airAccelerationSpeed * deltaTime;

                            Vector3 currentVelocityOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp);

                            // Limit air velocity from inputs
                            if (currentVelocityOnInputsPlane.magnitude < maxAirMoveSpeed) {
                                // clamp addedVel to make total vel not exceed max vel on inputs plane
                                Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity, maxAirMoveSpeed);
                                addedVelocity = newTotal - currentVelocityOnInputsPlane;
                            } else {
                                // Make sure added vel doesn't go in the direction of the already-exceeding velocity
                                if (Vector3.Dot(currentVelocityOnInputsPlane, addedVelocity) > 0f)
                                    addedVelocity = Vector3.ProjectOnPlane(addedVelocity, currentVelocityOnInputsPlane.normalized);
                            }

                            // Prevent air-climbing sloped walls
                            if (motor._groundingStatus._foundAnyGround) {
                                if (Vector3.Dot(currentVelocity + addedVelocity, addedVelocity) > 0f) {
                                    Vector3 perpendicularObstructionNormal =
                                        Vector3.Cross(Vector3.Cross(motor.CharacterUp, motor._groundingStatus._groundNormal), motor.CharacterUp).normalized;

                                    addedVelocity = Vector3.ProjectOnPlane(addedVelocity, perpendicularObstructionNormal);
                                }
                            }

                            // Apply added velocity
                            currentVelocity += addedVelocity;
                        }

                        // Gravity
                        currentVelocity += gravity * deltaTime;

                        // Drag
                        currentVelocity *= 1 / (1 + drag * deltaTime);
                    }

                    // Handle jumping
                    _jumpedThisFrame = false;
                    _timeSinceJumpRequested += deltaTime;

                    if (_jumpRequested) {
                        // See if we actually are allowed to jump
                        if (!_jumpConsumed && 
                            ((allowJumpingWhenSliding ? motor._groundingStatus._foundAnyGround : motor._groundingStatus._isStableOnGround) || 
                             _timeSinceLastAbleToJump <= jumpPostGroundingGraceTime)) {
                            // Calculate jump direction before ungrounding
                            Vector3 jumpDirection = motor.CharacterUp;

                            if (motor._groundingStatus is { _foundAnyGround: true, _isStableOnGround: false }) 
                                jumpDirection = motor._groundingStatus._groundNormal;

                            // Makes the character skip ground probing/snapping on its next update. 
                            // If this line weren't here, the character would remain snapped to the ground when trying to jump. Try commenting this line out and see.
                            motor.ForceUnground();

                            // Add to the return velocity and reset jump state
                            currentVelocity += jumpDirection * jumpUpSpeed - Vector3.Project(currentVelocity, motor.CharacterUp);
                            currentVelocity += _moveInputVector * jumpScalableForwardSpeed;
                            _jumpRequested = false;
                            _jumpConsumed = true;
                            _jumpedThisFrame = true;
                        }
                    }

                    // Take into account additive velocity
                    if (_internalVelocityAdd.sqrMagnitude > 0f) {
                        currentVelocity += _internalVelocityAdd;
                        _internalVelocityAdd = Vector3.zero;
                    }

                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void BeforeCharacterUpdate(float deltaTime) { }

        public void PostGroundingUpdate(float deltaTime) {
            // Handle landing and leaving ground
            switch (motor._groundingStatus._isStableOnGround) {
                case true when !motor._lastGroundingStatus._isStableOnGround:
                    OnLanded();
                    break;

                case false when motor._lastGroundingStatus._isStableOnGround:
                    OnLeaveStableGround();
                    break;
            }
        }

        public void AfterCharacterUpdate(float deltaTime) {
            switch (CurrentCharacterState) {
                case CharacterState.Default: {
                    // Handle jump-related values
                    // Handle jumping pre-ground grace period
                    if (_jumpRequested && _timeSinceJumpRequested > jumpPreGroundingGraceTime) _jumpRequested = false;

                    if (allowJumpingWhenSliding ? motor._groundingStatus._foundAnyGround : motor._groundingStatus._isStableOnGround) {
                        // If we're on a ground surface, reset jumping values
                        if (!_jumpedThisFrame) _jumpConsumed = false;

                        _timeSinceLastAbleToJump = 0f;
                    } else _timeSinceLastAbleToJump += deltaTime; // Keep track of time since we were last able to jump (for grace period)

                    // Handle uncrouching
                    if (_isCrouching && !_shouldBeCrouching) {
                        // Do an overlap test with the character's standing height to see if there are any obstructions
                        motor.SetCapsuleDimensions(0.5f, 2f, 1f);

                        if (motor.CharacterOverlap(
                                motor.TransientPosition,
                                motor.TransientRotation,
                                _probedColliders,
                                motor._collidableLayers,
                                QueryTriggerInteraction.Ignore) > 0)
                            motor.SetCapsuleDimensions(0.5f, crouchedCapsuleHeight, crouchedCapsuleHeight * 0.5f); // If obstructions, just stick to crouching dimensions
                        else {
                            // If no obstructions, uncrouch
                            meshRoot.localScale = new Vector3(1f, 1f, 1f);
                            _isCrouching = false;
                        }
                    }

                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool IsColliderValidForCollisions(Collider coll) => !ignoredColliders.Contains(coll);

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation,
            ref HitStabilityReport hitStabilityReport) { }

        public void OnDiscreteCollisionDetected(Collider hitCollider) { }
        
        void OnLanded() { }

        void OnLeaveStableGround() { }
        
        /// <summary>
        /// Event when entering a state
        /// </summary>
        void OnStateEnter(CharacterState state, CharacterState fromState) {
            switch (state) {
                case CharacterState.Default:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        /// <summary>
        /// Event when exiting a state
        /// </summary>
        void OnStateExit(CharacterState state, CharacterState toState) {
            switch (state) {
                case CharacterState.Default:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }
    }
}