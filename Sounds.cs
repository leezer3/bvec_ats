using OpenBveApi.Runtime;

namespace Plugin
{

    /// <summary>Manages the playback of sounds.</summary>
    internal static class SoundManager
    {

        // members
        /// <summary>An array which stores the returned sound handles associated with sound index playback.</summary>
        private static SoundHandle[] SoundHandles;
        /// <summary>An array which stores whether or not the sound index was looped on the previous playback.</summary>
        private static bool[] IsLooped;
        /// <summary>The last volume value assigned in the previous playback.</summary>
        private static double[] LastVolume;
        /// <summary>The last pitch value assigned in the previous playback.</summary>
        private static double[] LastPitch;
        /// <summary>The callback function for playing sounds.</summary>
        private static PlaySoundDelegate PlaySound;

        // constructors

        /// <summary>Initialises the sound manager. Call this method via the Load() method.</summary>
        /// <param name="playSound">The callback function for playing sounds.</param>
        /// <param name="numIndices">The number of sound indices to accomodate.</param>
        internal static void Initialise(PlaySoundDelegate playSound, int numIndices)
        {
            SoundHandles = new SoundHandle[numIndices];
            IsLooped = new bool[numIndices];
            LastVolume = new double[numIndices];
            LastPitch = new double[numIndices];
            PlaySound = playSound;
        }

        // methods

        /// <summary>Plays the sound assigned to the specified index, with a specified volume and pitch, once or in a continuous loop.</summary>
        /// <param name="soundIndex">The sound index to play, as defined in the sound configuration file.</param>
        /// <param name="volume">The playback volume, ranging from 0.0 upwards. A value of 1.0 corresponds to the original volume of the audio file.</param>
        /// <param name="pitch">The playback pitch, ranging from 0.0 upwards. A value of 1.0 corresponds to the original pitch of the audio file.</param>
        /// <param name="loop">Whether or not to play the sound in a continuous loop.</param>
        internal static void Play(int soundIndex, double volume, double pitch, bool loop)
        {
            /* Value validation */
            volume = volume < 0 ? 0 : volume;
            pitch = pitch < 0 ? 0 : pitch;
            if (soundIndex != -1)
            {
                if (SoundHandles[soundIndex] != null)
                {
                    /* A handle already exists, so... */
                    if (IsLooped[soundIndex] && SoundHandles[soundIndex].Playing)
                    {
                        /* The sound is looped...
                         * It is indeed playing already, so just modify the pitch and volume */
                        SoundHandles[soundIndex].Volume = volume;
                        SoundHandles[soundIndex].Pitch = pitch;
                    }
                    else if (volume == LastVolume[soundIndex] && pitch == LastPitch[soundIndex])
                    {
                        /* Handle play-once sounds...
                         * The pitch and volume for this sound handle are the same as they were in the last call, so start playing a new sound instead */
                        SoundHandles[soundIndex].Stop();
                        SoundHandles[soundIndex] = PlaySound(soundIndex, volume, pitch, loop);
                    }
                    else if (SoundHandles[soundIndex].Playing)
                    {
                        /* The handle is already playing and the pitch or volume has been changed since the last playback of this sound handle,
                         * so alter the pitch and volume, and continue the sound */
                        SoundHandles[soundIndex].Pitch = pitch;
                        SoundHandles[soundIndex].Volume = volume;
                    }
                    else
                    {
                        /* Neither pitch or volume have been changed, so start playback with a new sound handle */
                        SoundHandles[soundIndex] = PlaySound(soundIndex, volume, pitch, loop);
                    }
                }
                else
                {
                    /* There is no valid handle, so create a new handle for playback */
                    SoundHandles[soundIndex] = PlaySound(soundIndex, volume, pitch, loop);
                }

                /* Store states for future use */
                IsLooped[soundIndex] = loop;
                LastVolume[soundIndex] = volume;
                LastPitch[soundIndex] = pitch;
            }
        }

        /// <summary>Stops playback of the specified sound index.</summary>
        /// <param name="soundIndex">The sound index where playback is to be to stopped.</param>
        internal static void Stop(int soundIndex)
        {
            if (soundIndex != -1 && SoundHandles[soundIndex] != null)
            {
                SoundHandles[soundIndex].Stop();
                IsLooped[soundIndex] = false;
            }
        }

        /// <summary>Determines whether or not the specified sound index is currently playing.</summary>
        /// <param name="soundIndex">The sound index to query for its playback status.</param>
        /// <returns>Returns true if the sound is currently playing.</returns>
        internal static bool IsPlaying(int soundIndex)
        {
            if (SoundHandles[soundIndex] != null && SoundHandles[soundIndex].Playing)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}