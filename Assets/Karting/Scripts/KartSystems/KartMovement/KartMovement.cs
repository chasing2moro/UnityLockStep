using System;
using System.Collections.Generic;
using KartGame.Track;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace KartGame.KartSystems {
    /// <summary>
    /// This class is responsible for all aspects of the kart's movement.  It uses a kinematic rigidbody and a capsule collider
    /// to simulate the presence of the kart but does not use the internal physics solver and instead uses its own solver.
    /// The  movement of the kart depends on the KartStats.  These have a default value but can be adjusted by anything implementing
    /// the IKartModifier interface.
    /// </summary>
    [RequireComponent (typeof (Rigidbody))]
    public class KartMovement : MonoBehaviour, IKartCollider, IMovable, IKartInfo {
        enum DriftState {
            NotDrifting,
            FacingLeft,
            FacingRight
        }

        [RequireInterface (typeof (IKartModifier))]
        [Tooltip ("A reference to the stats modification due to the type of kart being moved.  This can be either a Component or ScriptableObject.")]
        public Object kart;//卡丁车改变Stats
        [RequireInterface (typeof (IKartModifier))]
        [Tooltip ("A reference to the stats modification due to the driver of kart being moved.  This can be either a Component or ScriptableObject.")]
        public Object driver;//驾驶员改变Stats
        [RequireInterface (typeof (IKartModifier))]
        [Tooltip ("A reference to the stats modification due to the kart being airborne.  This can be either a Component or ScriptableObject.")]
        public Object airborneModifier;//airborne: 空气传播 改变Stats

        public IInput input;

        [Tooltip ("A reference to a transform representing the origin of a ray to help determine if the kart is grounded.  This is the front of a diamond formation.")]
        public Transform frontGroundRaycast;//前面 发射射线，检测是否在地上
        [Tooltip ("A reference to a transform representing the origin of a ray to help determine if the kart is grounded.  This is the right of a diamond formation.")]
        public Transform rightGroundRaycast;//右面 发射射线，检测是否在地上
        [Tooltip ("A reference to a transform representing the origin of a ray to help determine if the kart is grounded.  This is the left of a diamond formation.")]
        public Transform leftGroundRaycast;//左面 发射射线，检测是否在地上
        [Tooltip ("A reference to a transform representing the origin of a ray to help determine if the kart is grounded.  This is the rear of a diamond formation.")]
        public Transform rearGroundRaycast;//后面 发射射线，检测是否在地上

        [Tooltip ("A reference to the default stats for all karts.  Modifications to these are typically made using the kart and driver fields or by using the AddKartModifier function.")]
        public KartStats defaultStats;

        [Tooltip ("The layers which represent any ground the kart can drive on.")]
        public LayerMask groundLayers;//可以行使的Layer

        [Tooltip ("The layers which represent anything the kart can collide with.  This should include the ground layers.")]
        public LayerMask allCollidingLayers;//可以碰撞的Layer

        [Tooltip ("How fast the kart levels out when airborne.")]//levels out :把……弄平；达到平衡；持平
        public float airborneOrientationSpeed = 60f;

        [Tooltip ("The minimum value for the input's steering when the kart is drifting.")]
        public float minDriftingSteering = 0.2f;//

        [Tooltip ("How fast the kart's rotation gets corrected.  This is used for smoothing the karts rotation and returning to normal driving after a drift")]
        public float rotationCorrectionSpeed = 180f;//

        [Tooltip ("The smallest allowed angle for a kart to be turned from the velocity direction in order to a drift to start.")]
        public float minDriftStartAngle = 15f;//?

        [Tooltip ("The largest allowed angle for a kart to be turned from the velocity direction in order to a drift to start.")]
        public float maxDriftStartAngle = 90f;//?

        [Tooltip ("When karts collide the movement is based on their weight difference and this additional velocity change.")]
        public float kartToKartBump = 10f;

        public UnityEvent OnBecomeAirborne;
        public UnityEvent OnBecomeGrounded;
        public UnityEvent OnHop;
        public UnityEvent OnDriftStarted;
        public UnityEvent OnDriftStopped;
        public UnityEvent OnKartCollision;

        public bool IsOtherKart = false;
        public Vector3 syncPosition = new Vector3 ();
        public Quaternion syncRotation = Quaternion.identity;

        Vector3 m_RigidbodyPosition;
        Vector3 m_Velocity;
        Vector3 m_Movement;
        bool m_IsGrounded;
        GroundInfo m_CurrentGroundInfo;
        Rigidbody m_Rigidbody;
        CapsuleCollider m_Capsule;
        IRacer m_Racer;
        List<IKartModifier> m_CurrentModifiers = new List<IKartModifier> (16); // The karts stats are based on a list of modifiers.  Each affects the results of the previous until the modified stats are calculated.
        List<IKartModifier> m_TempModifiers = new List<IKartModifier> (8);
        KartStats m_ModifiedStats; // The stats that are used to calculate the kart's velocity.
        RaycastHit[] m_RaycastHitBuffer = new RaycastHit[8];
        Collider[] m_ColliderBuffer = new Collider[8];
        Quaternion m_DriftOffset = Quaternion.identity;
        DriftState m_DriftState;
        bool m_HasControl;
        float m_SteeringInput;
        float m_AccelerationInput;
        bool m_HopPressed;
        bool m_HopHeld;
        Vector3 m_RepositionPositionDelta;
        Quaternion m_RepositionRotationDelta = Quaternion.identity;

        const int k_MaxPenetrationSolves = 3;//Penetration:渗透；突破；侵入；洞察力
        const float k_GroundToCapsuleOffsetDistance = 0.025f;
        const float k_Deadzone = 0.01f;
        const float k_VelocityNormalAirborneDot = 0.5f;

        // These properties are part of the IKartInfo interface.
        public Vector3 Position => m_RigidbodyPosition;
        public Quaternion Rotation {
            get => m_Rigidbody == null? new Quaternion() : m_Rigidbody.rotation;
        }
        public KartStats CurrentStats => m_ModifiedStats;
        public Vector3 Velocity => m_Velocity;
        public Vector3 Movement => m_Movement;
        public float LocalSpeed {
            get => m_Rigidbody == null? 0: (Quaternion.Inverse (m_Rigidbody.rotation) * Quaternion.Inverse (m_DriftOffset) * m_Velocity).z;
        }
        public bool IsGrounded => m_IsGrounded;
        public GroundInfo CurrentGroundInfo => m_CurrentGroundInfo;

        void Reset () {
            groundLayers = LayerMask.GetMask ("Default");
            allCollidingLayers = LayerMask.GetMask ("Default");
        }

        void Start () {
            m_Rigidbody = GetComponent<Rigidbody> ();
            m_Capsule = GetComponent<CapsuleCollider> ();
            m_Racer = GetComponent<IRacer> ();

            if (kart != null)
                m_CurrentModifiers.Add ((IKartModifier) kart);

            if (driver != null)
                m_CurrentModifiers.Add ((IKartModifier) driver);

            syncPosition.y = -100.0f;
        }

        void FixedUpdate () {
            //Time.timeScale = 0，就是暂停游戏
            if (Mathf.Approximately (Time.timeScale, 0f))
                return;
            if (m_RepositionPositionDelta.sqrMagnitude > float.Epsilon || m_RepositionRotationDelta != Quaternion.identity) {
                //移动
                m_Rigidbody.MovePosition (m_Rigidbody.position + m_RepositionPositionDelta);
                //转向
                m_Rigidbody.MoveRotation (m_RepositionRotationDelta * m_Rigidbody.rotation);
                m_RepositionPositionDelta = Vector3.zero;
                m_RepositionRotationDelta = Quaternion.identity;
                return;
            }
            m_RigidbodyPosition = m_Rigidbody.position;

            KartStats.GetModifiedStats (m_CurrentModifiers, defaultStats, ref m_ModifiedStats);
            ClearTempModifiers ();//清除 velocity collisions 添加的修改

            Quaternion rotationStream = m_Rigidbody.rotation;

            float deltaTime = Time.deltaTime;

            //获取当前车 地面上的信息
            m_CurrentGroundInfo = CheckForGround (deltaTime, rotationStream, Vector3.zero);

            Hop (rotationStream, m_CurrentGroundInfo);

            if (m_CurrentGroundInfo.isGrounded && !m_IsGrounded)
                OnBecomeGrounded.Invoke ();

            if (!m_CurrentGroundInfo.isGrounded && m_IsGrounded)
                OnBecomeAirborne.Invoke ();//车轮离地，空气助力

            m_IsGrounded = m_CurrentGroundInfo.isGrounded;

            //车轮离地，空气助力、车轮着地，去除空气助力
            ApplyAirborneModifier(m_CurrentGroundInfo);

            GroundInfo nextGroundInfo = CheckForGround (deltaTime, rotationStream, m_Velocity * deltaTime);

            //@warning 根据下一个地面，使车和地面平行
            GroundNormal (deltaTime, ref rotationStream, m_CurrentGroundInfo, nextGroundInfo);
            //@方向盘转车
            TurnKart(deltaTime, ref rotationStream);

            StartDrift (m_CurrentGroundInfo, nextGroundInfo, rotationStream);
            StopDrift (deltaTime);

            //计算速度
            CalculateDrivingVelocity (deltaTime, m_CurrentGroundInfo, rotationStream);

            Vector3 penetrationOffset = SolvePenetration (rotationStream);//Capsule: 胶囊（卡丁车）

            penetrationOffset = ProcessVelocityCollisions (deltaTime, rotationStream, penetrationOffset);

            //修改转向
            rotationStream = Quaternion.RotateTowards (m_Rigidbody.rotation, rotationStream, rotationCorrectionSpeed * deltaTime);
            //修改位置
            AdjustVelocityByPenetrationOffset (deltaTime, ref penetrationOffset);

            if (!IsOtherKart) {
                //MySelf
                m_Rigidbody.MoveRotation (rotationStream);
                m_Rigidbody.MovePosition (m_RigidbodyPosition + m_Movement);
            } else {
                if (syncPosition.y > -50.0f) {
                    Quaternion lerpRot = Quaternion.Lerp (rotationStream, syncRotation, 0.2f);
                    Vector3 lerpPos = Vector3.Lerp (m_RigidbodyPosition + m_Movement, syncPosition, 0.1f);

                    m_Rigidbody.MoveRotation (lerpRot);
                    m_Rigidbody.MovePosition (lerpPos);
                }
            }
        }

        /// <summary>
        /// Removes all the temporary modifiers added through velocity collisions.  They will be re-added in ProcessVelocityCollisions if they still apply.
        /// </summary>
        void ClearTempModifiers () {
            for (int i = 0; i < m_TempModifiers.Count; i++) {
                m_CurrentModifiers.Remove (m_TempModifiers[i]);
            }

            m_TempModifiers.Clear ();
        }

        /// <summary>
        /// Determines how much the kart should be moved due to its collider overlapping with others.
        /// </summary>
        Vector3 SolvePenetration (Quaternion rotationStream) {
            Vector3 summedOffset = Vector3.zero;
            for (var solveIterations = 0; solveIterations < k_MaxPenetrationSolves; solveIterations++) {
                summedOffset = ComputePenetrationOffset (rotationStream, summedOffset);
            }

            return summedOffset;
        }

        /// <summary>
        /// Computes the penetration offset for a single iteration.
        /// </summary>
        /// <param name="rotationStream">The current rotation of the kart.</param>
        /// <param name="summedOffset">How much the kart's capsule is offset so far.</param>
        /// <returns>How much the kart's capsule should be offset after this solve.</returns>
        Vector3 ComputePenetrationOffset (Quaternion rotationStream, Vector3 summedOffset) {
            var capsuleAxis = rotationStream * Vector3.forward * m_Capsule.height * 0.5f;
            var point0 = m_RigidbodyPosition + capsuleAxis + summedOffset;//车头
            var point1 = m_RigidbodyPosition - capsuleAxis + summedOffset;//车尾
            var kartCapsuleHitCount = Physics.OverlapCapsuleNonAlloc (point0, point1, m_Capsule.radius, m_ColliderBuffer, allCollidingLayers, QueryTriggerInteraction.Ignore);

            //重合的卡丁车，产生的反向距离
            for (int i = 0; i < kartCapsuleHitCount; i++) {
                var hitCollider = m_ColliderBuffer[i];
                if (hitCollider == m_Capsule)
                    continue;

                var hitColliderTransform = hitCollider.transform;
                if (Physics.ComputePenetration (m_Capsule, m_RigidbodyPosition + summedOffset, rotationStream, hitCollider, hitColliderTransform.position, hitColliderTransform.rotation, out Vector3 separationDirection, out float separationDistance)) {
                    Vector3 offset = separationDirection * (separationDistance + Physics.defaultContactOffset);
                    if (Mathf.Abs (offset.x) > Mathf.Abs (summedOffset.x))
                        summedOffset.x = offset.x;
                    if (Mathf.Abs (offset.y) > Mathf.Abs (summedOffset.y))
                        summedOffset.y = offset.y;
                    if (Mathf.Abs (offset.z) > Mathf.Abs (summedOffset.z))
                        summedOffset.z = offset.z;
                }
            }

            return summedOffset;
        }

        /// <summary>
        /// Checks whether or not the kart is grounded given a rotation and offset.
        /// </summary>
        /// <param name="deltaTime">The time between frames.</param>
        /// <param name="rotationStream">The rotation the kart will have.</param>
        /// <param name="offset">The offset from the kart's current position.</param>
        /// <returns>Information about the ground from the offset position.</returns>
        GroundInfo CheckForGround (float deltaTime, Quaternion rotationStream, Vector3 offset) {
            GroundInfo groundInfo = new GroundInfo ();
            Vector3 defaultPosition = offset + m_Velocity * deltaTime;
            Vector3 direction = rotationStream * Vector3.down;

            float capsuleRadius = m_Capsule.radius;
            float capsuleTouchingDistance = capsuleRadius + Physics.defaultContactOffset;
            float groundedDistance = capsuleTouchingDistance + k_GroundToCapsuleOffsetDistance;//中心点到地面的距离
            float closeToGroundDistance = Mathf.Max (groundedDistance + capsuleRadius, m_Velocity.y);//头顶到地面的距离

            int hitCount = 0;

            Ray ray = new Ray (defaultPosition + frontGroundRaycast.position, direction);

            bool didHitFront = GetNearestFromRaycast (ray, closeToGroundDistance, groundLayers, QueryTriggerInteraction.Ignore, out RaycastHit frontHit);
            if (didHitFront)
                hitCount++;

            ray.origin = defaultPosition + rightGroundRaycast.position;
            bool didHitRight = GetNearestFromRaycast (ray, closeToGroundDistance, groundLayers, QueryTriggerInteraction.Ignore, out RaycastHit rightHit);
            if (didHitRight)
                hitCount++;

            ray.origin = defaultPosition + leftGroundRaycast.position;
            bool didHitLeft = GetNearestFromRaycast (ray, closeToGroundDistance, groundLayers, QueryTriggerInteraction.Ignore, out RaycastHit leftHit);
            if (didHitLeft)
                hitCount++;

            ray.origin = defaultPosition + rearGroundRaycast.position;
            bool didHitRear = GetNearestFromRaycast (ray, closeToGroundDistance, groundLayers, QueryTriggerInteraction.Ignore, out RaycastHit rearHit);
            if (didHitRear)
                hitCount++;

            //车底盘碰到地
            groundInfo.isCapsuleTouching = frontHit.distance <= capsuleTouchingDistance || rightHit.distance <= capsuleTouchingDistance || leftHit.distance <= capsuleTouchingDistance || rearHit.distance <= capsuleTouchingDistance;
            //车轮碰到地
            groundInfo.isGrounded = frontHit.distance <= groundedDistance || rightHit.distance <= groundedDistance || leftHit.distance <= groundedDistance || rearHit.distance <= groundedDistance;
            //车轮飞起一点都碰到地
            groundInfo.isCloseToGround = hitCount > 0;

            // No hits - normal = Vector3.up
            if (hitCount == 0) {
                groundInfo.normal = Vector3.up;
            }

            // 1 hit - normal = hit.normal
            else if (hitCount == 1) {
                if (didHitFront)
                    groundInfo.normal = frontHit.normal;
                else if (didHitRight)
                    groundInfo.normal = rightHit.normal;
                else if (didHitLeft)
                    groundInfo.normal = leftHit.normal;
                else if (didHitRear)
                    groundInfo.normal = rearHit.normal;
            }

            // 2 hits - normal = hits average
            else if (hitCount == 2) {
                groundInfo.normal = (frontHit.normal + rightHit.normal + leftHit.normal + rearHit.normal) * 0.5f;
            }

            // 3 hits - normal = normal of plane from 3 points
            else if (hitCount == 3) {
                if (!didHitFront)
                    groundInfo.normal = Vector3.Cross (rearHit.point - rightHit.point, leftHit.point - rightHit.point);

                if (!didHitRight)
                    groundInfo.normal = Vector3.Cross (rearHit.point - frontHit.point, leftHit.point - frontHit.point);

                if (!didHitLeft)
                    groundInfo.normal = Vector3.Cross (rightHit.point - frontHit.point, rearHit.point - frontHit.point);

                if (!didHitRear)
                    groundInfo.normal = Vector3.Cross (leftHit.point - rightHit.point, frontHit.point - rightHit.point);
            }

            // 4 hits - normal = average of normals from 4 planes
            else {
                Vector3 normal0 = Vector3.Cross (rearHit.point - rightHit.point, leftHit.point - rightHit.point);
                Vector3 normal1 = Vector3.Cross (rearHit.point - frontHit.point, leftHit.point - frontHit.point);
                Vector3 normal2 = Vector3.Cross (rightHit.point - frontHit.point, rearHit.point - frontHit.point);
                Vector3 normal3 = Vector3.Cross (leftHit.point - rightHit.point, frontHit.point - rightHit.point);

                groundInfo.normal = (normal0 + normal1 + normal2 + normal3) * 0.25f;
            }

            if (groundInfo.isGrounded) {
                float dot = Vector3.Dot (groundInfo.normal, m_Velocity.normalized);
                if (dot > k_VelocityNormalAirborneDot) {//速度方向 和 地面方向 角度小，就是要飞起来了，判定为碰不到地
                    groundInfo.isGrounded = false;
                }
            }

            return groundInfo;
        }

        /// <summary>
        /// Gets information about the nearest object hit by a raycast.
        /// </summary>
        bool GetNearestFromRaycast (Ray ray, float rayDistance, int layerMask, QueryTriggerInteraction query, out RaycastHit hit) {
            int hits = Physics.RaycastNonAlloc (ray, m_RaycastHitBuffer, rayDistance, layerMask, query);

            hit = new RaycastHit ();
            hit.distance = float.PositiveInfinity;

            bool hitSelf = false;
            for (int i = 0; i < hits; i++) {
                if (m_RaycastHitBuffer[i].collider == m_Capsule) {
                    hitSelf = true;
                    continue;
                }

                if (m_RaycastHitBuffer[i].distance < hit.distance)
                    hit = m_RaycastHitBuffer[i];
            }

            if (hitSelf)
                hits--;

            return hits > 0;
        }

        /// <summary>
        /// Checks and applies the modifier to the kart's stats if the kart is not grounded.
        /// </summary>
        void ApplyAirborneModifier (GroundInfo currentGroundInfo) {
            if (airborneModifier != null) {
                if (m_CurrentModifiers.Contains ((IKartModifier) airborneModifier) && currentGroundInfo.isGrounded)
                    m_CurrentModifiers.Remove ((IKartModifier) airborneModifier);
                else if (!m_CurrentModifiers.Contains ((IKartModifier) airborneModifier) && !currentGroundInfo.isGrounded)
                    m_CurrentModifiers.Add ((IKartModifier) airborneModifier);
            }
        }

        /// <summary>
        /// Affects the rotation stream so that the kart is level with the ground.
        /// </summary>
        void GroundNormal (float deltaTime, ref Quaternion rotationStream, GroundInfo currentGroundInfo, GroundInfo nextGroundInfo){// Level with平跟
            Vector3 rigidbodyUp = m_Rigidbody.rotation * Vector3.up;
            Quaternion currentTargetRotation = Quaternion.FromToRotation (rigidbodyUp, currentGroundInfo.normal);
            Quaternion nextTargetRotation = Quaternion.FromToRotation (rigidbodyUp, nextGroundInfo.normal);
            if (nextGroundInfo.isCloseToGround)
                rotationStream = Quaternion.RotateTowards (currentTargetRotation, nextTargetRotation, 0.5f) * rotationStream;
            else
                rotationStream = Quaternion.RotateTowards (rotationStream, nextTargetRotation, airborneOrientationSpeed * deltaTime);
        }

        /// <summary>
        /// Affects the rotation stream based on the steering input.
        /// </summary>
        void TurnKart (float deltaTime, ref Quaternion rotationStream) {
            Vector3 localVelocity = Quaternion.Inverse (rotationStream) * Quaternion.Inverse (m_DriftOffset) * m_Velocity;
            float forwardReverseSwitch = Mathf.Sign (localVelocity.z);
            float modifiedSteering = m_HasControl ? input.Steering * forwardReverseSwitch : 0f;
            if (m_DriftState == DriftState.FacingLeft)
                modifiedSteering = Mathf.Clamp (modifiedSteering, -1f, -minDriftingSteering);
            else if (m_DriftState == DriftState.FacingRight)
                modifiedSteering = Mathf.Clamp (modifiedSteering, minDriftingSteering, 1f);

            //正在动的车，车才能修改Rotation
            float speedProportion = m_Velocity.sqrMagnitude > 0f ? 1f : 0f;

            float turn = m_ModifiedStats.turnSpeed * modifiedSteering * speedProportion * deltaTime;
            Quaternion deltaRotation = Quaternion.Euler (0f, turn, 0f);
            rotationStream = rotationStream * deltaRotation;
        }

        /// <summary>
        /// Calculates the velocity of the kart.
        /// </summary>
        void CalculateDrivingVelocity (float deltaTime, GroundInfo groundInfo, Quaternion rotationStream) {
            Vector3 localVelocity = Quaternion.Inverse (rotationStream) * Quaternion.Inverse (m_DriftOffset) * m_Velocity;
            if (groundInfo.isGrounded) {
                localVelocity.x = Mathf.MoveTowards (localVelocity.x, 0f, m_ModifiedStats.grip * deltaTime);

                float acceleration = m_HasControl ? input.Acceleration : localVelocity.z > 0.05f ? -1f : 0f;

                if (acceleration > -k_Deadzone && acceleration < k_Deadzone) // No acceleration input.
                    localVelocity.z = Mathf.MoveTowards (localVelocity.z, 0f, m_ModifiedStats.coastingDrag * deltaTime);
                else if (acceleration > k_Deadzone) // Positive acceleration input.
                    localVelocity.z = Mathf.MoveTowards (localVelocity.z, m_ModifiedStats.topSpeed, acceleration * m_ModifiedStats.acceleration * deltaTime);
                else if (localVelocity.z > k_Deadzone) // Negative acceleration input and going forwards.
                    localVelocity.z = Mathf.MoveTowards (localVelocity.z, 0f, -acceleration * m_ModifiedStats.braking * deltaTime);
                else // Negative acceleration input and not going forwards.
                    localVelocity.z = Mathf.MoveTowards (localVelocity.z, -m_ModifiedStats.reverseSpeed, -acceleration * m_ModifiedStats.reverseAcceleration * deltaTime);
            }

            if (groundInfo.isCapsuleTouching)
                localVelocity.y = Mathf.Max (0f, localVelocity.y);

            m_Velocity = m_DriftOffset * rotationStream * localVelocity;

            if (!groundInfo.isCapsuleTouching)
                m_Velocity += Vector3.down * m_ModifiedStats.gravity * deltaTime;
        }

        /// <summary>
        /// Affects the velocity of the kart if it hops.
        /// @deprecate 暂时没用
        /// </summary>
        void Hop (Quaternion rotationStream, GroundInfo currentGroundInfo) {
            // TODO: LZ:
            //      not support the following controls right now
#if false
            if (currentGroundInfo.isGrounded && m_Input.HopPressed && m_HasControl) {
                m_Velocity += rotationStream * Vector3.up * m_ModifiedStats.hopHeight;

                OnHop.Invoke ();
            }
#endif
        }

        /// <summary>
        /// Starts a drift if the kart lands with a sufficient turn.
        /// </summary>
        void StartDrift (GroundInfo currentGroundInfo, GroundInfo nextGroundInfo, Quaternion rotationStream) {
            // TODO: LZ:
            //      not support the following controls right now
#if false
            if (m_Input.HopHeld && !currentGroundInfo.isGrounded && nextGroundInfo.isGrounded && m_HasControl && m_DriftState == DriftState.NotDrifting) {
                Vector3 kartForward = rotationStream * Vector3.forward;
                kartForward.y = 0f;
                kartForward.Normalize ();
                Vector3 flatVelocity = m_Velocity;
                flatVelocity.y = 0f;
                flatVelocity.Normalize ();

                float signedAngle = Vector3.SignedAngle (kartForward, flatVelocity, Vector3.up);

                if (signedAngle > minDriftStartAngle && signedAngle < maxDriftStartAngle) {
                    m_DriftOffset = Quaternion.Euler (0f, signedAngle, 0f);
                    m_DriftState = DriftState.FacingLeft;

                    OnDriftStarted.Invoke ();
                } else if (signedAngle < -minDriftStartAngle && signedAngle > -maxDriftStartAngle) {
                    m_DriftOffset = Quaternion.Euler (0f, signedAngle, 0f);
                    m_DriftState = DriftState.FacingRight;

                    OnDriftStarted.Invoke ();
                }
            }
#endif
        }

        /// <summary>
        /// Stops a drift if the hop input is no longer held.
        /// </summary>
        void StopDrift (float deltaTime) {
            // TODO: LZ:
            //      not support the following controls right now
#if false
            if (!m_Input.HopHeld || !m_HasControl) {
                m_DriftOffset = Quaternion.RotateTowards (m_DriftOffset, Quaternion.identity, rotationCorrectionSpeed * deltaTime);
                m_DriftState = DriftState.NotDrifting;

                OnDriftStopped.Invoke ();
            }
#endif
        }

        /// <summary>
        /// Changes the velocity of the kart and processes collisions based on the velocity of the kart.
        /// </summary>
        Vector3 ProcessVelocityCollisions (float deltaTime, Quaternion rotationStream, Vector3 penetrationOffset) {
            Vector3 rayDirection = m_Velocity * deltaTime + penetrationOffset;
            float rayLength = rayDirection.magnitude + .2f;
            Ray sphereCastRay = new Ray (m_RigidbodyPosition, rayDirection);
            int hits = Physics.SphereCastNonAlloc (sphereCastRay, 0.75f, m_RaycastHitBuffer, rayLength, allCollidingLayers, QueryTriggerInteraction.Collide);

            for (int i = 0; i < hits; i++) {
                if (m_RaycastHitBuffer[i].collider == m_Capsule)
                    continue;

                IKartModifier kartModifier = m_RaycastHitBuffer[i].collider.GetComponent<IKartModifier> ();
                if (kartModifier != null) {
                    m_CurrentModifiers.Add (kartModifier);
                    m_TempModifiers.Add (kartModifier);//这个修改，只能影响一帧
                }

                IKartCollider kartCollider = m_RaycastHitBuffer[i].collider.GetComponent<IKartCollider> ();

                //撞上刚体&贴着刚体：换碰撞体为地面（防止产生无限的反作用力）
                if (Mathf.Approximately (m_RaycastHitBuffer[i].distance, 0f))
                    if (Physics.Raycast (m_RigidbodyPosition, rotationStream * Vector3.down, out RaycastHit hit, rayLength + 0.5f, allCollidingLayers, QueryTriggerInteraction.Collide))
                        m_RaycastHitBuffer[i] = hit;

                if (kartCollider != null) {
                    m_Velocity = kartCollider.ModifyVelocity (this, m_RaycastHitBuffer[i]);
                    //撞向刚体（不是地面）
                    if (Mathf.Abs (Vector3.Dot (m_RaycastHitBuffer[i].normal, Vector3.up)) <= .2f) {
                        OnKartCollision.Invoke ();
                    }
                } else {
                    //再检查一遍 （防止和前面的kart重合）
                    penetrationOffset = ComputePenetrationOffset (rotationStream, penetrationOffset);
                }
            }

            return penetrationOffset;
        }

        /// <summary>
        /// So that the velocity doesn't keep forcing a kart into a collider, the velocity is reduced by the penetrationOffset without flipping the direction of the velocity.
        /// </summary>
        /// <param name="deltaTime">The time between frames.</param>
        /// <param name="penetrationOffset">The amount the kart needs to be moved in order to not overlap other colliders.</param>
        void AdjustVelocityByPenetrationOffset (float deltaTime, ref Vector3 penetrationOffset) {
            // Find how much of the velocity is in the penetration offset's direction.
            Vector3 penetrationProjection = Vector3.Project (m_Velocity * deltaTime, penetrationOffset);

            // If the projection and offset are in opposite directions (more than 90 degrees between the velocity and offset) ...
            if (Vector3.Dot (penetrationOffset, penetrationProjection) < 0f) {
                // ... and if the offset is larger than the projection...
                if (penetrationOffset.sqrMagnitude > penetrationProjection.sqrMagnitude) {
                    // ... then reduce the velocity by the equivalent velocity of the projection and the the offset by the projection.
                    m_Velocity -= penetrationProjection / deltaTime;
                    penetrationOffset += penetrationProjection;
                } else // If the offset is smaller than the projection...
                {
                    // ... then reduce the velocity by the equivalent velocity of the offset and then there is the offset remaining.
                    m_Velocity += penetrationOffset / deltaTime;
                    penetrationOffset = Vector3.zero;
                }
            }

            m_Movement = m_Velocity * deltaTime + penetrationOffset;
        }

        /// <summary>
        /// This adds a modifier to the karts stats.  This might be something like a speed boost pickup being activated.
        /// </summary>
        /// <param name="kartModifier">The modifier to the kart's stats.</param>
        public void AddKartModifier (IKartModifier kartModifier) {
            m_CurrentModifiers.Add (kartModifier);
        }

        /// <summary>
        /// This removes a previously added modifier to the karts stats.  This might be something like a speed boost that has just run out.
        /// </summary>
        /// <param name="kartModifier"></param>
        public void RemoveKartModifier (IKartModifier kartModifier) {
            m_CurrentModifiers.Remove (kartModifier);
        }

        /// <summary>
        /// This exists as part of the IKartCollider interface.  It is called when a kart collides with this kart.
        /// </summary>
        /// <param name="collidingKart">The kart that has collided with this kart.</param>
        /// <param name="collisionHit">Data for the collision.</param>
        /// <returns>The velocity of the colliding kart once it has been modified.</returns>
        public Vector3 ModifyVelocity (IKartInfo collidingKart, RaycastHit collisionHit) {
            float weightDifference = collidingKart.CurrentStats.weight - m_ModifiedStats.weight;
            if (weightDifference <= 0f) {
                Vector3 toCollidingKart = (collidingKart.Position - m_RigidbodyPosition).normalized;
                return collidingKart.Velocity + toCollidingKart * (kartToKartBump - weightDifference);
            }

            return collidingKart.Velocity;
        }

        /// <summary>
        /// This exists as part of the IMovable interface.  Typically it is called by the TrackManager when the race starts.
        /// </summary>
        public void EnableControl () {
            m_HasControl = true;
        }

        /// <summary>
        /// This exists as part of the IMovable interface.  Typically it is called by the TrackManager when the kart finishes its final lap.
        /// </summary>
        public void DisableControl () {
            m_HasControl = false;
            m_DriftState = DriftState.NotDrifting;
        }

        /// <summary>
        /// This exists as part of the IMovable interface.  Typically it is called by the TrackManager to determine whether control should be re-enabled after a reposition. 
        /// </summary>
        /// <returns></returns>
        public bool IsControlled () {
            return m_HasControl;
        }

        /// <summary>
        /// This exists as part of the IMovable interface.  It is used to move the kart to a specific position for example to replace it when the kart falls off the track.
        /// </summary>
        public void ForceMove (Vector3 positionDelta, Quaternion rotationDelta) {
            m_Velocity = Vector3.zero;
            m_DriftState = DriftState.NotDrifting;
            m_RepositionPositionDelta = positionDelta;
            m_RepositionRotationDelta = rotationDelta;
        }

        /// <summary>
        /// This exists as part of the IMovable interface.
        /// </summary>
        /// <returns>The racer component implementation of the IRacer interface.</returns>
        public IRacer GetRacer () {
            return m_Racer;
        }

        /// <summary>
        /// This exists as part of the IMovable interface.
        /// </summary>
        /// <returns>The implementation of IKartInfo for this script.</returns>
        public IKartInfo GetKartInfo () {
            return this;
        }
    }
}