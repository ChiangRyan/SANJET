
namespace SANJET.Core.Interfaces
{
    /// <summary>
    /// Defines methods to perform text-to-speech playback.
    /// </summary>
    public interface ITextToSpeechService
    {
        /// <summary>
        /// Speaks the given text.
        /// </summary>
        /// <param name="text">Text to speak.</param>
        void Speak(string text);

        /// <summary>
        /// Stops ongoing speech playback.</summary>
        void Stop();

        /// <summary>
        /// Gets or sets the speech rate (-10 to 10).</summary>
        int Rate { get; set; }

        /// <summary>
        /// Gets or sets the voice name.</summary>
        string Voice { get; set; }
    }
}