/*
Solution: AudioAndTTS.sln
  - Project: AudioPlayerLibrary (Class Library, .NET Framework 4.8)
  - Project: TextToSpeechLibrary (Class Library, .NET Framework 4.8)

Each project outputs a DLL. You can reference these from any WPF or other .NET Framework application.
*/

// -------------------------------
// Project: AudioPlayerLibrary
// File: IAudioPlayer.cs
// -------------------------------


namespace SANJET.SANJET.Core.Interfaces
{
    /// <summary>
    /// Defines methods to play and stop pre-stored audio files.
    /// </summary>
    public interface IAudioPlayerService
    {
        /// <summary>
        /// Plays the audio file located at the specified path.
        /// </summary>
        /// <param name="filePath">Path to the audio file (WAV, MP3).</param>
        void Play(string filePath);

        /// <summary>
        /// Stops playback if currently playing.
        /// </summary>
        void Stop();

        /// <summary>
        /// Indicates whether playback is active.
        /// </summary>
        bool IsPlaying { get; }
    }
}
