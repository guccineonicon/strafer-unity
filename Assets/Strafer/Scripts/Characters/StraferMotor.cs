using UnityEngine;

namespace Strafer.Characters
{
    /// <summary>
    /// Moves the character with snappy, high-acceleration arena-shooter physics.
    ///
    /// The velocity math reproduces Unreal's CharacterMovementComponent, which the
    /// original version of Strafer was tuned with:
    /// - On the ground, input accelerates toward walk speed while "turning friction"
    ///   bends existing velocity toward the input direction. With no input, friction
    ///   and a constant braking deceleration bring the character to a stop.
    /// - In the air, input accelerates at a fraction of the ground rate (air control)
    ///   and nothing slows horizontal movement down.
    ///
    /// Dash and roll take over movement while active; see <see cref="Suspended"/>.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DefaultExecutionOrder(-10)]
    public class StraferMotor : MonoBehaviour
    {
        // Unreal brakes in fixed-size slices so very low frame rates do not overshoot zero.
        private const float MaxBrakingStep = 1f / 33f;

        // Below this speed, braking snaps the character to a stop.
        private const float BrakeToStopSpeed = 0.1f;

        // Small downward speed while grounded so the controller keeps contact on slopes and steps.
        private const float GroundStickSpeed = 2f;

        [Tooltip("Acceleration from input, in m/s². Higher values reach full speed faster.")]
        [SerializeField, Min(0f)]
        private float maxAcceleration = 80f;

        [Tooltip("Constant deceleration applied on the ground when there is no input, in m/s².")]
        [SerializeField, Min(0f)]
        private float brakingDeceleration = 40f;

        [Tooltip("How quickly velocity turns toward the input direction on the ground. Also scales braking.")]
        [SerializeField, Min(0f)]
        private float groundFriction = 8f;

        [Tooltip("Multiplier on ground friction while braking with no input.")]
        [SerializeField, Min(0f)]
        private float brakingFrictionFactor = 2f;

        [Tooltip("Fraction of ground acceleration available while airborne.")]
        [SerializeField, Range(0f, 1f)]
        private float airControl = 0.6f;

        [Tooltip("Air control is multiplied by this when nearly stationary in the air, so a standing jump can still be steered.")]
        [SerializeField, Min(0f)]
        private float airControlBoostMultiplier = 2f;

        [Tooltip("Below this horizontal speed (m/s) the air control boost applies.")]
        [SerializeField, Min(0f)]
        private float airControlBoostSpeedThreshold = 0.25f;

        [Tooltip("Upward launch speed of a jump, in m/s.")]
        [SerializeField, Min(0f)]
        private float jumpVelocity = 5.2f;

        [Tooltip("Multiplier on the project's gravity.")]
        [SerializeField, Min(0f)]
        private float gravityScale = 1.3f;

        private StraferCharacter character;
        private CharacterController controller;
        private Vector3 velocity;
        private bool jumpRequested;

        /// <summary>Current velocity in m/s.</summary>
        public Vector3 Velocity
        {
            get { return velocity; }
            set { velocity = value; }
        }

        public bool IsGrounded { get; private set; }

        /// <summary>
        /// While true, gravity, friction, and input are not applied, so another system
        /// (dash and roll) can move the character directly with <see cref="MoveDirect"/>.
        /// </summary>
        public bool Suspended { get; set; }

        private void Awake()
        {
            character = GetComponent<StraferCharacter>();
            controller = GetComponent<CharacterController>();
        }

        /// <summary>
        /// Asks for a jump on the next movement update. It happens only if the character
        /// is on the ground and able to act at that moment; otherwise the request is dropped.
        /// </summary>
        public void RequestJump()
        {
            jumpRequested = true;
        }

        /// <summary>Stops all movement immediately.</summary>
        public void ResetMotion()
        {
            velocity = Vector3.zero;
            jumpRequested = false;
        }

        /// <summary>Moves the character by <paramref name="delta"/>, sliding along anything it hits.</summary>
        public CollisionFlags MoveDirect(Vector3 delta)
        {
            if (!controller.enabled)
            {
                return CollisionFlags.None;
            }

            CollisionFlags flags = controller.Move(delta);
            IsGrounded = controller.isGrounded;
            return flags;
        }

        private void Update()
        {
            if (!controller.enabled || Suspended)
            {
                jumpRequested = false;
                return;
            }

            Simulate(Time.deltaTime);
        }

