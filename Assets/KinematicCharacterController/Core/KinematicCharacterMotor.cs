using System;
using System.Collections.Generic;
using UnityEngine;

namespace KinematicCharacterController.Core {
    public enum RigidbodyInteractionType {
        None,
        Kinematic,
        SimulatedDynamic
    }

    public enum StepHandlingMethod {
        None,
        Standard,
        Extra
    }

    public enum MovementSweepState {
        Initial,
        AfterFirstHit,
        FoundBlockingCrease,
        FoundBlockingCorner
    }

    /// <summary>
    /// Represents the entire state of a character motor that is pertinent for simulation.
    /// Use this to save state or revert to past state
    /// </summary>
    [Serializable]
    public struct KinematicCharacterMotorState {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 baseVelocity;

        public bool mustUnground;
        public float mustUngroundTime;
        public bool lastMovementIterationFoundAnyGround;

        public Rigidbody attachedRigidbody;
        public Vector3 attachedRigidbodyVelocity;
        public CharacterTransientGroundingReport _groundingStatus;
    }

    /// <summary>
    /// Describes an overlap between the character capsule and another collider
    /// </summary>
    public struct OverlapResult {
        public Vector3 _normal;
        public Collider _collider;

        public OverlapResult(Vector3 normal, Collider collider) {
            _normal = normal;
            _collider = collider;
        }
    }

    /// <summary>
    /// Contains all the information for the motor's grounding status
    /// </summary>
    public struct CharacterGroundingReport {
        public bool _foundAnyGround;
        public bool _isStableOnGround;
        public bool _snappingPrevented;
        public Vector3 _groundNormal;
        public Vector3 _innerGroundNormal;
        public Vector3 _outerGroundNormal;

        public Collider _groundCollider;
        public Vector3 _groundPoint;

        public void CopyFrom(CharacterTransientGroundingReport transientGroundingReport) {
            _foundAnyGround = transientGroundingReport._foundAnyGround;
            _isStableOnGround = transientGroundingReport._isStableOnGround;
            _snappingPrevented = transientGroundingReport._snappingPrevented;
            _groundNormal = transientGroundingReport._groundNormal;
            _innerGroundNormal = transientGroundingReport._innerGroundNormal;
            _outerGroundNormal = transientGroundingReport._outerGroundNormal;

            _groundCollider = null;
            _groundPoint = Vector3.zero;
        }
    }

    /// <summary>
    /// Contains the simulation-relevant information for the motor's grounding status
    /// </summary>
    public struct CharacterTransientGroundingReport {
        public bool _foundAnyGround;
        public bool _isStableOnGround;
        public bool _snappingPrevented;
        public Vector3 _groundNormal;
        public Vector3 _innerGroundNormal;
        public Vector3 _outerGroundNormal;

        public void CopyFrom(CharacterGroundingReport groundingReport) {
            _foundAnyGround = groundingReport._foundAnyGround;
            _isStableOnGround = groundingReport._isStableOnGround;
            _snappingPrevented = groundingReport._snappingPrevented;
            _groundNormal = groundingReport._groundNormal;
            _innerGroundNormal = groundingReport._innerGroundNormal;
            _outerGroundNormal = groundingReport._outerGroundNormal;
        }
    }

    /// <summary>
    /// Contains all the information from a hit stability evaluation
    /// </summary>
    public struct HitStabilityReport {
        public bool _isStable;

        public bool _foundInnerNormal;
        public Vector3 _innerNormal;
        public bool _foundOuterNormal;
        public Vector3 _outerNormal;

        public bool _validStepDetected;
        public Collider _steppedCollider;

        public bool _ledgeDetected;
        public bool _isOnEmptySideOfLedge;
        public float _distanceFromLedge;
        public bool _isMovingTowardsEmptySideOfLedge;
        public Vector3 _ledgeGroundNormal;
        public Vector3 _ledgeRightDirection;
        public Vector3 _ledgeFacingDirection;
    }

    /// <summary>
    /// Contains the information of hit rigidbodies during the movement phase, so they can be processed afterwards
    /// </summary>
    public struct RigidbodyProjectionHit {
        public Rigidbody _rigidbody;
        public Vector3 _hitPoint;
        public Vector3 _effectiveHitNormal;
        public Vector3 _hitVelocity;
        public bool _stableOnHit;
    }

