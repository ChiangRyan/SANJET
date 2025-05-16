using SANJET.Core.Interfaces;
using System.Speech.Synthesis;

namespace SANJET.Core.Tools
{
    /// <summary>
    /// Implements ISpeechService using System.Speech.Synthesis for offline TTS.
    /// </summary>
    public class SpeechService : ITextToSpeechService, IDisposable
    {
        private readonly SpeechSynthesizer _synthesizer;

        public SpeechService()
        {
            _synthesizer = new SpeechSynthesizer();
            // Set defaults
            Rate = 0;
            Voice = _synthesizer.Voice.Name;
        }

        public int Rate
        {
            get => _synthesizer.Rate;
            set => _synthesizer.Rate = Math.Max(-10, Math.Min(10, value));
        }

        public string Voice
        {
            get => _synthesizer.Voice.Name;
            set
            {
                try
                {
                    _synthesizer.SelectVoice(value);
                }
                catch (Exception)
                {
                    // ignore or log
                }
            }
        }

        public void Speak(string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("Text cannot be null or empty.", nameof(text));

            _synthesizer.SpeakAsyncCancelAll();
            _synthesizer.SpeakAsync(text);
        }

        public void Stop()
        {
            _synthesizer.SpeakAsyncCancelAll();
        }

        public void Dispose()
        {
            _synthesizer?.Dispose();
        }
    }
}

/*
Usage:

// In your WPF application, add references to both DLLs and to WMPLib COM component (Windows Media Player) and System.Speech.

var player = new AudioPlayerLibrary.AudioPlayer();
player.Play("C:\\Media\\sound.mp3");
player.Stop();

var tts = new TextToSpeechLibrary.SpeechService();
tts.Rate = -2;
tts.Speak("Hello, this is a test.");
tts.Stop();
*/