        private void Simulate(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            bool grounded = IsGrounded;
            bool jumped = false;

            if (jumpRequested && grounded && character.CanAct)
            {
                velocity.y = jumpVelocity;
                grounded = false;
                jumped = true;
            }
            jumpRequested = false;

            // A stunned or dead character receives no input but still slows down and falls.
            Vector3 wishDirection = character.CanAct ? character.GetMoveInputWorld() : Vector3.zero;
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            float maxSpeed = character.WalkSpeed;

            if (grounded)
            {
                horizontal = CalculateVelocity(horizontal, wishDirection * maxAcceleration, maxSpeed,
                    groundFriction, brakingDeceleration, deltaTime);
                velocity.y = -GroundStickSpeed;
            }
            else
            {
                float control = airControl;
                if (horizontal.sqrMagnitude < airControlBoostSpeedThreshold * airControlBoostSpeedThreshold)
                {
                    control *= airControlBoostMultiplier;
                }

                // No friction and no braking in the air, so momentum is kept unless the player steers.
                horizontal = CalculateVelocity(horizontal, wishDirection * maxAcceleration * control, maxSpeed,
                    0f, 0f, deltaTime);
                velocity.y -= Mathf.Abs(Physics.gravity.y) * gravityScale * deltaTime;
            }

            velocity.x = horizontal.x;
            velocity.z = horizontal.z;

            CollisionFlags flags = controller.Move(velocity * deltaTime);
            IsGrounded = controller.isGrounded;

            if ((flags & CollisionFlags.Above) != 0 && velocity.y > 0f)
            {
                // Bumped a ceiling: stop rising.
                velocity.y = 0f;
            }

            if ((flags & CollisionFlags.Sides) != 0)
            {
                // Keep only the part of the velocity that actually moved, so running into
                // a wall does not store up speed that releases when the wall ends.
                Vector3 moved = controller.velocity;
                velocity.x = moved.x;
                velocity.z = moved.z;
            }

            if (grounded && !IsGrounded && !jumped)
            {
                // Walked off a ledge. Start falling from rest rather than from the stick speed.
                velocity.y = 0f;
            }
        }

        /// <summary>
        /// One step of Unreal's CharacterMovementComponent::CalcVelocity for horizontal movement.
        /// </summary>
        /// <param name="current">Current horizontal velocity.</param>
        /// <param name="acceleration">Requested acceleration from input. Zero when there is no input.</param>
        /// <param name="maxSpeed">Speed that input acceleration cannot push past.</param>
        /// <param name="friction">Turning friction. Also scaled by the braking friction factor when braking.</param>
        /// <param name="braking">Constant deceleration applied when braking.</param>
        private Vector3 CalculateVelocity(Vector3 current, Vector3 acceleration, float maxSpeed,
            float friction, float braking, float deltaTime)
        {
            bool noInput = acceleration.sqrMagnitude < 1e-8f;
            bool overMaxSpeed = current.sqrMagnitude > maxSpeed * maxSpeed * 1.0001f;
            Vector3 result = current;

            if (noInput || overMaxSpeed)
            {
                // Brake when there is no input, or when something pushed the character past max speed.
                Vector3 beforeBraking = result;
                result = ApplyBraking(result, friction * brakingFrictionFactor, braking, deltaTime);

                // When braking down from above max speed while still pushing forward, stop at max speed.
                if (overMaxSpeed && result.sqrMagnitude < maxSpeed * maxSpeed
                    && Vector3.Dot(acceleration, beforeBraking) > 0f)
                {
                    result = beforeBraking.normalized * maxSpeed;
                }
            }
            else
            {
                // Turning friction: pull the velocity toward the input direction while keeping its speed.
                float speed = result.magnitude;
                Vector3 inputDirection = acceleration.normalized;
                result -= (result - inputDirection * speed) * Mathf.Min(deltaTime * friction, 1f);
            }

            if (!noInput)
            {
                // Input can accelerate up to max speed, but never removes speed above it.
                float speedLimit = result.sqrMagnitude > maxSpeed * maxSpeed * 1.0001f ? result.magnitude : maxSpeed;
                result += acceleration * deltaTime;
                result = Vector3.ClampMagnitude(result, speedLimit);
            }

            return result;
        }

        /// <summary>Unreal's ApplyVelocityBraking: friction proportional to speed plus a constant deceleration.</summary>
        private static Vector3 ApplyBraking(Vector3 velocity, float friction, float deceleration, float deltaTime)
        {
            if (velocity.sqrMagnitude < 1e-8f || deltaTime < 1e-6f)
            {
                return velocity;
            }

            bool noFriction = friction <= 0f;
            bool noDeceleration = deceleration <= 0f;
            if (noFriction && noDeceleration)
            {
                return velocity;
            }

            Vector3 original = velocity;
            Vector3 reverseAcceleration = noDeceleration ? Vector3.zero : -velocity.normalized * deceleration;

            float remaining = deltaTime;
            while (remaining >= 1e-6f)
            {
                float step = (remaining > MaxBrakingStep && !noFriction)
                    ? Mathf.Min(MaxBrakingStep, remaining * 0.5f)
                    : remaining;
                remaining -= step;

                velocity += (-friction * velocity + reverseAcceleration) * step;

                // Braking never reverses direction.
                if (Vector3.Dot(velocity, original) <= 0f)
                {
                    return Vector3.zero;
                }
            }

            if (!noDeceleration && velocity.sqrMagnitude <= BrakeToStopSpeed * BrakeToStopSpeed)
            {
                return Vector3.zero;
            }

            return velocity;
        }
    }
}