    /// <summary>
    /// Component that manages character collisions and movement solving
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider))]
    public class KinematicCharacterMotor : MonoBehaviour {
        void Awake() {
            Transform = transform;
            ValidateData();

            _transientPosition = Transform.position;
            TransientRotation = Transform.rotation;

            // Build CollidableLayers mask
            _collidableLayers = 0;

            for (int i = 0; i < 32; i++) {
                if (!Physics.GetIgnoreLayerCollision(gameObject.layer, i)) {
                    _collidableLayers |= 1 << i;
                }
            }

            SetCapsuleDimensions(capsuleRadius, capsuleHeight, capsuleYOffset);
        }

        void Reset() => ValidateData();

        void OnEnable() {
            KinematicCharacterSystem.EnsureCreation();
            KinematicCharacterSystem.RegisterCharacterMotor(this);
        }

        void OnDisable() => KinematicCharacterSystem.UnregisterCharacterMotor(this);

        void OnValidate() => ValidateData();

        [ContextMenu("Remove Component")]
        void HandleRemoveComponent() {
            CapsuleCollider tmpCapsule = gameObject.GetComponent<CapsuleCollider>();
            DestroyImmediate(this);
            DestroyImmediate(tmpCapsule);
        }

        /// <summary>
        /// Handle validating all required values
        /// </summary>
        public void ValidateData() {
            capsule = GetComponent<CapsuleCollider>();
            capsuleRadius = Mathf.Clamp(capsuleRadius, 0f, capsuleHeight * 0.5f);
            capsule.direction = 1;
            capsule.sharedMaterial = capsulePhysicsMaterial;
            SetCapsuleDimensions(capsuleRadius, capsuleHeight, capsuleYOffset);

            maxStepHeight = Mathf.Clamp(maxStepHeight, 0f, Mathf.Infinity);
            minRequiredStepDepth = Mathf.Clamp(minRequiredStepDepth, 0f, capsuleRadius);
            maxStableDistanceFromLedge = Mathf.Clamp(maxStableDistanceFromLedge, 0f, capsuleRadius);

            transform.localScale = Vector3.one;

#if UNITY_EDITOR
            capsule.hideFlags = HideFlags.NotEditable;

            if (!Mathf.Approximately(transform.lossyScale.x, 1f) ||
                !Mathf.Approximately(transform.lossyScale.y, 1f) ||
                !Mathf.Approximately(transform.lossyScale.z, 1f)) {
                Debug.LogError("Character's lossy scale is not (1,1,1). This is not allowed. Make sure the character's transform and all of its parents have a (1,1,1) scale.",
                    gameObject);
            }
#endif
        }

        /// <summary>
        /// Sets whether or not the capsule collider will detect collisions
        /// </summary>
        public void SetCapsuleCollisionsActivation(bool collisionsActive) => capsule.isTrigger = !collisionsActive;

        /// <summary>
        /// Sets whether or not the motor will solve collisions when moving (or moved onto)
        /// </summary>
        public void SetMovementCollisionsSolvingActivation(bool movementCollisionsSolvingActive) => _solveMovementCollisions = movementCollisionsSolvingActive;

        /// <summary>
        /// Sets whether or not grounding will be evaluated for all hits
        /// </summary>
        public void SetGroundSolvingActivation(bool stabilitySolvingActive) => _solveGrounding = stabilitySolvingActive;

        /// <summary>
        /// Sets the character's position directly
        /// </summary>
        public void SetPosition(Vector3 position, bool bypassInterpolation = true) {
            Transform.position = position;
            InitialSimulationPosition = position;
            _transientPosition = position;

            if (bypassInterpolation) {
                _initialTickPosition = position;
            }
        }

        /// <summary>
        /// Sets the character's rotation directly
        /// </summary>
        public void SetRotation(Quaternion rotation, bool bypassInterpolation = true) {
            Transform.rotation = rotation;
            InitialSimulationRotation = rotation;
            TransientRotation = rotation;

            if (bypassInterpolation) {
                _initialTickRotation = rotation;
            }
        }

        /// <summary>
        /// Sets the character's position and rotation directly
        /// </summary>
        public void SetPositionAndRotation(Vector3 position, Quaternion rotation, bool bypassInterpolation = true) {
            Transform.SetPositionAndRotation(position, rotation);
            InitialSimulationPosition = position;
            InitialSimulationRotation = rotation;
            _transientPosition = position;
            TransientRotation = rotation;

            if (!bypassInterpolation) return;

            _initialTickPosition = position;
            _initialTickRotation = rotation;
        }

        /// <summary>
        /// Moves the character position, taking all movement collision solving int account. The actual move is done the next time
        /// the motor updates are called
        /// </summary>
        public void MoveCharacter(Vector3 toPosition) {
            _movePositionDirty = true;
            _movePositionTarget = toPosition;
        }

        /// <summary>
        /// Moves the character rotation. The actual move is done the next time the motor updates are called
        /// </summary>
        public void RotateCharacter(Quaternion toRotation) {
            _moveRotationDirty = true;
            _moveRotationTarget = toRotation;
        }

        /// <summary>
        /// Returns all the state information of the motor that is pertinent for simulation
        /// </summary>
        public KinematicCharacterMotorState GetState() {
            KinematicCharacterMotorState state = new() {
                position = _transientPosition,
                rotation = _transientRotation,
                baseVelocity = _baseVelocity,
                attachedRigidbodyVelocity = _attachedRigidbodyVelocity,
                mustUnground = _mustUnground,
                mustUngroundTime = _mustUngroundTimeCounter,
                lastMovementIterationFoundAnyGround = _lastMovementIterationFoundAnyGround
            };

            state._groundingStatus.CopyFrom(_groundingStatus);
            state.attachedRigidbody = AttachedRigidbody;

            return state;
        }

        /// <summary>
        /// Applies a motor state instantly
        /// </summary>
        public void ApplyState(KinematicCharacterMotorState state, bool bypassInterpolation = true) {
            SetPositionAndRotation(state.position, state.rotation, bypassInterpolation);

            _baseVelocity = state.baseVelocity;
            _attachedRigidbodyVelocity = state.attachedRigidbodyVelocity;

            _mustUnground = state.mustUnground;
            _mustUngroundTimeCounter = state.mustUngroundTime;
            _lastMovementIterationFoundAnyGround = state.lastMovementIterationFoundAnyGround;
            _groundingStatus.CopyFrom(state._groundingStatus);
            AttachedRigidbody = state.attachedRigidbody;
        }

        /// <summary>
        /// Resizes capsule. ALso caches important capsule size data
        /// </summary>
        public void SetCapsuleDimensions(float radius, float height, float yOffset) {
            height = Mathf.Max(height, radius * 2f + 0.01f); // Safety to prevent invalid capsule geometries

            capsuleRadius = radius;
            capsuleHeight = height;
            capsuleYOffset = yOffset;

            capsule.radius = capsuleRadius;
            capsule.height = Mathf.Clamp(capsuleHeight, capsuleRadius * 2f, capsuleHeight);
            capsule.center = new Vector3(0f, capsuleYOffset, 0f);

            CharacterTransformToCapsuleCenter = capsule.center;
            CharacterTransformToCapsuleBottom = capsule.center + -_cachedWorldUp * (capsule.height * 0.5f);
            CharacterTransformToCapsuleTop = capsule.center + _cachedWorldUp * (capsule.height * 0.5f);
            CharacterTransformToCapsuleBottomHemi = capsule.center + -_cachedWorldUp * (capsule.height * 0.5f) + _cachedWorldUp * capsule.radius;
            CharacterTransformToCapsuleTopHemi = capsule.center + _cachedWorldUp * (capsule.height * 0.5f) + -_cachedWorldUp * capsule.radius;
        }

        /// <summary>
        /// Update phase 1 is meant to be called after physics movers have calculated their velocities, but
        /// before they have simulated their goal positions/rotations. It is responsible for:
        /// - Initializing all values for update
        /// - Handling MovePosition calls
        /// - Solving initial collision overlaps
        /// - Ground probing
        /// - Handle detecting potential interactable rigidbodies
        /// </summary>
        public void UpdatePhase1(float deltaTime) {
            // NaN propagation safety stop
            if (float.IsNaN(_baseVelocity.x) ||
                float.IsNaN(_baseVelocity.y) ||
                float.IsNaN(_baseVelocity.z)) {
                _baseVelocity = Vector3.zero;
            }

            if (float.IsNaN(_attachedRigidbodyVelocity.x) ||
                float.IsNaN(_attachedRigidbodyVelocity.y) ||
                float.IsNaN(_attachedRigidbodyVelocity.z)) {
                _attachedRigidbodyVelocity = Vector3.zero;
            }

#if UNITY_EDITOR
            if (!Mathf.Approximately(Transform.lossyScale.x, 1f) ||
                !Mathf.Approximately(Transform.lossyScale.y, 1f) ||
                !Mathf.Approximately(Transform.lossyScale.z, 1f)) {
                Debug.LogError("Character's lossy scale is not (1,1,1). This is not allowed. Make sure the character's transform and all of its parents have a (1,1,1) scale.",
                    gameObject);
            }
#endif

            _rigidbodiesPushedThisMove.Clear();

            // Before update
            _characterController.BeforeCharacterUpdate(deltaTime);

            _transientPosition = Transform.position;
            TransientRotation = Transform.rotation;
            InitialSimulationPosition = _transientPosition;
            InitialSimulationRotation = _transientRotation;
            _rigidbodyProjectionHitCount = 0;
            OverlapsCount = 0;
            _lastSolvedOverlapNormalDirty = false;

            #region Handle Move Position

            if (_movePositionDirty) {
                if (_solveMovementCollisions) {
                    Vector3 tmpVelocity = GetVelocityFromMovement(_movePositionTarget - _transientPosition, deltaTime);

                    if (InternalCharacterMove(ref tmpVelocity, deltaTime)) {
                        if (interactiveRigidbodyHandling) {
                            ProcessVelocityForRigidbodyHits(ref tmpVelocity, deltaTime);
                        }
                    }
                } else {
                    _transientPosition = _movePositionTarget;
                }

                _movePositionDirty = false;
            }

            #endregion

            _lastGroundingStatus.CopyFrom(_groundingStatus);

            _groundingStatus = new CharacterGroundingReport {
                _groundNormal = CharacterUp
            };

            if (_solveMovementCollisions) {
                #region Resolve initial overlaps

                int iterationsMade = 0;
                bool overlapSolved = false;

                while (iterationsMade < maxDecollisionIterations && !overlapSolved) {
                    int nbOverlaps = CharacterCollisionsOverlap(_transientPosition, _transientRotation, _internalProbedColliders);

                    if (nbOverlaps > 0) {
                        // Solve overlaps that aren't against dynamic rigidbodies or physics movers
                        for (int i = 0; i < nbOverlaps; i++) {
                            if (GetInteractiveRigidbody(_internalProbedColliders[i]) != null) continue;

                            // Process overlap
                            Transform overlappedTransform = _internalProbedColliders[i].GetComponent<Transform>();

                            if (!Physics.ComputePenetration(
                                    capsule,
                                    _transientPosition,
                                    _transientRotation,
                                    _internalProbedColliders[i],
                                    overlappedTransform.position,
                                    overlappedTransform.rotation,
                                    out Vector3 resolutionDirection,
                                    out float resolutionDistance)) {
                                continue;
                            }

                            // Resolve along obstruction direction
                            HitStabilityReport mockReport = new() {
                                _isStable = IsStableOnNormal(resolutionDirection)
                            };

                            resolutionDirection = GetObstructionNormal(resolutionDirection, mockReport._isStable);

                            // Solve overlap
                            Vector3 resolutionMovement = resolutionDirection * (resolutionDistance + CollisionOffset);
                            _transientPosition += resolutionMovement;

                            // Remember overlaps
                            if (OverlapsCount < Overlaps.Length) {
                                Overlaps[OverlapsCount] = new OverlapResult(resolutionDirection, _internalProbedColliders[i]);
                                OverlapsCount++;
                            }

                            break;
                        }
                    } else {
                        overlapSolved = true;
                    }

                    iterationsMade++;
                }

                #endregion
            }

            #region Ground Probing and Snapping

            // Handle ungrounding
            if (_solveGrounding) {
                if (MustUnground()) {
                    _transientPosition += CharacterUp * (MinimumGroundProbingDistance * 1.5f);
                } else {
                    // Choose the appropriate ground probing distance
                    float selectedGroundProbingDistance = MinimumGroundProbingDistance;

                    if (!_lastGroundingStatus._snappingPrevented && (_lastGroundingStatus._isStableOnGround || _lastMovementIterationFoundAnyGround)) {
                        selectedGroundProbingDistance = stepHandling != StepHandlingMethod.None ? Mathf.Max(capsuleRadius, maxStepHeight) : capsuleRadius;

                        selectedGroundProbingDistance += groundDetectionExtraDistance;
                    }

                    ProbeGround(ref _transientPosition, _transientRotation, selectedGroundProbingDistance, ref _groundingStatus);

                    if (!_lastGroundingStatus._isStableOnGround && _groundingStatus._isStableOnGround) {
                        // Handle stable landing
                        _baseVelocity = Vector3.ProjectOnPlane(_baseVelocity, CharacterUp);
                        _baseVelocity = GetDirectionTangentToSurface(_baseVelocity, _groundingStatus._groundNormal) * _baseVelocity.magnitude;
                    }
                }
            }

            _lastMovementIterationFoundAnyGround = false;

            if (_mustUngroundTimeCounter > 0f) {
                _mustUngroundTimeCounter -= deltaTime;
            }

            _mustUnground = false;

            #endregion

            if (_solveGrounding) {
                _characterController.PostGroundingUpdate(deltaTime);
            }

            if (!interactiveRigidbodyHandling) return;

            #region Interactive Rigidbody Handling

            _lastAttachedRigidbody = AttachedRigidbody;

            if (_attachedRigidbodyOverride) {
                AttachedRigidbody = _attachedRigidbodyOverride;
            } else {
                // Detect interactive rigidbodies from grounding
                if (_groundingStatus._isStableOnGround && _groundingStatus._groundCollider.attachedRigidbody) {
                    Rigidbody interactiveRigidbody = GetInteractiveRigidbody(_groundingStatus._groundCollider);

                    if (interactiveRigidbody) {
                        AttachedRigidbody = interactiveRigidbody;
                    }
                } else {
                    AttachedRigidbody = null;
                }
            }

            Vector3 tmpVelocityFromCurrentAttachedRigidbody = Vector3.zero;
            Vector3 tmpAngularVelocityFromCurrentAttachedRigidbody = Vector3.zero;

            if (AttachedRigidbody) {
                GetVelocityFromRigidbodyMovement(AttachedRigidbody,
                    _transientPosition,
                    deltaTime,
                    out tmpVelocityFromCurrentAttachedRigidbody,
                    out tmpAngularVelocityFromCurrentAttachedRigidbody);
            }

            // Conserve momentum when de-stabilized from an attached rigidbody
            if (preserveAttachedRigidbodyMomentum && _lastAttachedRigidbody != null && AttachedRigidbody != _lastAttachedRigidbody) {
                _baseVelocity += _attachedRigidbodyVelocity;
                _baseVelocity -= tmpVelocityFromCurrentAttachedRigidbody;
            }

            // Process additional Velocity from attached rigidbody
            _attachedRigidbodyVelocity = _cachedZeroVector;

            if (AttachedRigidbody) {
                _attachedRigidbodyVelocity = tmpVelocityFromCurrentAttachedRigidbody;

                // Rotation from attached rigidbody
                Vector3 newForward = Vector3
                    .ProjectOnPlane(Quaternion.Euler(Mathf.Rad2Deg * tmpAngularVelocityFromCurrentAttachedRigidbody * deltaTime) * CharacterForward, CharacterUp).normalized;

                TransientRotation = Quaternion.LookRotation(newForward, CharacterUp);
            }

            // Cancel out horizontal velocity upon landing on an attached rigidbody
            if (_groundingStatus._groundCollider &&
                _groundingStatus._groundCollider.attachedRigidbody &&
                _groundingStatus._groundCollider.attachedRigidbody == AttachedRigidbody &&
                AttachedRigidbody != null &&
                _lastAttachedRigidbody == null) {
                _baseVelocity -= Vector3.ProjectOnPlane(_attachedRigidbodyVelocity, CharacterUp);
            }

            // Movement from Attached Rigidbody
            if (!(_attachedRigidbodyVelocity.sqrMagnitude > 0f)) return;

            _isMovingFromAttachedRigidbody = true;

            if (_solveMovementCollisions) {
                // Perform the move from rigidbody velocity
                InternalCharacterMove(ref _attachedRigidbodyVelocity, deltaTime);
            } else {
                _transientPosition += _attachedRigidbodyVelocity * deltaTime;
            }

            _isMovingFromAttachedRigidbody = false;

            #endregion
        }

        /// <summary>
        /// Update phase 2 is meant to be called after physics movers have simulated their goal positions/rotations.
        /// At the end of this, the TransientPosition/Rotation values will be up-to-date with where the motor should be at the end
        /// of its move.
        /// It is responsible for:
        /// - Solving Rotation
        /// - Handle MoveRotation calls
        /// - Solving potential attached rigidbody overlaps
        /// - Solving Velocity
        /// - Applying planar constraint
        /// </summary>
        public void UpdatePhase2(float deltaTime) {
            // Handle rotation
            _characterController.UpdateRotation(ref _transientRotation, deltaTime);
            TransientRotation = _transientRotation;

            // Handle move rotation
            if (_moveRotationDirty) {
                TransientRotation = _moveRotationTarget;
                _moveRotationDirty = false;
            }

            if (_solveMovementCollisions && interactiveRigidbodyHandling) {
                if (interactiveRigidbodyHandling) {
                    #region Solve potential attached rigidbody overlap

                    if (AttachedRigidbody) {
                        float upwardsOffset = capsule.radius;

                        if (CharacterGroundSweep(
                                _transientPosition + CharacterUp * upwardsOffset,
                                _transientRotation,
                                -CharacterUp,
                                upwardsOffset,
                                out RaycastHit closestHit)) {
                            if (closestHit.collider.attachedRigidbody == AttachedRigidbody && IsStableOnNormal(closestHit.normal)) {
                                float distanceMovedUp = upwardsOffset - closestHit.distance;
                                _transientPosition = _transientPosition + CharacterUp * distanceMovedUp + CharacterUp * CollisionOffset;
                            }
                        }
                    }

                    #endregion
                }

                if (interactiveRigidbodyHandling) {
                    #region Resolve overlaps that could've been caused by rotation or physics movers simulation pushing the character

                    int iterationsMade = 0;
                    bool overlapSolved = false;

                    while (iterationsMade < maxDecollisionIterations && !overlapSolved) {
                        int nbOverlaps = CharacterCollisionsOverlap(_transientPosition, _transientRotation, _internalProbedColliders);

                        if (nbOverlaps > 0) {
                            for (int i = 0; i < nbOverlaps; i++) {
                                // Process overlap
                                Transform overlappedTransform = _internalProbedColliders[i].GetComponent<Transform>();

                                if (!Physics.ComputePenetration(
                                        capsule,
                                        _transientPosition,
                                        _transientRotation,
                                        _internalProbedColliders[i],
                                        overlappedTransform.position,
                                        overlappedTransform.rotation,
                                        out Vector3 resolutionDirection,
                                        out float resolutionDistance)) {
                                    continue;
                                }

                                // Resolve along obstruction direction
                                HitStabilityReport mockReport = new() {
                                    _isStable = IsStableOnNormal(resolutionDirection)
                                };

                                resolutionDirection = GetObstructionNormal(resolutionDirection, mockReport._isStable);

                                // Solve overlap
                                Vector3 resolutionMovement = resolutionDirection * (resolutionDistance + CollisionOffset);
                                _transientPosition += resolutionMovement;

                                // If interactiveRigidbody, register as rigidbody hit for velocity
                                if (interactiveRigidbodyHandling) {
                                    Rigidbody probedRigidbody = GetInteractiveRigidbody(_internalProbedColliders[i]);

                                    if (probedRigidbody != null) {
                                        HitStabilityReport tmpReport = new() {
                                            _isStable = IsStableOnNormal(resolutionDirection)
                                        };

                                        if (tmpReport._isStable) {
                                            _lastMovementIterationFoundAnyGround = tmpReport._isStable;
                                        }

                                        if (probedRigidbody != AttachedRigidbody) {
                                            Vector3 characterCenter = _transientPosition + _transientRotation * CharacterTransformToCapsuleCenter;
                                            Vector3 estimatedCollisionPoint = _transientPosition;


                                            StoreRigidbodyHit(
                                                probedRigidbody,
                                                Velocity,
                                                estimatedCollisionPoint,
                                                resolutionDirection,
                                                tmpReport);
                                        }
                                    }
                                }

                                // Remember overlaps
                                if (OverlapsCount < Overlaps.Length) {
                                    Overlaps[OverlapsCount] = new OverlapResult(resolutionDirection, _internalProbedColliders[i]);
                                    OverlapsCount++;
                                }

                                break;
                            }
                        } else {
                            overlapSolved = true;
                        }

                        iterationsMade++;
                    }

                    #endregion
                }
            }

            // Handle velocity
            _characterController.UpdateVelocity(ref _baseVelocity, deltaTime);

            //this.CharacterController.UpdateVelocity(ref BaseVelocity, deltaTime);
            if (_baseVelocity.magnitude < MinVelocityMagnitude) {
                _baseVelocity = Vector3.zero;
            }

            #region Calculate Character movement from base velocity

            // Perform the move from base velocity
            if (_baseVelocity.sqrMagnitude > 0f) {
                if (_solveMovementCollisions) {
                    InternalCharacterMove(ref _baseVelocity, deltaTime);
                } else {
                    _transientPosition += _baseVelocity * deltaTime;
                }
            }

            // Process rigidbody hits/overlaps to affect velocity
            if (interactiveRigidbodyHandling) {
                ProcessVelocityForRigidbodyHits(ref _baseVelocity, deltaTime);
            }

            #endregion

            // Handle planar constraint
            if (hasPlanarConstraint) {
                _transientPosition = InitialSimulationPosition + Vector3.ProjectOnPlane(_transientPosition - InitialSimulationPosition, planarConstraintAxis.normalized);
            }

            // Discrete collision detection
            if (discreteCollisionEvents) {
                int nbOverlaps = CharacterCollisionsOverlap(_transientPosition, _transientRotation, _internalProbedColliders, CollisionOffset * 2f);

                for (int i = 0; i < nbOverlaps; i++) {
                    _characterController.OnDiscreteCollisionDetected(_internalProbedColliders[i]);
                }
            }

            _characterController.AfterCharacterUpdate(deltaTime);
        }

        /// <summary>
        /// Determines if motor can be considered stable on given slope normal
        /// </summary>
        bool IsStableOnNormal(Vector3 normal) => Vector3.Angle(CharacterUp, normal) <= maxStableSlopeAngle;

        /// <summary>
        /// Determines if motor can be considered stable on given slope normal
        /// </summary>
        bool IsStableWithSpecialCases(ref HitStabilityReport stabilityReport, Vector3 velocity) {
            if (!ledgeAndDenivelationHandling) return true;

            if (stabilityReport._ledgeDetected) {
                if (stabilityReport._isMovingTowardsEmptySideOfLedge) {
                    // Max snap vel
                    Vector3 velocityOnLedgeNormal = Vector3.Project(velocity, stabilityReport._ledgeFacingDirection);

                    if (velocityOnLedgeNormal.magnitude >= maxVelocityForLedgeSnap) {
                        return false;
                    }
                }

                // Distance from ledge
                if (stabilityReport._isOnEmptySideOfLedge && stabilityReport._distanceFromLedge > maxStableDistanceFromLedge) {
                    return false;
                }
            }

            // "Launching" off of slopes of a certain denivelation angle
            if (!_lastGroundingStatus._foundAnyGround || stabilityReport._innerNormal.sqrMagnitude == 0f || stabilityReport._outerNormal.sqrMagnitude == 0f) return true;

            float denivelationAngle = Vector3.Angle(stabilityReport._innerNormal, stabilityReport._outerNormal);

            if (denivelationAngle > maxStableDenivelationAngle) {
                return false;
            }

            denivelationAngle = Vector3.Angle(_lastGroundingStatus._innerGroundNormal, stabilityReport._outerNormal);

            return !(denivelationAngle > maxStableDenivelationAngle);
        }

        /// <summary>
        /// Probes for valid ground and midifies the input transientPosition if ground snapping occurs
        /// </summary>
        public void ProbeGround(ref Vector3 probingPosition, Quaternion atRotation, float probingDistance, ref CharacterGroundingReport groundingReport) {
            if (probingDistance < MinimumGroundProbingDistance) {
                probingDistance = MinimumGroundProbingDistance;
            }

            int groundSweepsMade = 0;
            bool groundSweepingIsOver = false;
            Vector3 groundSweepPosition = probingPosition;
            Vector3 groundSweepDirection = atRotation * -_cachedWorldUp;
            float groundProbeDistanceRemaining = probingDistance;

            while (groundProbeDistanceRemaining > 0 && groundSweepsMade <= MaxGroundingSweepIterations && !groundSweepingIsOver) {
                // Sweep for ground detection
                if (CharacterGroundSweep(
                        groundSweepPosition, // position
                        atRotation, // rotation
                        groundSweepDirection, // direction
                        groundProbeDistanceRemaining, // distance
                        out RaycastHit groundSweepHit)) // hit
                {
                    Vector3 targetPosition = groundSweepPosition + groundSweepDirection * groundSweepHit.distance;
                    HitStabilityReport groundHitStabilityReport = new();

                    EvaluateHitStability(groundSweepHit.collider,
                        groundSweepHit.normal,
                        groundSweepHit.point,
                        targetPosition,
                        _transientRotation,
                        _baseVelocity,
                        ref groundHitStabilityReport);

                    groundingReport._foundAnyGround = true;
                    groundingReport._groundNormal = groundSweepHit.normal;
                    groundingReport._innerGroundNormal = groundHitStabilityReport._innerNormal;
                    groundingReport._outerGroundNormal = groundHitStabilityReport._outerNormal;
                    groundingReport._groundCollider = groundSweepHit.collider;
                    groundingReport._groundPoint = groundSweepHit.point;
                    groundingReport._snappingPrevented = false;

                    // Found stable ground
                    if (groundHitStabilityReport._isStable) {
                        // Find all scenarios where ground snapping should be canceled
                        groundingReport._snappingPrevented = !IsStableWithSpecialCases(ref groundHitStabilityReport, _baseVelocity);

                        groundingReport._isStableOnGround = true;

                        // Ground snapping
                        if (!groundingReport._snappingPrevented) {
                            probingPosition = groundSweepPosition + groundSweepDirection * (groundSweepHit.distance - CollisionOffset);
                        }

                        _characterController.OnGroundHit(groundSweepHit.collider, groundSweepHit.normal, groundSweepHit.point, ref groundHitStabilityReport);
                        groundSweepingIsOver = true;
                    } else {
                        // Calculate movement from this iteration and advance position
                        Vector3 sweepMovement = groundSweepDirection * groundSweepHit.distance + atRotation * _cachedWorldUp * Mathf.Max(CollisionOffset, groundSweepHit.distance);
                        groundSweepPosition = groundSweepPosition + sweepMovement;

                        // Set remaining distance
                        groundProbeDistanceRemaining = Mathf.Min(GroundProbeReboundDistance, Mathf.Max(groundProbeDistanceRemaining - sweepMovement.magnitude, 0f));

                        // Reorient direction
                        groundSweepDirection = Vector3.ProjectOnPlane(groundSweepDirection, groundSweepHit.normal).normalized;
                    }
                } else {
                    groundSweepingIsOver = true;
                }

                groundSweepsMade++;
            }
        }

        /// <summary>
        /// Forces the character to unground itself on its next grounding update
        /// </summary>
        public void ForceUnground(float time = 0.1f) {
            _mustUnground = true;
            _mustUngroundTimeCounter = time;
        }

        public bool MustUnground() => _mustUnground || _mustUngroundTimeCounter > 0f;

        /// <summary>
        /// Returns the direction adjusted to be tangent to a specified surface normal relatively to the character's up direction.
        /// Useful for reorienting a direction on a slope without any lateral deviation in trajectory
        /// </summary>
        public Vector3 GetDirectionTangentToSurface(Vector3 direction, Vector3 surfaceNormal) {
            Vector3 directionRight = Vector3.Cross(direction, CharacterUp);
            return Vector3.Cross(surfaceNormal, directionRight).normalized;
        }

        /// <summary>
        /// Moves the character's position by given movement while taking into account all physics simulation, step-handling and
        /// velocity projection rules that affect the character motor
        /// </summary>
        /// <returns> Returns false if movement could not be solved until the end </returns>
        bool InternalCharacterMove(ref Vector3 transientVelocity, float deltaTime) {
            if (deltaTime <= 0f) {
                return false;
            }

            // Planar constraint
            if (hasPlanarConstraint) {
                transientVelocity = Vector3.ProjectOnPlane(transientVelocity, planarConstraintAxis.normalized);
            }

            bool wasCompleted = true;
            Vector3 remainingMovementDirection = transientVelocity.normalized;
            float remainingMovementMagnitude = transientVelocity.magnitude * deltaTime;
            Vector3 originalVelocityDirection = remainingMovementDirection;
            int sweepsMade = 0;
            bool hitSomethingThisSweepIteration = true;
            Vector3 tmpMovedPosition = _transientPosition;
            bool previousHitIsStable = false;
            Vector3 previousVelocity = _cachedZeroVector;
            Vector3 previousObstructionNormal = _cachedZeroVector;
            MovementSweepState sweepState = MovementSweepState.Initial;

            // Project movement against current overlaps before doing the sweeps
            for (int i = 0; i < OverlapsCount; i++) {
                Vector3 overlapNormal = Overlaps[i]._normal;

                if (!(Vector3.Dot(remainingMovementDirection, overlapNormal) < 0f)) continue;

                bool stableOnHit = IsStableOnNormal(overlapNormal) && !MustUnground();
                Vector3 velocityBeforeProjection = transientVelocity;
                Vector3 obstructionNormal = GetObstructionNormal(overlapNormal, stableOnHit);

                InternalHandleVelocityProjection(
                    stableOnHit,
                    overlapNormal,
                    obstructionNormal,
                    originalVelocityDirection,
                    ref sweepState,
                    previousHitIsStable,
                    previousVelocity,
                    previousObstructionNormal,
                    ref transientVelocity,
                    ref remainingMovementMagnitude,
                    ref remainingMovementDirection);

                previousHitIsStable = stableOnHit;
                previousVelocity = velocityBeforeProjection;
                previousObstructionNormal = obstructionNormal;
            }

            // Sweep the desired movement to detect collisions
            while (remainingMovementMagnitude > 0f &&
                   sweepsMade <= maxMovementIterations &&
                   hitSomethingThisSweepIteration) {
                bool foundClosestHit = false;
                Vector3 closestSweepHitPoint = default;
                Vector3 closestSweepHitNormal = default;
                float closestSweepHitDistance = 0f;
                Collider closestSweepHitCollider = null;

                if (checkMovementInitialOverlaps) {
                    int numOverlaps = CharacterCollisionsOverlap(
                        tmpMovedPosition,
                        _transientRotation,
                        _internalProbedColliders);

                    if (numOverlaps > 0) {
                        closestSweepHitDistance = 0f;

                        float mostObstructingOverlapNormalDotProduct = 2f;

                        for (int i = 0; i < numOverlaps; i++) {
                            Collider tmpCollider = _internalProbedColliders[i];

                            if (!Physics.ComputePenetration(
                                    capsule,
                                    tmpMovedPosition,
                                    _transientRotation,
                                    tmpCollider,
                                    tmpCollider.transform.position,
                                    tmpCollider.transform.rotation,
                                    out Vector3 resolutionDirection,
                                    out float resolutionDistance)) {
                                continue;
                            }

                            float dotProduct = Vector3.Dot(remainingMovementDirection, resolutionDirection);

                            if (!(dotProduct < 0f) || !(dotProduct < mostObstructingOverlapNormalDotProduct)) continue;

                            mostObstructingOverlapNormalDotProduct = dotProduct;

                            closestSweepHitNormal = resolutionDirection;
                            closestSweepHitCollider = tmpCollider;
                            closestSweepHitPoint = tmpMovedPosition + _transientRotation * CharacterTransformToCapsuleCenter + resolutionDirection * resolutionDistance;

                            foundClosestHit = true;
                        }
                    }
                }

                if (!foundClosestHit &&
                    CharacterCollisionsSweep(
                        tmpMovedPosition, // position
                        _transientRotation, // rotation
                        remainingMovementDirection, // direction
                        remainingMovementMagnitude + CollisionOffset, // distance
                        out RaycastHit closestSweepHit, // closest hit
                        _internalCharacterHits) // all hits
                    >
                    0) {
                    closestSweepHitNormal = closestSweepHit.normal;
                    closestSweepHitDistance = closestSweepHit.distance;
                    closestSweepHitCollider = closestSweepHit.collider;
                    closestSweepHitPoint = closestSweepHit.point;

                    foundClosestHit = true;
                }

                if (foundClosestHit) {
                    // Calculate movement from this iteration
                    Vector3 sweepMovement = remainingMovementDirection * Mathf.Max(0f, closestSweepHitDistance - CollisionOffset);
                    tmpMovedPosition += sweepMovement;
                    remainingMovementMagnitude -= sweepMovement.magnitude;

                    // Evaluate if hit is stable
                    HitStabilityReport moveHitStabilityReport = new();

                    EvaluateHitStability(closestSweepHitCollider,
                        closestSweepHitNormal,
                        closestSweepHitPoint,
                        tmpMovedPosition,
                        _transientRotation,
                        transientVelocity,
                        ref moveHitStabilityReport);

                    // Handle stepping up steps points higher than bottom capsule radius
                    bool foundValidStepHit = false;

                    if (_solveGrounding && stepHandling != StepHandlingMethod.None && moveHitStabilityReport._validStepDetected) {
                        float obstructionCorrelation = Mathf.Abs(Vector3.Dot(closestSweepHitNormal, CharacterUp));

                        if (obstructionCorrelation <= CorrelationForVerticalObstruction) {
                            Vector3 stepForwardDirection = Vector3.ProjectOnPlane(-closestSweepHitNormal, CharacterUp).normalized;

                            Vector3 stepCastStartPoint = tmpMovedPosition +
                                                         stepForwardDirection * SteppingForwardDistance +
                                                         CharacterUp * maxStepHeight;

                            // Cast downward from the top of the stepping height
                            int nbStepHits = CharacterCollisionsSweep(
                                stepCastStartPoint, // position
                                _transientRotation, // rotation
                                -CharacterUp, // direction
                                maxStepHeight, // distance
                                out RaycastHit closestStepHit, // closest hit
                                _internalCharacterHits,
                                0f,
                                true); // all hits 

                            // Check for hit corresponding to stepped collider
                            for (int i = 0; i < nbStepHits; i++) {
                                if (_internalCharacterHits[i].collider != moveHitStabilityReport._steppedCollider) continue;

                                Vector3 endStepPosition = stepCastStartPoint + -CharacterUp * (_internalCharacterHits[i].distance - CollisionOffset);
                                tmpMovedPosition = endStepPosition;
                                foundValidStepHit = true;

                                // Project velocity on ground normal at step
                                transientVelocity = Vector3.ProjectOnPlane(transientVelocity, CharacterUp);
                                remainingMovementDirection = transientVelocity.normalized;

                                break;
                            }
                        }
                    }

                    // Handle movement solving
                    if (!foundValidStepHit) {
                        Vector3 obstructionNormal = GetObstructionNormal(closestSweepHitNormal, moveHitStabilityReport._isStable);

                        // Movement hit callback
                        _characterController.OnMovementHit(closestSweepHitCollider, closestSweepHitNormal, closestSweepHitPoint, ref moveHitStabilityReport);

                        // Handle remembering rigidbody hits
                        if (interactiveRigidbodyHandling && closestSweepHitCollider.attachedRigidbody) {
                            StoreRigidbodyHit(
                                closestSweepHitCollider.attachedRigidbody,
                                transientVelocity,
                                closestSweepHitPoint,
                                obstructionNormal,
                                moveHitStabilityReport);
                        }

                        bool stableOnHit = moveHitStabilityReport._isStable && !MustUnground();
                        Vector3 velocityBeforeProj = transientVelocity;

                        // Project velocity for next iteration
                        InternalHandleVelocityProjection(
                            stableOnHit,
                            closestSweepHitNormal,
                            obstructionNormal,
                            originalVelocityDirection,
                            ref sweepState,
                            previousHitIsStable,
                            previousVelocity,
                            previousObstructionNormal,
                            ref transientVelocity,
                            ref remainingMovementMagnitude,
                            ref remainingMovementDirection);

                        previousHitIsStable = stableOnHit;
                        previousVelocity = velocityBeforeProj;
                        previousObstructionNormal = obstructionNormal;
                    }
                }
                // If we hit nothing...
                else {
                    hitSomethingThisSweepIteration = false;
                }

                // Safety for exceeding max sweeps allowed
                sweepsMade++;

                if (sweepsMade <= maxMovementIterations) continue;

                if (killRemainingMovementWhenExceedMaxMovementIterations) {
                    remainingMovementMagnitude = 0f;
                }

                if (killVelocityWhenExceedMaxMovementIterations) {
                    transientVelocity = Vector3.zero;
                }

                wasCompleted = false;
            }

            // Move position for the remainder of the movement
            tmpMovedPosition += remainingMovementDirection * remainingMovementMagnitude;
            _transientPosition = tmpMovedPosition;

            return wasCompleted;
        }

        /// <summary>
        /// Gets the effective normal for movement obstruction depending on current grounding status
        /// </summary>
        Vector3 GetObstructionNormal(Vector3 hitNormal, bool stableOnHit) {
            // Find hit/obstruction/offset normal
            Vector3 obstructionNormal = hitNormal;

            if (_groundingStatus._isStableOnGround && !MustUnground() && !stableOnHit) {
                Vector3 obstructionLeftAlongGround = Vector3.Cross(_groundingStatus._groundNormal, obstructionNormal).normalized;
                obstructionNormal = Vector3.Cross(obstructionLeftAlongGround, CharacterUp).normalized;
            }

            // Catch cases where cross product between parallel normals returned 0
            if (obstructionNormal.sqrMagnitude == 0f) {
                obstructionNormal = hitNormal;
            }

            return obstructionNormal;
        }

        /// <summary>
        /// Remembers a rigidbody hit for processing later
        /// </summary>
        void StoreRigidbodyHit(Rigidbody hitRigidbody, Vector3 hitVelocity, Vector3 hitPoint, Vector3 obstructionNormal, HitStabilityReport hitStabilityReport) {
            if (_rigidbodyProjectionHitCount >= _internalRigidbodyProjectionHits.Length ||
                hitRigidbody.GetComponent<KinematicCharacterMotor>()) {
                return;
            }

            RigidbodyProjectionHit rph = new() {
                _rigidbody = hitRigidbody,
                _hitPoint = hitPoint,
                _effectiveHitNormal = obstructionNormal,
                _hitVelocity = hitVelocity,
                _stableOnHit = hitStabilityReport._isStable
            };

            _internalRigidbodyProjectionHits[_rigidbodyProjectionHitCount] = rph;
            _rigidbodyProjectionHitCount++;
        }

        public void SetTransientPosition(Vector3 newPos) => _transientPosition = newPos;

        /// <summary>
        /// Processes movement projection upon detecting a hit
        /// </summary>
        void InternalHandleVelocityProjection(bool stableOnHit, Vector3 hitNormal, Vector3 obstructionNormal, Vector3 originalDirection,
            ref MovementSweepState sweepState, bool previousHitIsStable, Vector3 previousVelocity, Vector3 previousObstructionNormal,
            ref Vector3 transientVelocity, ref float remainingMovementMagnitude, ref Vector3 remainingMovementDirection) {
            if (transientVelocity.sqrMagnitude <= 0f) {
                return;
            }

            Vector3 velocityBeforeProjection = transientVelocity;

            if (stableOnHit) {
                _lastMovementIterationFoundAnyGround = true;
                HandleVelocityProjection(ref transientVelocity, obstructionNormal, stableOnHit);
            } else {
                switch (sweepState) {
                    // Handle projection
                    case MovementSweepState.Initial:
                        HandleVelocityProjection(ref transientVelocity, obstructionNormal, stableOnHit);
                        sweepState = MovementSweepState.AfterFirstHit;
                        break;

                    // Blocking crease handling
                    case MovementSweepState.AfterFirstHit: {
                        EvaluateCrease(
                            transientVelocity,
                            previousVelocity,
                            obstructionNormal,
                            previousObstructionNormal,
                            stableOnHit,
                            previousHitIsStable,
                            _groundingStatus._isStableOnGround && !MustUnground(),
                            out bool foundCrease,
                            out Vector3 creaseDirection);

                        if (foundCrease) {
                            if (_groundingStatus._isStableOnGround && !MustUnground()) {
                                transientVelocity = Vector3.zero;
                                sweepState = MovementSweepState.FoundBlockingCorner;
                            } else {
                                transientVelocity = Vector3.Project(transientVelocity, creaseDirection);
                                sweepState = MovementSweepState.FoundBlockingCrease;
                            }
                        } else {
                            HandleVelocityProjection(ref transientVelocity, obstructionNormal, stableOnHit);
                        }

                        break;
                    }

                    // Blocking corner handling
                    case MovementSweepState.FoundBlockingCrease:
                        transientVelocity = Vector3.zero;
                        sweepState = MovementSweepState.FoundBlockingCorner;
                        break;
                }
            }

            if (hasPlanarConstraint) {
                transientVelocity = Vector3.ProjectOnPlane(transientVelocity, planarConstraintAxis.normalized);
            }

            float newVelocityFactor = transientVelocity.magnitude / velocityBeforeProjection.magnitude;
            remainingMovementMagnitude *= newVelocityFactor;
            remainingMovementDirection = transientVelocity.normalized;
        }

        void EvaluateCrease(
            Vector3 currentCharacterVelocity,
            Vector3 previousCharacterVelocity,
            Vector3 currentHitNormal,
            Vector3 previousHitNormal,
            bool currentHitIsStable,
            bool previousHitIsStable,
            bool characterIsStable,
            out bool isValidCrease,
            out Vector3 creaseDirection) {
            isValidCrease = false;
            creaseDirection = default;

            if (characterIsStable && currentHitIsStable && previousHitIsStable) return;

            Vector3 tmpBlockingCreaseDirection = Vector3.Cross(currentHitNormal, previousHitNormal).normalized;
            float dotPlanes = Vector3.Dot(currentHitNormal, previousHitNormal);
            bool isVelocityConstrainedByCrease = false;

            // Avoid calculations if the two planes are the same
            if (dotPlanes < 0.999f) {
                // TODO: can this whole part be made simpler? (with 2d projections, etc)
                Vector3 normalAOnCreasePlane = Vector3.ProjectOnPlane(currentHitNormal, tmpBlockingCreaseDirection).normalized;
                Vector3 normalBOnCreasePlane = Vector3.ProjectOnPlane(previousHitNormal, tmpBlockingCreaseDirection).normalized;
                float dotPlanesOnCreasePlane = Vector3.Dot(normalAOnCreasePlane, normalBOnCreasePlane);

                Vector3 enteringVelocityDirectionOnCreasePlane = Vector3.ProjectOnPlane(previousCharacterVelocity, tmpBlockingCreaseDirection).normalized;

                if (dotPlanesOnCreasePlane <= Vector3.Dot(-enteringVelocityDirectionOnCreasePlane, normalAOnCreasePlane) + 0.001f &&
                    dotPlanesOnCreasePlane <= Vector3.Dot(-enteringVelocityDirectionOnCreasePlane, normalBOnCreasePlane) + 0.001f) {
                    isVelocityConstrainedByCrease = true;
                }
            }

            if (!isVelocityConstrainedByCrease) return;

            // Flip crease direction to make it representative of the real direction our velocity would be projected to
            if (Vector3.Dot(tmpBlockingCreaseDirection, currentCharacterVelocity) < 0f) {
                tmpBlockingCreaseDirection = -tmpBlockingCreaseDirection;
            }

            isValidCrease = true;
            creaseDirection = tmpBlockingCreaseDirection;
        }

        /// <summary>
        /// Allows you to override the way velocity is projected on an obstruction
        /// </summary>
        public virtual void HandleVelocityProjection(ref Vector3 velocity, Vector3 obstructionNormal, bool stableOnHit) {
            if (_groundingStatus._isStableOnGround && !MustUnground()) {
                // On stable slopes, simply reorient the movement without any loss
                if (stableOnHit) {
                    velocity = GetDirectionTangentToSurface(velocity, obstructionNormal) * velocity.magnitude;
                }
                // On blocking hits, project the movement on the obstruction while following the grounding plane
                else {
                    Vector3 obstructionRightAlongGround = Vector3.Cross(obstructionNormal, _groundingStatus._groundNormal).normalized;
                    Vector3 obstructionUpAlongGround = Vector3.Cross(obstructionRightAlongGround, obstructionNormal).normalized;
                    velocity = GetDirectionTangentToSurface(velocity, obstructionUpAlongGround) * velocity.magnitude;
                    velocity = Vector3.ProjectOnPlane(velocity, obstructionNormal);
                }
            } else {
                if (stableOnHit) {
                    // Handle stable landing
                    velocity = Vector3.ProjectOnPlane(velocity, CharacterUp);
                    velocity = GetDirectionTangentToSurface(velocity, obstructionNormal) * velocity.magnitude;
                }
                // Handle generic obstruction
                else {
                    velocity = Vector3.ProjectOnPlane(velocity, obstructionNormal);
                }
            }
        }

        /// <summary>
        /// Allows you to override the way hit rigidbodies are pushed / interacted with.
        /// ProcessedVelocity is what must be modified if this interaction affects the character's velocity.
        /// </summary>
        public virtual void HandleSimulatedRigidbodyInteraction(ref Vector3 processedVelocity, RigidbodyProjectionHit hit, float deltaTime) { }

        /// <summary>
        /// Takes into account rigidbody hits for adding to the velocity
        /// </summary>
        void ProcessVelocityForRigidbodyHits(ref Vector3 processedVelocity, float deltaTime) {
            for (int i = 0; i < _rigidbodyProjectionHitCount; i++) {
                RigidbodyProjectionHit bodyHit = _internalRigidbodyProjectionHits[i];

                if (!bodyHit._rigidbody ||
                    _rigidbodiesPushedThisMove.Contains(bodyHit._rigidbody) ||
                    _internalRigidbodyProjectionHits[i]._rigidbody == AttachedRigidbody) {
                    continue;
                }

                // Remember we hit this rigidbody
                _rigidbodiesPushedThisMove.Add(bodyHit._rigidbody);

                float characterMass = simulatedCharacterMass;
                Vector3 characterVelocity = bodyHit._hitVelocity;

                KinematicCharacterMotor hitCharacterMotor = bodyHit._rigidbody.GetComponent<KinematicCharacterMotor>();
                bool hitBodyIsCharacter = hitCharacterMotor != null;
                bool hitBodyIsDynamic = !bodyHit._rigidbody.isKinematic;
                float hitBodyMass = bodyHit._rigidbody.mass;
                float hitBodyMassAtPoint = bodyHit._rigidbody.mass; // todo
                Vector3 hitBodyVelocity = bodyHit._rigidbody.velocity;

                if (hitBodyIsCharacter) {
                    hitBodyMass = hitCharacterMotor.simulatedCharacterMass;
                    hitBodyMassAtPoint = hitCharacterMotor.simulatedCharacterMass; // todo
                    hitBodyVelocity = hitCharacterMotor._baseVelocity;
                } else if (!hitBodyIsDynamic) {
                    PhysicsMover physicsMover = bodyHit._rigidbody.GetComponent<PhysicsMover>();

                    if (physicsMover) {
                        hitBodyVelocity = physicsMover.Velocity;
                    }
                }

                // Calculate the ratio of the total mass that the character mass represents
                float characterToBodyMassRatio = 1f;

                {
                    if (characterMass + hitBodyMassAtPoint > 0f) {
                        characterToBodyMassRatio = characterMass / (characterMass + hitBodyMassAtPoint);
                    } else {
                        characterToBodyMassRatio = 0.5f;
                    }

                    // Hitting a non-dynamic body
                    if (!hitBodyIsDynamic) {
                        characterToBodyMassRatio = 0f;
                    }
                    // Emulate kinematic body interaction
                    else if (rigidbodyInteractionType == RigidbodyInteractionType.Kinematic && !hitBodyIsCharacter) {
                        characterToBodyMassRatio = 1f;
                    }
                }

                ComputeCollisionResolutionForHitBody(
                    bodyHit._effectiveHitNormal,
                    characterVelocity,
                    hitBodyVelocity,
                    characterToBodyMassRatio,
                    out Vector3 velocityChangeOnCharacter,
                    out Vector3 velocityChangeOnBody);

                processedVelocity += velocityChangeOnCharacter;

                if (hitBodyIsCharacter) {
                    hitCharacterMotor._baseVelocity += velocityChangeOnCharacter;
                } else if (hitBodyIsDynamic) {
                    bodyHit._rigidbody.AddForceAtPosition(velocityChangeOnBody, bodyHit._hitPoint, ForceMode.VelocityChange);
                }

                if (rigidbodyInteractionType == RigidbodyInteractionType.SimulatedDynamic) {
                    HandleSimulatedRigidbodyInteraction(ref processedVelocity, bodyHit, deltaTime);
                }
            }
        }

        public void ComputeCollisionResolutionForHitBody(
            Vector3 hitNormal,
            Vector3 characterVelocity,
            Vector3 bodyVelocity,
            float characterToBodyMassRatio,
            out Vector3 velocityChangeOnCharacter,
            out Vector3 velocityChangeOnBody) {
            velocityChangeOnCharacter = default;
            velocityChangeOnBody = default;

            float bodyToCharacterMassRatio = 1f - characterToBodyMassRatio;
            float characterVelocityMagnitudeOnHitNormal = Vector3.Dot(characterVelocity, hitNormal);
            float bodyVelocityMagnitudeOnHitNormal = Vector3.Dot(bodyVelocity, hitNormal);

            // if character velocity was going against the obstruction, restore the portion of the velocity that got projected during the movement phase
            if (characterVelocityMagnitudeOnHitNormal < 0f) {
                Vector3 restoredCharacterVelocity = hitNormal * characterVelocityMagnitudeOnHitNormal;
                velocityChangeOnCharacter += restoredCharacterVelocity;
            }

            // solve impulse velocities on both bodies, but only if the body velocity would be giving resistance to the character in any way
            if (!(bodyVelocityMagnitudeOnHitNormal > characterVelocityMagnitudeOnHitNormal)) return;

            Vector3 relativeImpactVelocity = hitNormal * (bodyVelocityMagnitudeOnHitNormal - characterVelocityMagnitudeOnHitNormal);
            velocityChangeOnCharacter += relativeImpactVelocity * bodyToCharacterMassRatio;
            velocityChangeOnBody += -relativeImpactVelocity * characterToBodyMassRatio;
        }

        /// <summary>
        /// Determines if the input collider is valid for collision processing
        /// </summary>
        /// <returns> Returns true if the collider is valid </returns>
        bool CheckIfColliderValidForCollisions(Collider coll) => coll != capsule && InternalIsColliderValidForCollisions(coll); // Ignore self

        /// <summary>
        /// Determines if the input collider is valid for collision processing
        /// </summary>
        bool InternalIsColliderValidForCollisions(Collider coll) {
            Rigidbody colliderAttachedRigidbody = coll.attachedRigidbody;

            if (colliderAttachedRigidbody) {
                bool isRigidbodyKinematic = colliderAttachedRigidbody.isKinematic;

                // If movement is made from AttachedRigidbody, ignore the AttachedRigidbody
                if (_isMovingFromAttachedRigidbody && (!isRigidbodyKinematic || colliderAttachedRigidbody == AttachedRigidbody)) {
                    return false;
                }

                // don't collide with dynamic rigidbodies if our RigidbodyInteractionType is kinematic
                if (rigidbodyInteractionType == RigidbodyInteractionType.Kinematic && !isRigidbodyKinematic) {
                    // wake up rigidbody
                    if (coll.attachedRigidbody) {
                        coll.attachedRigidbody.WakeUp();
                    }

                    return false;
                }
            }

            // Custom checks
            bool colliderValid = _characterController.IsColliderValidForCollisions(coll);

            return colliderValid;
        }

        /// <summary>
        /// Determines if the motor is considered stable on a given hit
        /// </summary>
        public void EvaluateHitStability(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation,
            Vector3 withCharacterVelocity, ref HitStabilityReport stabilityReport) {
            if (!_solveGrounding) {
                stabilityReport._isStable = false;
                return;
            }

            Vector3 atCharacterUp = atCharacterRotation * _cachedWorldUp;
            Vector3 innerHitDirection = Vector3.ProjectOnPlane(hitNormal, atCharacterUp).normalized;

            stabilityReport._isStable = IsStableOnNormal(hitNormal);

            stabilityReport._foundInnerNormal = false;
            stabilityReport._foundOuterNormal = false;
            stabilityReport._innerNormal = hitNormal;
            stabilityReport._outerNormal = hitNormal;

            // Ledge handling
            if (ledgeAndDenivelationHandling) {
                float ledgeCheckHeight = MinDistanceForLedge;

                if (stepHandling != StepHandlingMethod.None) {
                    ledgeCheckHeight = maxStepHeight;
                }

                bool isStableLedgeInner = false;
                bool isStableLedgeOuter = false;

                if (CharacterCollisionsRaycast(
                        hitPoint + atCharacterUp * SecondaryProbesVertical + innerHitDirection * SecondaryProbesHorizontal,
                        -atCharacterUp,
                        ledgeCheckHeight + SecondaryProbesVertical,
                        out RaycastHit innerLedgeHit,
                        _internalCharacterHits) >
                    0) {
                    Vector3 innerLedgeNormal = innerLedgeHit.normal;
                    stabilityReport._innerNormal = innerLedgeNormal;
                    stabilityReport._foundInnerNormal = true;
                    isStableLedgeInner = IsStableOnNormal(innerLedgeNormal);
                }

                if (CharacterCollisionsRaycast(
                        hitPoint + atCharacterUp * SecondaryProbesVertical + -innerHitDirection * SecondaryProbesHorizontal,
                        -atCharacterUp,
                        ledgeCheckHeight + SecondaryProbesVertical,
                        out RaycastHit outerLedgeHit,
                        _internalCharacterHits) >
                    0) {
                    Vector3 outerLedgeNormal = outerLedgeHit.normal;
                    stabilityReport._outerNormal = outerLedgeNormal;
                    stabilityReport._foundOuterNormal = true;
                    isStableLedgeOuter = IsStableOnNormal(outerLedgeNormal);
                }

                stabilityReport._ledgeDetected = isStableLedgeInner != isStableLedgeOuter;

                if (stabilityReport._ledgeDetected) {
                    stabilityReport._isOnEmptySideOfLedge = isStableLedgeOuter && !isStableLedgeInner;
                    stabilityReport._ledgeGroundNormal = isStableLedgeOuter ? stabilityReport._outerNormal : stabilityReport._innerNormal;
                    stabilityReport._ledgeRightDirection = Vector3.Cross(hitNormal, stabilityReport._ledgeGroundNormal).normalized;

                    stabilityReport._ledgeFacingDirection =
                        Vector3.ProjectOnPlane(Vector3.Cross(stabilityReport._ledgeGroundNormal, stabilityReport._ledgeRightDirection), CharacterUp).normalized;

                    stabilityReport._distanceFromLedge = Vector3.ProjectOnPlane(hitPoint - (atCharacterPosition + atCharacterRotation * CharacterTransformToCapsuleBottom), atCharacterUp)
                        .magnitude;

                    stabilityReport._isMovingTowardsEmptySideOfLedge = Vector3.Dot(withCharacterVelocity.normalized, stabilityReport._ledgeFacingDirection) > 0f;
                }

                if (stabilityReport._isStable) {
                    stabilityReport._isStable = IsStableWithSpecialCases(ref stabilityReport, withCharacterVelocity);
                }
            }

            // Step handling
            if (stepHandling != StepHandlingMethod.None && !stabilityReport._isStable) {
                // Stepping not supported on dynamic rigidbodies
                Rigidbody hitRigidbody = hitCollider.attachedRigidbody;

                if (!(hitRigidbody && !hitRigidbody.isKinematic)) {
                    DetectSteps(atCharacterPosition, atCharacterRotation, hitPoint, innerHitDirection, ref stabilityReport);

                    if (stabilityReport._validStepDetected) {
                        stabilityReport._isStable = true;
                    }
                }
            }

            _characterController.ProcessHitStabilityReport(hitCollider, hitNormal, hitPoint, atCharacterPosition, atCharacterRotation, ref stabilityReport);
        }

        void DetectSteps(Vector3 characterPosition, Quaternion characterRotation, Vector3 hitPoint, Vector3 innerHitDirection, ref HitStabilityReport stabilityReport) {
            int nbStepHits = 0;
            RaycastHit outerStepHit;
            Vector3 characterUp = characterRotation * _cachedWorldUp;
            Vector3 verticalCharToHit = Vector3.Project(hitPoint - characterPosition, characterUp);
            Vector3 horizontalCharToHitDirection = Vector3.ProjectOnPlane(hitPoint - characterPosition, characterUp).normalized;
            Vector3 stepCheckStartPos = hitPoint - verticalCharToHit + characterUp * maxStepHeight + horizontalCharToHitDirection * CollisionOffset * 3f;

            // Do outer step check with capsule cast on hit point
            nbStepHits = CharacterCollisionsSweep(
                stepCheckStartPos,
                characterRotation,
                -characterUp,
                maxStepHeight + CollisionOffset,
                out outerStepHit,
                _internalCharacterHits,
                0f,
                true);

            // Check for overlaps and obstructions at the hit position
            if (CheckStepValidity(nbStepHits, characterPosition, characterRotation, innerHitDirection, stepCheckStartPos, out Collider tmpCollider)) {
                stabilityReport._validStepDetected = true;
                stabilityReport._steppedCollider = tmpCollider;
            }

            if (stepHandling != StepHandlingMethod.Extra || stabilityReport._validStepDetected) return;

            // Do min reach step check with capsule cast on hit point
            stepCheckStartPos = characterPosition + characterUp * maxStepHeight + -innerHitDirection * minRequiredStepDepth;

            nbStepHits = CharacterCollisionsSweep(
                stepCheckStartPos,
                characterRotation,
                -characterUp,
                maxStepHeight - CollisionOffset,
                out outerStepHit,
                _internalCharacterHits,
                0f,
                true);

            // Check for overlaps and obstructions at the hit position
            if (!CheckStepValidity(nbStepHits, characterPosition, characterRotation, innerHitDirection, stepCheckStartPos, out tmpCollider)) return;

            stabilityReport._validStepDetected = true;
            stabilityReport._steppedCollider = tmpCollider;
        }

        bool CheckStepValidity(int nbStepHits, Vector3 characterPosition, Quaternion characterRotation, Vector3 innerHitDirection, Vector3 stepCheckStartPos, out Collider hitCollider) {
            hitCollider = null;
            Vector3 characterUp = characterRotation * Vector3.up;

            // Find the farthest valid hit for stepping
            bool foundValidStepPosition = false;

            while (nbStepHits > 0 && !foundValidStepPosition) {
                // Get farthest hit among the remaining hits
                RaycastHit farthestHit = new();
                float farthestDistance = 0f;
                int farthestIndex = 0;

                for (int i = 0; i < nbStepHits; i++) {
                    float hitDistance = _internalCharacterHits[i].distance;

                    if (!(hitDistance > farthestDistance)) continue;

                    farthestDistance = hitDistance;
                    farthestHit = _internalCharacterHits[i];
                    farthestIndex = i;
                }

                Vector3 characterPositionAtHit = stepCheckStartPos + -characterUp * (farthestHit.distance - CollisionOffset);

                int atStepOverlaps = CharacterCollisionsOverlap(characterPositionAtHit, characterRotation, _internalProbedColliders);

                if (atStepOverlaps <= 0) {
                    // Check for outer hit slope normal stability at the step position
                    if (CharacterCollisionsRaycast(
                            farthestHit.point + characterUp * SecondaryProbesVertical + -innerHitDirection * SecondaryProbesHorizontal,
                            -characterUp,
                            maxStepHeight + SecondaryProbesVertical,
                            out RaycastHit outerSlopeHit,
                            _internalCharacterHits,
                            true) >
                        0) {
                        if (IsStableOnNormal(outerSlopeHit.normal)) {
                            // Cast upward to detect any obstructions to moving there
                            if (CharacterCollisionsSweep(
                                    characterPosition, // position
                                    characterRotation, // rotation
                                    characterUp, // direction
                                    maxStepHeight - farthestHit.distance, // distance
                                    out RaycastHit tmpUpObstructionHit, // closest hit
                                    _internalCharacterHits) // all hits
                                <=
                                0) {
                                // Do inner step check...
                                bool innerStepValid = false;
                                RaycastHit innerStepHit;

                                if (allowSteppingWithoutStableGrounding) {
                                    innerStepValid = true;
                                } else {
                                    // At the capsule center at the step height
                                    if (CharacterCollisionsRaycast(
                                            characterPosition + Vector3.Project(characterPositionAtHit - characterPosition, characterUp),
                                            -characterUp,
                                            maxStepHeight,
                                            out innerStepHit,
                                            _internalCharacterHits,
                                            true) >
                                        0) {
                                        if (IsStableOnNormal(innerStepHit.normal)) {
                                            innerStepValid = true;
                                        }
                                    }
                                }

                                if (!innerStepValid) {
                                    // At inner step of the step point
                                    if (CharacterCollisionsRaycast(
                                            farthestHit.point + innerHitDirection * SecondaryProbesHorizontal,
                                            -characterUp,
                                            maxStepHeight,
                                            out innerStepHit,
                                            _internalCharacterHits,
                                            true) >
                                        0) {
                                        if (IsStableOnNormal(innerStepHit.normal)) {
                                            innerStepValid = true;
                                        }
                                    }
                                }

                                // Final validation of step
                                if (innerStepValid) {
                                    hitCollider = farthestHit.collider;
                                    foundValidStepPosition = true;
                                    return true;
                                }
                            }
                        }
                    }
                }

                // Discard hit if not valid step
                if (foundValidStepPosition) continue;

                nbStepHits--;

                if (farthestIndex < nbStepHits) {
                    _internalCharacterHits[farthestIndex] = _internalCharacterHits[nbStepHits];
                }
            }

            return false;
        }

        /// <summary>
        /// Get true linear velocity (taking into account rotational velocity) on a given point of a rigidbody
        /// </summary>
        public void GetVelocityFromRigidbodyMovement(Rigidbody interactiveRigidbody, Vector3 atPoint, float deltaTime, out Vector3 linearVelocity, out Vector3 angularVelocity) {
            if (deltaTime > 0f) {
                linearVelocity = interactiveRigidbody.velocity;
                angularVelocity = interactiveRigidbody.angularVelocity;

                if (interactiveRigidbody.isKinematic) {
                    PhysicsMover physicsMover = interactiveRigidbody.GetComponent<PhysicsMover>();

                    if (physicsMover) {
                        linearVelocity = physicsMover.Velocity;
                        angularVelocity = physicsMover.AngularVelocity;
                    }
                }

                if (angularVelocity == Vector3.zero) return;

                Vector3 centerOfRotation = interactiveRigidbody.transform.TransformPoint(interactiveRigidbody.centerOfMass);

                Vector3 centerOfRotationToPoint = atPoint - centerOfRotation;
                Quaternion rotationFromInteractiveRigidbody = Quaternion.Euler(Mathf.Rad2Deg * angularVelocity * deltaTime);
                Vector3 finalPointPosition = centerOfRotation + rotationFromInteractiveRigidbody * centerOfRotationToPoint;
                linearVelocity += (finalPointPosition - atPoint) / deltaTime;
            } else {
                linearVelocity = default;
                angularVelocity = default;
            }
        }

        /// <summary>
        /// Determines if a collider has an attached interactive rigidbody
        /// </summary>
        Rigidbody GetInteractiveRigidbody(Collider onCollider) {
            Rigidbody colliderAttachedRigidbody = onCollider.attachedRigidbody;

            if (!colliderAttachedRigidbody) return null;

            if (colliderAttachedRigidbody.gameObject.GetComponent<PhysicsMover>()) {
                return colliderAttachedRigidbody;
            }

            if (!colliderAttachedRigidbody.isKinematic) {
                return colliderAttachedRigidbody;
            }

            return null;
        }

        /// <summary>
        /// Calculates the velocity required to move the character to the target position over a specific deltaTime.
        /// Useful for when you wish to work with positions rather than velocities in the UpdateVelocity callback
        /// </summary>
        public Vector3 GetVelocityForMovePosition(Vector3 fromPosition, Vector3 toPosition, float deltaTime) => GetVelocityFromMovement(toPosition - fromPosition, deltaTime);

        public Vector3 GetVelocityFromMovement(Vector3 movement, float deltaTime) {
            if (deltaTime <= 0f) {
                return Vector3.zero;
            }

            return movement / deltaTime;
        }

        /// <summary>
        /// Trims a vector to make it restricted against a plane
        /// </summary>
        void RestrictVectorToPlane(ref Vector3 vector, Vector3 toPlane) {
            if (vector.x > 0 != toPlane.x > 0) {
                vector.x = 0;
            }

            if (vector.y > 0 != toPlane.y > 0) {
                vector.y = 0;
            }

            if (vector.z > 0 != toPlane.z > 0) {
                vector.z = 0;
            }
        }

        /// <summary>
        /// Detect if the character capsule is overlapping with anything collideable
        /// </summary>
        /// <returns> Returns number of overlaps </returns>
        public int CharacterCollisionsOverlap(Vector3 position, Quaternion rotation, Collider[] overlappedColliders, float inflate = 0f, bool acceptOnlyStableGroundLayer = false) {
            int queryLayers = _collidableLayers;

            if (acceptOnlyStableGroundLayer) {
                queryLayers = _collidableLayers & stableGroundLayers;
            }

            Vector3 bottom = position + rotation * CharacterTransformToCapsuleBottomHemi;
            Vector3 top = position + rotation * CharacterTransformToCapsuleTopHemi;

            if (inflate != 0f) {
                bottom += rotation * Vector3.down * inflate;
                top += rotation * Vector3.up * inflate;
            }

            int nbHits = 0;

            int nbUnfilteredHits = Physics.OverlapCapsuleNonAlloc(
                bottom,
                top,
                capsule.radius + inflate,
                overlappedColliders,
                queryLayers,
                QueryTriggerInteraction.Ignore);

            // Filter out invalid colliders
            nbHits = nbUnfilteredHits;

            for (int i = nbUnfilteredHits - 1; i >= 0; i--) {
                if (CheckIfColliderValidForCollisions(overlappedColliders[i])) continue;

                nbHits--;

                if (i < nbHits) {
                    overlappedColliders[i] = overlappedColliders[nbHits];
                }
            }

            return nbHits;
        }

        /// <summary>
        /// Detect if the character capsule is overlapping with anything
        /// </summary>
        /// <returns> Returns number of overlaps </returns>
        public int CharacterOverlap(Vector3 position, Quaternion rotation, Collider[] overlappedColliders, LayerMask layers, QueryTriggerInteraction triggerInteraction,
            float inflate = 0f) {
            Vector3 bottom = position + rotation * CharacterTransformToCapsuleBottomHemi;
            Vector3 top = position + rotation * CharacterTransformToCapsuleTopHemi;

            if (inflate != 0f) {
                bottom += rotation * Vector3.down * inflate;
                top += rotation * Vector3.up * inflate;
            }

            int nbHits = 0;

            int nbUnfilteredHits = Physics.OverlapCapsuleNonAlloc(
                bottom,
                top,
                capsule.radius + inflate,
                overlappedColliders,
                layers,
                triggerInteraction);

            // Filter out the character capsule itself
            nbHits = nbUnfilteredHits;

            for (int i = nbUnfilteredHits - 1; i >= 0; i--) {
                if (overlappedColliders[i] != capsule) continue;

                nbHits--;

                if (i < nbHits) {
                    overlappedColliders[i] = overlappedColliders[nbHits];
                }
            }

            return nbHits;
        }

        /// <summary>
        /// Sweeps the capsule's volume to detect collision hits
        /// </summary>
        /// <returns> Returns the number of hits </returns>
        public int CharacterCollisionsSweep(Vector3 position, Quaternion rotation, Vector3 direction, float distance, out RaycastHit closestHit, RaycastHit[] hits, float inflate = 0f,
            bool acceptOnlyStableGroundLayer = false) {
            int queryLayers = _collidableLayers;

            if (acceptOnlyStableGroundLayer) {
                queryLayers = _collidableLayers & stableGroundLayers;
            }

            Vector3 bottom = position + rotation * CharacterTransformToCapsuleBottomHemi - direction * SweepProbingBackstepDistance;
            Vector3 top = position + rotation * CharacterTransformToCapsuleTopHemi - direction * SweepProbingBackstepDistance;

            if (inflate != 0f) {
                bottom += rotation * Vector3.down * inflate;
                top += rotation * Vector3.up * inflate;
            }

            // Capsule cast
            int nbHits;

            int nbUnfilteredHits = Physics.CapsuleCastNonAlloc(
                bottom,
                top,
                capsule.radius + inflate,
                direction,
                hits,
                distance + SweepProbingBackstepDistance,
                queryLayers,
                QueryTriggerInteraction.Ignore);

            // Hits filter
            closestHit = new RaycastHit();
            float closestDistance = Mathf.Infinity;
            nbHits = nbUnfilteredHits;

            for (int i = nbUnfilteredHits - 1; i >= 0; i--) {
                hits[i].distance -= SweepProbingBackstepDistance;

                RaycastHit hit = hits[i];
                float hitDistance = hit.distance;

                // Filter out the invalid hits
                if (hitDistance <= 0f || !CheckIfColliderValidForCollisions(hit.collider)) {
                    nbHits--;

                    if (i < nbHits) {
                        hits[i] = hits[nbHits];
                    }
                } else {
                    // Remember closest valid hit
                    if (!(hitDistance < closestDistance)) continue;

                    closestHit = hit;
                    closestDistance = hitDistance;
                }
            }

            return nbHits;
        }

        /// <summary>
        /// Sweeps the capsule's volume to detect hits
        /// </summary>
        /// <returns> Returns the number of hits </returns>
        public int CharacterSweep(Vector3 position, Quaternion rotation, Vector3 direction, float distance, out RaycastHit closestHit, RaycastHit[] hits, LayerMask layers,
            QueryTriggerInteraction triggerInteraction, float inflate = 0f) {
            closestHit = new RaycastHit();

            Vector3 bottom = position + rotation * CharacterTransformToCapsuleBottomHemi;
            Vector3 top = position + rotation * CharacterTransformToCapsuleTopHemi;

            if (inflate != 0f) {
                bottom += rotation * Vector3.down * inflate;
                top += rotation * Vector3.up * inflate;
            }

            // Capsule cast
            int nbHits = 0;

            int nbUnfilteredHits = Physics.CapsuleCastNonAlloc(
                bottom,
                top,
                capsule.radius + inflate,
                direction,
                hits,
                distance,
                layers,
                triggerInteraction);

            // Hits filter
            float closestDistance = Mathf.Infinity;
            nbHits = nbUnfilteredHits;

            for (int i = nbUnfilteredHits - 1; i >= 0; i--) {
                RaycastHit hit = hits[i];

                // Filter out the character capsule
                if (hit.distance <= 0f || hit.collider == capsule) {
                    nbHits--;

                    if (i < nbHits) {
                        hits[i] = hits[nbHits];
                    }
                } else {
                    // Remember closest valid hit
                    float hitDistance = hit.distance;

                    if (!(hitDistance < closestDistance)) continue;

                    closestHit = hit;
                    closestDistance = hitDistance;
                }
            }

            return nbHits;
        }

        /// <summary>
        /// Casts the character volume in the character's downward direction to detect ground
        /// </summary>
        /// <returns> Returns the number of hits </returns>
        bool CharacterGroundSweep(Vector3 position, Quaternion rotation, Vector3 direction, float distance, out RaycastHit closestHit) {
            closestHit = new RaycastHit();

            // Capsule cast
            int nbUnfilteredHits = Physics.CapsuleCastNonAlloc(
                position + rotation * CharacterTransformToCapsuleBottomHemi - direction * GroundProbingBackstepDistance,
                position + rotation * CharacterTransformToCapsuleTopHemi - direction * GroundProbingBackstepDistance,
                capsule.radius,
                direction,
                _internalCharacterHits,
                distance + GroundProbingBackstepDistance,
                _collidableLayers & stableGroundLayers,
                QueryTriggerInteraction.Ignore);

            // Hits filter
            bool foundValidHit = false;
            float closestDistance = Mathf.Infinity;

            for (int i = 0; i < nbUnfilteredHits; i++) {
                RaycastHit hit = _internalCharacterHits[i];
                float hitDistance = hit.distance;

                // Find the closest valid hit
                if (hitDistance <= 0f ||
                    !CheckIfColliderValidForCollisions(hit.collider) ||
                    hitDistance >= closestDistance) {
                    continue;
                }

                closestHit = hit;
                closestHit.distance -= GroundProbingBackstepDistance;
                closestDistance = hitDistance;

                foundValidHit = true;
            }

            return foundValidHit;
        }

        /// <summary>
        /// Raycasts to detect collision hits
        /// </summary>
        /// <returns> Returns the number of hits </returns>
        public int CharacterCollisionsRaycast(Vector3 position, Vector3 direction, float distance, out RaycastHit closestHit, RaycastHit[] hits,
            bool acceptOnlyStableGroundLayer = false) {
            int queryLayers = _collidableLayers;

            if (acceptOnlyStableGroundLayer) {
                queryLayers = _collidableLayers & stableGroundLayers;
            }

            // Raycast
            int nbHits = 0;

            int nbUnfilteredHits = Physics.RaycastNonAlloc(
                position,
                direction,
                hits,
                distance,
                queryLayers,
                QueryTriggerInteraction.Ignore);

            // Hits filter
            closestHit = new RaycastHit();
            float closestDistance = Mathf.Infinity;
            nbHits = nbUnfilteredHits;

            for (int i = nbUnfilteredHits - 1; i >= 0; i--) {
                RaycastHit hit = hits[i];
                float hitDistance = hit.distance;

                // Filter out the invalid hits
                if (hitDistance <= 0f ||
                    !CheckIfColliderValidForCollisions(hit.collider)) {
                    nbHits--;

                    if (i < nbHits) {
                        hits[i] = hits[nbHits];
                    }
                } else {
                    // Remember closest valid hit
                    if (!(hitDistance < closestDistance)) continue;

                    closestHit = hit;
                    closestDistance = hitDistance;
                }
            }

            return nbHits;
        }
#pragma warning disable 0414
        [Header("Components")]
        /// <summary>
        /// The capsule collider of this motor
        /// </summary>
        [ReadOnly]
        public CapsuleCollider capsule;

        [Header("Capsule Settings")]
        /// <summary>
        /// Radius of the character's capsule
        /// </summary>
        [SerializeField]
        [Tooltip("Radius of the Character Capsule")]
        float capsuleRadius = 0.5f;

        /// <summary>
        /// Height of the character's capsule
        /// </summary>
        [SerializeField] [Tooltip("Height of the Character Capsule")]
        float capsuleHeight = 2f;

        /// <summary>
        /// Local y position of the character's capsule center
        /// </summary>
        [SerializeField] [Tooltip("Height of the Character Capsule")]
        float capsuleYOffset = 1f;

        /// <summary>
        /// Physics material of the character's capsule
        /// </summary>
        [SerializeField] [Tooltip("Physics material of the Character Capsule (Does not affect character movement. Only affects things colliding with it)")]
#pragma warning disable 0649
        PhysicMaterial capsulePhysicsMaterial;
#pragma warning restore 0649


        [Header("Grounding settings")]
        /// <summary>
        /// Increases the range of ground detection, to allow snapping to ground at very high speeds
        /// </summary>    
        [Tooltip("Increases the range of ground detection, to allow snapping to ground at very high speeds")]
        public float groundDetectionExtraDistance;

        /// <summary>
        /// Maximum slope angle on which the character can be stable
        /// </summary>
        [Range(0f, 89f)] [Tooltip("Maximum slope angle on which the character can be stable")]
        public float maxStableSlopeAngle = 60f;

        /// <summary>
        /// Which layers can the character be considered stable on
        /// </summary>
        [Tooltip("Which layers can the character be considered stable on")]
        public LayerMask stableGroundLayers = -1;

        /// <summary>
        /// Notifies the Character Controller when discrete collisions are detected
        /// </summary>
        [Tooltip("Notifies the Character Controller when discrete collisions are detected")]
        public bool discreteCollisionEvents;


        [Header("Step settings")]
        /// <summary>
        /// Handles properly detecting grounding status on steps, but has a performance cost.
        /// </summary>
        [Tooltip("Handles properly detecting grounding status on steps, but has a performance cost.")]
        public StepHandlingMethod stepHandling = StepHandlingMethod.Standard;

        /// <summary>
        /// Maximum height of a step which the character can climb
        /// </summary>
        [Tooltip("Maximum height of a step which the character can climb")]
        public float maxStepHeight = 0.5f;

        /// <summary>
        /// Can the character step up obstacles even if it is not currently stable?
        /// </summary>
        [Tooltip("Can the character step up obstacles even if it is not currently stable?")]
        public bool allowSteppingWithoutStableGrounding;

        /// <summary>
        /// Minimum length of a step that the character can step on (used in Extra stepping method. Use this to let the character
        /// step on steps that are smaller that its radius
        /// </summary>
        [Tooltip("Minimum length of a step that the character can step on (used in Extra stepping method). Use this to let the character step on steps that are smaller that its radius")]
        public float minRequiredStepDepth = 0.1f;


        [Header("Ledge settings")]
        /// <summary>
        /// Handles properly detecting ledge information and grounding status, but has a performance cost.
        /// </summary>
        [Tooltip("Handles properly detecting ledge information and grounding status, but has a performance cost.")]
        public bool ledgeAndDenivelationHandling = true;

        /// <summary>
        /// The distance from the capsule central axis at which the character can stand on a ledge and still be stable
        /// </summary>
        [Tooltip("The distance from the capsule central axis at which the character can stand on a ledge and still be stable")]
        public float maxStableDistanceFromLedge = 0.5f;

        /// <summary>
        /// Prevents snapping to ground on ledges beyond a certain velocity
        /// </summary>
        [Tooltip("Prevents snapping to ground on ledges beyond a certain velocity")]
        public float maxVelocityForLedgeSnap;

        /// <summary>
        /// The maximun downward slope angle change that the character can be subjected to and still be snapping to the ground
        /// </summary>
        [Tooltip("The maximun downward slope angle change that the character can be subjected to and still be snapping to the ground")] [Range(1f, 180f)]
        public float maxStableDenivelationAngle = 180f;


        [Header("Rigidbody interaction settings")]
        /// <summary>
        /// Handles properly being pushed by and standing on PhysicsMovers or dynamic rigidbodies. Also handles pushing dynamic rigidbodies
        /// </summary>
        [Tooltip("Handles properly being pushed by and standing on PhysicsMovers or dynamic rigidbodies. Also handles pushing dynamic rigidbodies")]
        public bool interactiveRigidbodyHandling = true;

        /// <summary>
        /// How the character interacts with non-kinematic rigidbodies. \"Kinematic\" mode means the character pushes the
        /// rigidbodies with infinite force (as a kinematic body would). \"SimulatedDynamic\" pushes the rigidbodies with a
        /// simulated mass value.
        /// </summary>
        [Tooltip(
            "How the character interacts with non-kinematic rigidbodies. \"Kinematic\" mode means the character pushes the rigidbodies with infinite force (as a kinematic body would). \"SimulatedDynamic\" pushes the rigidbodies with a simulated mass value.")]
        public RigidbodyInteractionType rigidbodyInteractionType;

        [Tooltip("Mass used for pushing bodies")]
        public float simulatedCharacterMass = 1f;

        /// <summary>
        /// Determines if the character preserves moving platform velocities when de-grounding from them
        /// </summary>
        [Tooltip("Determines if the character preserves moving platform velocities when de-grounding from them")]
        public bool preserveAttachedRigidbodyMomentum = true;


        [Header("Constraints settings")]
        /// <summary>
        /// Determines if the character's movement uses the planar constraint
        /// </summary>
        [Tooltip("Determines if the character's movement uses the planar constraint")]
        public bool hasPlanarConstraint;

        /// <summary>
        /// Defines the plane that the character's movement is constrained on, if HasMovementConstraintPlane is active
        /// </summary>
        [Tooltip("Defines the plane that the character's movement is constrained on, if HasMovementConstraintPlane is active")]
        public Vector3 planarConstraintAxis = Vector3.forward;

        [Header("Other settings")]
        /// <summary>
        /// How many times can we sweep for movement per update
        /// </summary>
        [Tooltip("How many times can we sweep for movement per update")]
        public int maxMovementIterations = 5;

        /// <summary>
        /// How many times can we check for decollision per update
        /// </summary>
        [Tooltip("How many times can we check for decollision per update")]
        public int maxDecollisionIterations = 1;

        /// <summary>
        /// Checks for overlaps before casting movement, making sure all collisions are detected even when already intersecting
        /// geometry (has a performance cost, but provides safety against tunneling through colliders)
        /// </summary>
        [Tooltip(
            "Checks for overlaps before casting movement, making sure all collisions are detected even when already intersecting geometry (has a performance cost, but provides safety against tunneling through colliders)")]
        public bool checkMovementInitialOverlaps = true;

        /// <summary>
        /// Sets the velocity to zero if exceed max movement iterations
        /// </summary>
        [Tooltip("Sets the velocity to zero if exceed max movement iterations")]
        public bool killVelocityWhenExceedMaxMovementIterations = true;

        /// <summary>
        /// Sets the remaining movement to zero if exceed max movement iterations
        /// </summary>
        [Tooltip("Sets the remaining movement to zero if exceed max movement iterations")]
        public bool killRemainingMovementWhenExceedMaxMovementIterations = true;

        /// <summary>
        /// Contains the current grounding information
        /// </summary>
        [NonSerialized] public CharacterGroundingReport _groundingStatus;

        /// <summary>
        /// Contains the previous grounding information
        /// </summary>
        [NonSerialized] public CharacterTransientGroundingReport _lastGroundingStatus;

        /// <summary>
        /// Specifies the LayerMask that the character's movement algorithm can detect collisions with. By default, this uses the
        /// rigidbody's layer's collision matrix
        /// </summary>
        [NonSerialized] public LayerMask _collidableLayers = -1;

        /// <summary>
        /// The Transform of the character motor
        /// </summary>
        public Transform Transform { get; private set; }

        /// <summary>
        /// The character's goal position in its movement calculations (always up-to-date during the character update phase)
        /// </summary>
        public Vector3 TransientPosition => _transientPosition;

        Vector3 _transientPosition;

        /// <summary>
        /// The character's up direction (always up-to-date during the character update phase)
        /// </summary>
        public Vector3 CharacterUp { get; private set; }

        /// <summary>
        /// The character's forward direction (always up-to-date during the character update phase)
        /// </summary>
        public Vector3 CharacterForward { get; private set; }

        /// <summary>
        /// The character's right direction (always up-to-date during the character update phase)
        /// </summary>
        public Vector3 CharacterRight { get; private set; }

        /// <summary>
        /// The character's position before the movement calculations began
        /// </summary>
        public Vector3 InitialSimulationPosition { get; private set; }

        /// <summary>
        /// The character's rotation before the movement calculations began
        /// </summary>
        public Quaternion InitialSimulationRotation { get; private set; }

        /// <summary>
        /// Represents the Rigidbody to stay attached to
        /// </summary>
        public Rigidbody AttachedRigidbody { get; private set; }

        /// <summary>
        /// Vector3 from the character transform position to the capsule center
        /// </summary>
        public Vector3 CharacterTransformToCapsuleCenter { get; private set; }

        /// <summary>
        /// Vector3 from the character transform position to the capsule bottom
        /// </summary>
        public Vector3 CharacterTransformToCapsuleBottom { get; private set; }

        /// <summary>
        /// Vector3 from the character transform position to the capsule top
        /// </summary>
        public Vector3 CharacterTransformToCapsuleTop { get; private set; }

        /// <summary>
        /// Vector3 from the character transform position to the capsule bottom hemi center
        /// </summary>
        public Vector3 CharacterTransformToCapsuleBottomHemi { get; private set; }

        /// <summary>
        /// Vector3 from the character transform position to the capsule top hemi center
        /// </summary>
        public Vector3 CharacterTransformToCapsuleTopHemi { get; private set; }

        /// <summary>
        /// The character's velocity resulting from standing on rigidbodies or PhysicsMover
        /// </summary>
        public Vector3 AttachedRigidbodyVelocity => _attachedRigidbodyVelocity;

        Vector3 _attachedRigidbodyVelocity;

        /// <summary>
        /// The number of overlaps detected so far during character update (is reset at the beginning of the update)
        /// </summary>
        public int OverlapsCount { get; private set; }

        /// <summary>
        /// The overlaps detected so far during character update
        /// </summary>
        public OverlapResult[] Overlaps { get; } = new OverlapResult[MaxRigidbodyOverlapsCount];

        /// <summary>
        /// The motor's assigned controller
        /// </summary>
        [NonSerialized] public ICharacterController _characterController;

        /// <summary>
        /// Did the motor's last swept collision detection find a ground?
        /// </summary>
        [NonSerialized] public bool _lastMovementIterationFoundAnyGround;

        /// <summary>
        /// Index of this motor in KinematicCharacterSystem arrays
        /// </summary>
        [NonSerialized] public int _indexInCharacterSystem;

        /// <summary>
        /// Remembers initial position before all simulation are done
        /// </summary>
        [NonSerialized] public Vector3 _initialTickPosition;

        /// <summary>
        /// Remembers initial rotation before all simulation are done
        /// </summary>
        [NonSerialized] public Quaternion _initialTickRotation;

        /// <summary>
        /// Specifies a Rigidbody to stay attached to
        /// </summary>
        [NonSerialized] public Rigidbody _attachedRigidbodyOverride;

        /// <summary>
        /// The character's velocity resulting from direct movement
        /// </summary>
        [NonSerialized] public Vector3 _baseVelocity;

        // Private
        readonly RaycastHit[] _internalCharacterHits = new RaycastHit[MaxHitsBudget];
        readonly Collider[] _internalProbedColliders = new Collider[MaxCollisionBudget];
        readonly List<Rigidbody> _rigidbodiesPushedThisMove = new(16);
        readonly RigidbodyProjectionHit[] _internalRigidbodyProjectionHits = new RigidbodyProjectionHit[MaxRigidbodyOverlapsCount];
        Rigidbody _lastAttachedRigidbody;
        bool _solveMovementCollisions = true;
        bool _solveGrounding = true;
        bool _movePositionDirty;
        Vector3 _movePositionTarget = Vector3.zero;
        bool _moveRotationDirty;
        Quaternion _moveRotationTarget = Quaternion.identity;
        bool _lastSolvedOverlapNormalDirty;
        Vector3 _lastSolvedOverlapNormal = Vector3.forward;
        int _rigidbodyProjectionHitCount;
        bool _isMovingFromAttachedRigidbody;
        bool _mustUnground;
        float _mustUngroundTimeCounter;
        readonly Vector3 _cachedWorldUp = Vector3.up;
        readonly Vector3 _cachedWorldForward = Vector3.forward;
        readonly Vector3 _cachedWorldRight = Vector3.right;
        readonly Vector3 _cachedZeroVector = Vector3.zero;

        Quaternion _transientRotation;

        /// <summary>
        /// The character's goal rotation in its movement calculations (always up-to-date during the character update phase)
        /// </summary>
        public Quaternion TransientRotation {
            get => _transientRotation;
            private set {
                _transientRotation = value;
                CharacterUp = _transientRotation * _cachedWorldUp;
                CharacterForward = _transientRotation * _cachedWorldForward;
                CharacterRight = _transientRotation * _cachedWorldRight;
            }
        }

        /// <summary>
        /// The character's total velocity, including velocity from standing on rigidbodies or PhysicsMover
        /// </summary>
        public Vector3 Velocity => _baseVelocity + _attachedRigidbodyVelocity;

        // Warning: Don't touch these constants unless you know exactly what you're doing!
        public const int MaxHitsBudget = 16;
        public const int MaxCollisionBudget = 16;
        public const int MaxGroundingSweepIterations = 2;
        public const int MaxSteppingSweepIterations = 3;
        public const int MaxRigidbodyOverlapsCount = 16;
        public const float CollisionOffset = 0.01f;
        public const float GroundProbeReboundDistance = 0.02f;
        public const float MinimumGroundProbingDistance = 0.005f;
        public const float GroundProbingBackstepDistance = 0.1f;
        public const float SweepProbingBackstepDistance = 0.002f;
        public const float SecondaryProbesVertical = 0.02f;
        public const float SecondaryProbesHorizontal = 0.001f;
        public const float MinVelocityMagnitude = 0.01f;
        public const float SteppingForwardDistance = 0.03f;
        public const float MinDistanceForLedge = 0.05f;
        public const float CorrelationForVerticalObstruction = 0.01f;
        public const float ExtraSteppingForwardDistance = 0.01f;
        public const float ExtraStepHeightPadding = 0.01f;
#pragma warning restore 0414
    }
}