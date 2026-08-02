using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Commons;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using UnityEngine;

namespace Controllers
{
    /// <summary>
    /// Mirrors the player's progress into UGS Cloud Save so a reinstall or a second device
    /// picks up where they left off. Lives as a singleton GameObject in Splash next to
    /// <see cref="GameServicesController"/> and survives scene loads.
    ///
    /// Flow: wait for sign-in, pull the cloud record once, decide whether cloud or local wins
    /// (see <see cref="ResolveAsync"/>), then upload after every <see cref="GameSetting.SaveData"/>
    /// — coalesced, so a burst of saves during a level end is one request.
    ///
    /// Everything here fails soft. Cloud Save is a convenience on top of PlayerPrefs, which
    /// stays the source of truth for the running session; no network means the player still
    /// plays with their local save.
    /// </summary>
    public class CloudSaveController : MonoBehaviour
    {
        public static CloudSaveController Instance { get; private set; }

        /// <summary>Cloud Save key holding the whole snapshot. Keys are per-player, so one is enough.</summary>
        private const string SaveKey = "player_save";

        [SerializeField] private GameSetting gameSetting;

        [Tooltip("Seconds to wait after a save before uploading, so a burst of saves is one request.")]
        [SerializeField] private float uploadDelaySeconds = 3f;

        [Tooltip("Skip all cloud traffic (useful while testing offline behaviour).")]
        [SerializeField] private bool disableCloudSave = false;

        /// <summary>Fires once the initial pull has resolved, whether or not cloud data existed.</summary>
        public event Action<bool> OnSyncCompleted;

        /// <summary>True once the first pull finished; until then the local save is all we have.</summary>
        public bool HasSynced { get; private set; }

        /// <summary>True while an upload is queued or in flight.</summary>
        public bool IsUploading { get; private set; }

        private bool _pendingUpload;
        private float _uploadDueAt;

        // Consecutive upload failures, used to back off. Offline play would otherwise retry
        // every uploadDelaySeconds for as long as the app is open.
        private int _failedUploads;
        private const float MaxRetryDelaySeconds = 60f;

        // Set while ApplySnapshot writes downloaded data, so the SaveData it performs does not
        // bounce straight back up as an "edit".
        private bool _applyingCloudData;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            GameSetting.OnDataSaved += HandleDataSaved;
        }

        private void OnDisable()
        {
            GameSetting.OnDataSaved -= HandleDataSaved;
        }

        private void Start()
        {
            // Both bail-outs mark the sync "done" so LoadingScene starts the game immediately
            // instead of sitting through its cloud-sync timeout for a sync that will never run.
            if (disableCloudSave)
            {
                Debug.Log("[CloudSave] Disabled on this build; local save only.");
                CompleteSync(false);
                return;
            }

            if (gameSetting == null)
            {
                Debug.LogError("[CloudSave] No GameSetting assigned; cloud save is off.");
                CompleteSync(false);
                return;
            }

            StartCoroutine(SyncWhenSignedIn());
        }

        /// <summary>
        /// Waits for <see cref="GameServicesController"/> to exist and finish signing in — both
        /// singletons wake in the same scene with no guaranteed order, so we cannot subscribe
        /// in Awake.
        /// </summary>
        private IEnumerator SyncWhenSignedIn()
        {
            while (GameServicesController.Instance == null) yield return null;

            var signedIn = false;
            GameServicesController.Instance.WhenSignedIn(() => signedIn = true);

            var failed = false;
            void OnFailed(string _) => failed = true;
            GameServicesController.Instance.OnSignInFailed += OnFailed;

            while (!signedIn && !failed) yield return null;

            GameServicesController.Instance.OnSignInFailed -= OnFailed;

            if (failed)
            {
                Debug.LogWarning("[CloudSave] Sign-in failed; running on the local save only.");
                CompleteSync(false);
                yield break;
            }

            var resolve = ResolveAsync();
            while (!resolve.IsCompleted) yield return null;

            // The task swallows its own exceptions; this only guards against an unexpected one.
            if (resolve.IsFaulted)
            {
                Debug.LogWarning($"[CloudSave] Initial sync failed: {resolve.Exception?.GetBaseException().Message}");
                CompleteSync(false);
            }
        }

