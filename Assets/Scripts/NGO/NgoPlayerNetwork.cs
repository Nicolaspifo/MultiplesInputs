using NetworkingLab.Combat;
using NetworkingLab.Player;
using NetworkingLab.PowerUp;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NetworkingLab.NGO
{
    /// <summary>NGO tank: server-authoritative movement, power up broadcast and shooting.</summary>
    public sealed class NgoPlayerNetwork : NetworkBehaviour, IDamageable
    {
        private static int nextPlayerNumber;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player 1";
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private TextMesh playerLabel;
        [SerializeField] private Transform firePoint;
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField] private LayerMask obstacleLayers = ~0;
        [SerializeField, Min(0.05f)] private float collisionRadius = 0.4f;
        [SerializeField, Min(0.05f)] private float collisionHeight = 0.8f;
        [SerializeField, Min(0.05f)] private float fireCooldown = 0.35f;
        [SerializeField, Min(1f)] private float projectileSpeed = 18f;
        [SerializeField, Min(0.1f)] private float projectileLifetime = 3f;
        [SerializeField, Min(1)] private int projectileDamage = 1;
        [SerializeField, Min(1)] private int maxHealth = 3;
        [SerializeField, Min(0f)] private float respawnDelay = 1.5f;
        [SerializeField, Min(0f)] private float spawnHeight = 0.75f;

        private readonly NetworkVariable<int> playerNumber = new(0,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> health = new(3,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<bool> alive = new(true,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private InputActionMap actionMap;
        private InputAction moveAction;
        private InputAction powerUpAction;
        private Vector2 pendingServerInput;
        private Vector3 aimDirection = Vector3.forward;
        private float nextFireTime;
        private float serverRespawnAt;
        private GameObject projectilePrefab;
        private TankFacing facing;

        public int PlayerNumber => playerNumber.Value;
        public int Health => health.Value;
        public bool IsAlive => alive.Value;
        public Transform DamageRoot => transform;

        public static void ResetSequence() => nextPlayerNumber = 0;

        public void Configure(InputActionAsset actions, string mapName, Renderer renderer, TextMesh label, float speed)
        {
            inputActions = actions;
            actionMapName = mapName;
            bodyRenderer = renderer;
            playerLabel = label;
            moveSpeed = speed;
        }

        public override void OnNetworkSpawn()
        {
            playerNumber.OnValueChanged += OnPlayerNumberChanged;
            health.OnValueChanged += OnHealthChanged;
            alive.OnValueChanged += OnAliveChanged;
            projectilePrefab = CombatAssets.ProjectilePrefab;
            facing = GetComponent<TankFacing>();

            if (IsServer)
            {
                int assigned = ++nextPlayerNumber;
                playerNumber.Value = assigned;
                transform.position = SpawnPosition(assigned);
                health.Value = Mathf.Max(1, maxHealth);
                alive.Value = true;
            }

            ApplyIdentity(playerNumber.Value);
            SetAliveVisual(alive.Value);

            if (IsOwner && IsClient)
            {
                ResolveInput();
                powerUpAction.performed += OnPowerUpPerformed;
                actionMap.Enable();
            }
        }

        public override void OnNetworkDespawn()
        {
            playerNumber.OnValueChanged -= OnPlayerNumberChanged;
            health.OnValueChanged -= OnHealthChanged;
            alive.OnValueChanged -= OnAliveChanged;
            if (powerUpAction != null)
            {
                powerUpAction.performed -= OnPowerUpPerformed;
            }

            actionMap?.Disable();
        }

        private void Update()
        {
            if (IsServer && !alive.Value && Time.time >= serverRespawnAt)
            {
                ServerRespawn();
            }

            if (!IsOwner || !IsClient || moveAction == null)
            {
                return;
            }

            Vector2 move = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
            if (move.sqrMagnitude > 0.0004f)
            {
                aimDirection = new Vector3(move.x, 0f, move.y).normalized;
            }

            SubmitMoveRpc(move);
        }

        private void FixedUpdate()
        {
            if (!IsServer || !IsSpawned || !alive.Value)
            {
                return;
            }

            Vector2 move = Vector2.ClampMagnitude(pendingServerInput, 1f);
            Vector3 current = transform.position;
            Vector3 desired = current + new Vector3(move.x, 0f, move.y) * (moveSpeed * Time.fixedDeltaTime);
            transform.position = MovementCollision.Resolve(transform, current, desired, obstacleLayers, collisionRadius, collisionHeight);

            if (facing != null && move.sqrMagnitude > 0.0004f)
            {
                facing.SetDirection(new Vector3(move.x, 0f, move.y));
            }
        }

        public bool ApplyDamage(int amount)
        {
            if (!IsServer || !IsSpawned || amount <= 0 || !alive.Value)
            {
                return false;
            }

            int next = Mathf.Max(0, health.Value - amount);
            health.Value = next;
            if (next <= 0)
            {
                alive.Value = false;
                serverRespawnAt = Time.time + respawnDelay;
            }

            return true;
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable, InvokePermission = RpcInvokePermission.Owner)]
        private void SubmitMoveRpc(Vector2 move)
        {
            pendingServerInput = Vector2.ClampMagnitude(move, 1f);
        }

        private void OnPowerUpPerformed(InputAction.CallbackContext context)
        {
            RequestPowerUpRpc();
            TryFire();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void RequestPowerUpRpc()
        {
            if (!IsSpawned || playerNumber.Value <= 0)
            {
                return;
            }

            AnnouncePowerUpRpc(playerNumber.Value);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void AnnouncePowerUpRpc(int number)
        {
            PowerUpEvents.RaiseActivated($"Player {number}");
        }

        private void TryFire()
        {
            if (!IsOwner || !IsSpawned || !alive.Value || Time.time < nextFireTime)
            {
                return;
            }

            nextFireTime = Time.time + fireCooldown;
            Vector3 origin = FireOrigin(aimDirection);
            FireServerRpc(origin, aimDirection);
        }

        private Vector3 FireOrigin(Vector3 direction)
        {
            if (firePoint != null)
            {
                return firePoint.position;
            }

            float height = 0.6f;
            Collider body = GetComponent<Collider>();
            if (body != null)
            {
                height = Mathf.Max(0.35f, body.bounds.extents.y);
            }

            return transform.position + Vector3.up * height + direction * 0.9f;
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, InvokePermission = RpcInvokePermission.Owner)]
        private void FireServerRpc(Vector3 origin, Vector3 direction)
        {
            if (!alive.Value)
            {
                return;
            }

            SpawnProjectile(origin, direction, true);
            FireVisualRpc(origin, direction);
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Reliable)]
        private void FireVisualRpc(Vector3 origin, Vector3 direction)
        {
            SpawnProjectile(origin, direction, false);
        }

        private void SpawnProjectile(Vector3 origin, Vector3 direction, bool authoritative)
        {
            if (projectilePrefab == null)
            {
                projectilePrefab = CombatAssets.ProjectilePrefab;
            }

            if (projectilePrefab == null)
            {
                return;
            }

            Vector3 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
            GameObject shot = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(dir, Vector3.up));
            if (shot.TryGetComponent(out Projectile projectile))
            {
                projectile.Launch(dir, transform, authoritative, projectileSpeed, projectileLifetime, projectileDamage);
            }
        }

        private void ServerRespawn()
        {
            if (!IsServer)
            {
                return;
            }

            health.Value = Mathf.Max(1, maxHealth);
            alive.Value = true;
            pendingServerInput = Vector2.zero;
            transform.position = SpawnPosition(Mathf.Max(1, playerNumber.Value));
        }

        private void ResolveInput()
        {
            actionMap = inputActions.FindActionMap(actionMapName, true);
            moveAction = actionMap.FindAction("Move", true);
            powerUpAction = actionMap.FindAction("PowerUp", true);
        }

        private void OnPlayerNumberChanged(int previous, int current) => ApplyIdentity(current);

        private void OnHealthChanged(int previous, int current)
        {
            if (current <= 0 && previous > 0)
            {
                CombatFx.SpawnExplosion(transform.position, CombatFx.TankColor);
            }
        }

        private void OnAliveChanged(bool previous, bool current) => SetAliveVisual(current);

        private void SetAliveVisual(bool value)
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = value;
            }

            foreach (Collider collider in GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = value;
            }
        }

        private void ApplyIdentity(int number)
        {
            if (number <= 0)
            {
                return;
            }

            gameObject.name = $"NGO Player {number}";
            if (playerLabel != null)
            {
                playerLabel.text = $"Player {number}";
            }

            if (bodyRenderer != null)
            {
                bodyRenderer.material.color = PlayerColor(number);
            }
        }

        private Vector3 SpawnPosition(int number)
        {
            NgoSpawnPoints points = FindFirstObjectByType<NgoSpawnPoints>();
            if (points != null && points.Count > 0)
            {
                Vector3 position = points.GetPosition(number - 1);
                position.y = spawnHeight;
                return position;
            }

            Vector3[] positions = { new(-5f, 0f, 3f), new(5f, 0f, 3f), new(-5f, 0f, -3f), new(5f, 0f, -3f) };
            Vector3 fallback = positions[(Mathf.Max(1, number) - 1) % positions.Length];
            fallback.y = spawnHeight;
            return fallback;
        }

        private static Color PlayerColor(int number)
        {
            Color[] colors = { new(.1f, .55f, 1f), new(1f, .25f, .2f), new(.25f, .85f, .35f), new(1f, .72f, .12f) };
            return colors[(Mathf.Max(1, number) - 1) % colors.Length];
        }
    }
}
