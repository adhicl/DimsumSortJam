using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

// Compiled in the Editor too (when the build target is Android) so the Play Games path is
// type-checked on every script reload instead of only failing at Android build time. The
// Editor is excluded at runtime instead — see TrySignInWithPlayGamesAsync.
#if GPGS_ENABLED && UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

namespace Controllers
{
    /// <summary>
    /// Boots Unity Gaming Services and signs the player in. Lives as a singleton GameObject
    /// in the Loading scene (mirrors RewardedAdController.Instance) and survives scene loads,
    /// so anything that needs a player id can reach it without Zenject injection.
    ///
    /// Sign-in order on Android: Google Play Games first (the player keeps their progress
    /// across devices and reinstalls), falling back to anonymous if Play Games is
    /// unavailable, declined, or the plugin is not installed. Every other platform signs in
    /// anonymously.
    ///
    /// Play Games sign-in is compiled out unless the GPGS_ENABLED scripting define symbol is
    /// set — see next_step.md, "Google Play login". Without it the game still
    /// builds and runs on anonymous auth.
    /// </summary>
    public class GameServicesController : MonoBehaviour
    {
        public static GameServicesController Instance { get; private set; }

        [Tooltip("Sign in anonymously instead of Google Play Games (useful while testing).")]
        [SerializeField] private bool forceAnonymousSignIn = false;

        /// <summary>Fires once the player is signed in. Late subscribers are invoked immediately.</summary>
        public event Action OnSignedIn;

        /// <summary>Fires when sign-in failed outright; the game should carry on offline.</summary>
        public event Action<string> OnSignInFailed;

        public bool IsSignedIn { get; private set; }

        /// <summary>UGS player id, or null until sign-in completes.</summary>
        public string PlayerId { get; private set; }

        /// <summary>True when the player is on a real Play Games account rather than an anonymous one.</summary>
        public bool IsPlayGamesAccount { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    await UnityServices.InitializeAsync();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameServices] UGS init failed: {e.Message}");
                OnSignInFailed?.Invoke(e.Message);
                return;
            }

            if (AuthenticationService.Instance.IsSignedIn)
            {
                MarkSignedIn();
                return;
            }

            AuthenticationService.Instance.SignedOut += () => IsSignedIn = false;
            AuthenticationService.Instance.Expired += () => _ = SignInAsync();

            await SignInAsync();
        }

        private async Task SignInAsync()
        {
            // Try Play Games first so returning players land back on their own account.
            if (!forceAnonymousSignIn && await TrySignInWithPlayGamesAsync())
            {
                IsPlayGamesAccount = true;
                MarkSignedIn();
                return;
            }

            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                MarkSignedIn();
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameServices] Anonymous sign-in failed: {e.Message}");
                OnSignInFailed?.Invoke(e.Message);
            }
        }

        /// <summary>
        /// Authenticates with Play Games, exchanges the session for a server auth code, and
        /// hands that code to UGS. Returns false on any failure so the caller can fall back
        /// to anonymous — a player who declines the Play Games prompt should still be able
        /// to play.
        /// </summary>
        private async Task<bool> TrySignInWithPlayGamesAsync()
        {
#if GPGS_ENABLED && UNITY_ANDROID
            // Play Games has no Editor implementation — the plugin's dummy client never
            // completes, so skip straight to anonymous rather than hanging on the callback.
            if (Application.isEditor) return false;

            try
            {
                var status = await AuthenticatePlayGamesAsync();
                if (status != SignInStatus.Success)
                {
                    Debug.Log($"[GameServices] Play Games sign-in unavailable ({status}); using anonymous.");
                    return false;
                }

                string authCode = await RequestServerSideAccessAsync();
                if (string.IsNullOrEmpty(authCode))
                {
                    Debug.LogWarning("[GameServices] Play Games returned an empty auth code; using anonymous.");
                    return false;
                }

                await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(authCode);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameServices] Play Games sign-in failed: {e.Message}");
                return false;
            }
#else
            await Task.CompletedTask;
            return false;
#endif
        }

#if GPGS_ENABLED && UNITY_ANDROID
        private static Task<SignInStatus> AuthenticatePlayGamesAsync()
        {
            var tcs = new TaskCompletionSource<SignInStatus>();
            PlayGamesPlatform.Instance.Authenticate(status => tcs.TrySetResult(status));
            return tcs.Task;
        }

        private static Task<string> RequestServerSideAccessAsync()
        {
            var tcs = new TaskCompletionSource<string>();
            // forceRefreshToken: false — a cached code is fine, UGS only needs a one-shot exchange.
            PlayGamesPlatform.Instance.RequestServerSideAccess(false, code => tcs.TrySetResult(code));
            return tcs.Task;
        }
#endif

        private void MarkSignedIn()
        {
            IsSignedIn = true;
            PlayerId = AuthenticationService.Instance.PlayerId;

            Debug.Log($"[GameServices] Signed in as {PlayerId} " +
                      $"({(IsPlayGamesAccount ? "Google Play Games" : "anonymous")}).");

            OnSignedIn?.Invoke();
        }

        /// <summary>
        /// Runs <paramref name="action"/> once the player is signed in, immediately if that
        /// already happened. Saves callers from racing the async sign-in.
        /// </summary>
        public void WhenSignedIn(Action action)
        {
            if (action == null) return;

            if (IsSignedIn) action();
            else OnSignedIn += action;
        }

        /// <summary>
        /// Upgrades an existing anonymous account to Play Games, keeping the current progress
        /// and player id. Use this behind a "Sign in with Google Play" button rather than
        /// signing out and back in, which would strand the anonymous account's data.
        /// </summary>
        public async Task<bool> LinkWithPlayGamesAsync()
        {
#if GPGS_ENABLED && UNITY_ANDROID
            if (Application.isEditor) return false;
            if (!IsSignedIn || IsPlayGamesAccount) return false;

            try
            {
                var status = await AuthenticatePlayGamesAsync();
                if (status != SignInStatus.Success) return false;

                string authCode = await RequestServerSideAccessAsync();
                if (string.IsNullOrEmpty(authCode)) return false;

                await AuthenticationService.Instance.LinkWithGooglePlayGamesAsync(authCode);
                IsPlayGamesAccount = true;
                return true;
            }
            catch (AuthenticationException e) when (e.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
            {
                // This Play Games account belongs to another UGS player. Signing in outright
                // switches to it, at the cost of abandoning the anonymous progress.
                Debug.LogWarning("[GameServices] Play Games account already linked to another player.");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameServices] Link with Play Games failed: {e.Message}");
                return false;
            }
#else
            await Task.CompletedTask;
            return false;
#endif
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