        /// <summary>
        /// Reconciles the cloud record with the local save. Rules, in order:
        /// <list type="number">
        /// <item>No cloud record — upload what we have.</item>
        /// <item>Local was never saved (fresh install, <c>savedAt == 0</c>) — cloud wins. This is
        /// the case that matters: without it a reinstall would push its defaults over real progress.</item>
        /// <item>Cloud is further along (<c>currentLevel</c>) — cloud wins. Replaying finished
        /// levels is the one loss players actually notice, so it outranks the clock.</item>
        /// <item>Otherwise the newer <c>savedAt</c> wins.</item>
        /// </list>
        /// The snapshot is taken or applied whole. Merging field by field would let a player
        /// keep one device's currency and another's progress.
        /// </summary>
        private async Task ResolveAsync()
        {
            GameSetting.SaveSnapshot cloud;

            try
            {
                var results = await CloudSaveService.Instance.Data.Player.LoadAsync(
                    new HashSet<string> { SaveKey });

                cloud = results.TryGetValue(SaveKey, out Item item)
                    ? item.Value.GetAs<GameSetting.SaveSnapshot>()
                    : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CloudSave] Download failed: {e.Message}. Using the local save.");
                CompleteSync(false);
                return;
            }

            if (cloud == null)
            {
                Debug.Log("[CloudSave] No cloud record yet; uploading the local save.");
                CompleteSync(true);
                await UploadAsync();
                return;
            }

            var local = gameSetting.SavedAt;
            var cloudWins = local == 0
                            || cloud.currentLevel > gameSetting.currentLevel
                            || cloud.savedAt > local;

            if (cloudWins)
            {
                _applyingCloudData = true;
                try
                {
                    gameSetting.ApplySnapshot(cloud);
                }
                finally
                {
                    _applyingCloudData = false;
                }

                Debug.Log($"[CloudSave] Restored cloud save (level {cloud.currentLevel}, {cloud.totalGold} gold).");
                CompleteSync(true);
                return;
            }

            Debug.Log($"[CloudSave] Local save is newer (level {gameSetting.currentLevel}); uploading.");
            CompleteSync(true);
            await UploadAsync();
        }

        private void CompleteSync(bool success)
        {
            if (HasSynced) return;

            HasSynced = true;
            OnSyncCompleted?.Invoke(success);
        }

        private void HandleDataSaved(GameSetting setting)
        {
            if (disableCloudSave || _applyingCloudData) return;
            if (setting != gameSetting) return;

            _pendingUpload = true;
            _uploadDueAt = Time.unscaledTime + uploadDelaySeconds;
        }

        private void Update()
        {
            if (!_pendingUpload || IsUploading) return;
            if (Time.unscaledTime < _uploadDueAt) return;

            _pendingUpload = false;
            _ = UploadAsync();
        }

        /// <summary>
        /// Pushes the current snapshot. Safe to call at any time — it no-ops until sign-in has
        /// happened and only one upload is ever in flight.
        /// </summary>
        public async Task UploadAsync()
        {
            if (disableCloudSave || gameSetting == null || IsUploading) return;
            if (GameServicesController.Instance == null || !GameServicesController.Instance.IsSignedIn) return;

            IsUploading = true;

            try
            {
                await CloudSaveService.Instance.Data.Player.SaveAsync(
                    new Dictionary<string, object> { { SaveKey, gameSetting.CreateSnapshot() } });

                _failedUploads = 0;
            }
            catch (Exception e)
            {
                // Keep the pending flag so the next tick retries rather than dropping the save.
                // The snapshot is rebuilt at send time, so a retry always carries the latest
                // state — a failed upload never replays stale values over newer ones.
                _failedUploads++;
                _pendingUpload = true;
                _uploadDueAt = Time.unscaledTime + Mathf.Min(
                    uploadDelaySeconds * (1 << Mathf.Min(_failedUploads, 5)), MaxRetryDelaySeconds);

                Debug.LogWarning($"[CloudSave] Upload failed (attempt {_failedUploads}): {e.Message}");
            }
            finally
            {
                IsUploading = false;
            }
        }

        /// <summary>
        /// Uploads immediately instead of waiting out the coalescing delay. Android gives no
        /// reliable window at process death, so this is fire-and-forget: it usually lands, and
        /// when it does not the next launch re-uploads from PlayerPrefs.
        /// </summary>
        private void Flush()
        {
            if (!_pendingUpload) return;

            _pendingUpload = false;
            _ = UploadAsync();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Flush();
        }

        private void OnApplicationQuit()
        {
            Flush();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
