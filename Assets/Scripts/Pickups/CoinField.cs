using System.Collections.Generic;
using BulletHell.Arena;
using BulletHell.Core;
using BulletHell.Player;
using BulletHell.Feedback;
using UnityEngine;
using UnityEngine.Pool;

namespace BulletHell.Pickups
{
    /// <summary>
    /// All the currency coins of the round. Enemies drop coins here; coins near the player are pulled in by a magnet and
    /// collected, adding to the round's earnings. When the last wave is cleared everything left is collected at once.
    /// Coins are pooled and moved from a single loop (no per-coin Update, no allocation while running).
    /// </summary>
    public sealed class CoinField : MonoBehaviour
    {
        [SerializeField] private CoinTuning tuning;
        [SerializeField] private CoinPickup prefab;
        [SerializeField] private PlayerHealth player;

        private readonly List<CoinPickup> active = new List<CoinPickup>();
        private ObjectPool<CoinPickup> pool;
        private RunManager run;
        private int created;

        public int CountActive => active.Count;

        private void Awake()
        {
            run = GameServices.Ensure().Run;
            pool = new ObjectPool<CoinPickup>(Create, c => c.gameObject.SetActive(true), c => c.gameObject.SetActive(false),
                                              c => { if (c != null) Destroy(c.gameObject); }, true, tuning.PoolPrewarm, tuning.PoolMax);

            var warm = new CoinPickup[tuning.PoolPrewarm];
            for (int i = 0; i < warm.Length; i++)
                warm[i] = pool.Get();
            for (int i = 0; i < warm.Length; i++)
                pool.Release(warm[i]);
        }

        /// <summary>Drops a coin worth the given currency at a position (a dying enemy).</summary>
        public void Drop(Vector2 position, int value)
        {
            if (value <= 0)
                return;

            CoinPickup coin = pool.Get();
            coin.ActiveIndex = active.Count;
            active.Add(coin);

            coin.Value = value;
            coin.Engaged = false;
            coin.MagnetSpeed = tuning.MagnetStartSpeed;
            coin.Velocity = Random.insideUnitCircle.normalized * tuning.PopSpeed;
            coin.transform.position = position;
            coin.transform.localScale = Vector3.one * tuning.CoinSize;
        }

        /// <summary>Round end: every coin still on the ground is collected at once.</summary>
        public void CollectAll()
        {
            for (int i = active.Count - 1; i >= 0; i--)
                Collect(active[i], false);
        }

        /// <summary>Removes every coin without collecting it (start of a round).</summary>
        public void Clear()
        {
            for (int i = active.Count - 1; i >= 0; i--)
                Remove(active[i]);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || !player.IsAlive)
                return;

            Vector2 target = player.FeetPosition;
            float collectSqr = tuning.CollectRadius * tuning.CollectRadius;
            float magnetSqr = tuning.MagnetRadius * tuning.MagnetRadius;

            for (int i = active.Count - 1; i >= 0; i--)
            {
                CoinPickup coin = active[i];
                Vector2 position = coin.transform.position;
                Vector2 toPlayer = target - position;
                float distanceSqr = toPlayer.sqrMagnitude;

                if (distanceSqr <= collectSqr)
                {
                    Collect(coin);
                    continue;
                }

                if (coin.Engaged || distanceSqr <= magnetSqr)
                {
                    coin.Engaged = true;
                    coin.MagnetSpeed = Mathf.Min(coin.MagnetSpeed + tuning.MagnetAcceleration * dt, tuning.MagnetMaxSpeed);
                    position += toPlayer / Mathf.Sqrt(distanceSqr) * Mathf.Min(coin.MagnetSpeed * dt, Mathf.Sqrt(distanceSqr));
                }
                else
                {
                    position += coin.Velocity * dt;
                    coin.Velocity = Vector2.MoveTowards(coin.Velocity, Vector2.zero, tuning.PopDrag * dt);
                }
                coin.transform.position = position;
            }
        }

        private void Collect(CoinPickup coin, bool sparkle = true)
        {
            run.AddEarnings(coin.Value);
            GameServices.Ensure().Audio.Play(BulletHell.Audio.SfxId.PickupCoin);
            if (sparkle)
                FeedbackHub.Play(VfxKind.Coin, coin.transform.position, 4);
            Remove(coin);
        }

        private void Remove(CoinPickup coin)
        {
            int index = coin.ActiveIndex;
            if (index >= 0 && index < active.Count && active[index] == coin)
            {
                CoinPickup last = active[active.Count - 1];
                active[index] = last;
                last.ActiveIndex = index;
                active.RemoveAt(active.Count - 1);
            }
            coin.ActiveIndex = -1;
            pool.Release(coin);
        }

        private CoinPickup Create()
        {
            created++;
            if (created > tuning.PoolPrewarm)
                Debug.LogWarning($"CoinField pool grew past its prewarm size ({tuning.PoolPrewarm}): {created} created.", this);

            CoinPickup coin = Instantiate(prefab, transform);
            var coinRenderer = coin.GetComponent<SpriteRenderer>();
            PerspectiveTuning look = GameServices.Ensure().Config.Perspective;
            coinRenderer.color = look.Muted(tuning.CoinColor);
            PlaceholderLook.Outline(coinRenderer, look);
            PlaceholderLook.ContactShadow(coinRenderer, look.ShadowSprite, new Vector2(0.03f, -0.32f), new Vector2(look.ContactShadowWidth * 0.85f, look.ContactShadowWidth * 0.85f * look.ShadowFlatness), look, -2);
            return coin;
        }
    }
}
