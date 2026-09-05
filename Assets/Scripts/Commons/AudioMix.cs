using UnityEngine;
using UnityEngine.Audio;

namespace Commons
{
    /// <summary>
    /// The one place that knows how muting is wired to the audio mixer: which exposed parameters
    /// carry the two volumes, and what "muted" is in decibels.
    ///
    /// It is a named contract rather than three copies of the same two lines, because the copies
    /// are what let this break quietly. <c>soundMute</c> and <c>musicMute</c> are saved and
    /// reloaded, but nothing else in the game reads them - no <c>AudioSource.mute</c>, no volume -
    /// so the mixer is not one way of muting, it is the only one. When the mixer asset went
    /// missing nothing said so: the parameter names were string literals scattered across three
    /// popups, and the first sign of trouble was a MissingReferenceException at runtime.
    /// </summary>
    public static class AudioMix
    {
        /// <summary>Exposed parameter on the Sfx group. Must match the mixer asset.</summary>
        public const string SfxVolumeParam = "SfxVolume";

        /// <summary>Exposed parameter on the Music group. Must match the mixer asset.</summary>
        public const string MusicVolumeParam = "MusicVolume";

        // -80dB is the mixer's own silence floor - the same value as its suspend threshold - so a
        // muted group is not merely quiet, it stops being mixed at all.
        private const float MutedDb = -80f;
        private const float AudibleDb = 0f;

        /// <summary>
        /// Pushes both saved mute flags at the mixer. Called when the game boots, because a build
        /// starts every run with the mixer at the volumes it was authored with.
        /// </summary>
        public static void Apply(AudioMixer mixer, GameSetting setting, Object context = null)
        {
            if (setting == null)
            {
                Debug.LogError("[Audio] No GameSetting, so the saved mute state cannot be applied.", context);
                return;
            }

            ApplySound(mixer, setting.soundMute, context);
            ApplyMusic(mixer, setting.musicMute, context);
        }

        public static void ApplySound(AudioMixer mixer, bool muted, Object context = null)
        {
            SetVolume(mixer, SfxVolumeParam, muted, context);
        }

        public static void ApplyMusic(AudioMixer mixer, bool muted, Object context = null)
        {
            SetVolume(mixer, MusicVolumeParam, muted, context);
        }

        /// <summary>
        /// Fails loudly but harmlessly. A missing mixer used to throw straight out of the caller,
        /// which cost more than the audio: in the loading screen it aborted the coroutine that
        /// animates the "Loading..." text, and in the settings popups it unwound past
        /// <c>SaveData()</c>, so the toggle neither applied nor persisted. Naming what is wrong and
        /// carrying on leaves the rest of the caller working, and leaves a message that points at
        /// the object whose field is empty.
        ///
        /// <c>== null</c> rather than <c>is null</c> on purpose: a reference to a deleted asset is
        /// not a real null, and only Unity's own comparison recognises it. That is exactly the case
        /// this has to catch - it is how the mixer went missing in the first place.
        /// </summary>
        private static void SetVolume(AudioMixer mixer, string parameter, bool muted, Object context)
        {
            if (mixer == null)
            {
                Debug.LogError($"[Audio] No AudioMixer assigned, so '{parameter}' cannot be set. " +
                               "Assign Assets/Sounds/GameAudioMixer.mixer to the mixer field.", context);
                return;
            }

            if (!mixer.SetFloat(parameter, muted ? MutedDb : AudibleDb))
            {
                // The asset is there but does not expose this parameter - a different mixer, or one
                // whose exposed parameter was renamed. Audio that will not mute is the only other
                // symptom, and it looks like a broken button rather than a broken reference.
                Debug.LogError($"[Audio] Mixer '{mixer.name}' exposes no parameter named " +
                               $"'{parameter}', so that channel cannot be muted.", context);
            }
        }
    }
}
