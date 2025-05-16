using Microsoft.Extensions.Logging;
using NAudio.Wave;
using SANJET.Core.Interfaces;
using System;

namespace SANJET.Core.Services
{
    public class AudioPlayerService : IAudioPlayerService, IDisposable
    {
        private readonly ILogger<AudioPlayerService> _logger;
        private WaveOutEvent? _waveOut;
        private AudioFileReader? _audioFileReader;

        public AudioPlayerService(ILogger<AudioPlayerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("AudioPlayerService initialized.");
        }

        public bool IsPlaying
        {
            get
            {
                bool isPlaying = _waveOut != null && _waveOut.PlaybackState == PlaybackState.Playing;
                _logger.LogDebug("Checked IsPlaying: {IsPlaying}", isPlaying);
                return isPlaying;
            }
        }

        public void Play(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    _logger.LogWarning("Play failed: File path is empty.");
                    return;
                }

                DisposePlayer();
                _waveOut = new WaveOutEvent();
                _audioFileReader = new AudioFileReader(filePath);
                _waveOut.Init(_audioFileReader);
                _waveOut.Play();
                _logger.LogInformation("Playing audio file: {FilePath}, Duration: {Duration}s", filePath, _audioFileReader.TotalTime.TotalSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to play audio file: {FilePath}", filePath);
            }
        }

        public void Stop()
        {
            try
            {
                if (_waveOut != null)
                {
                    _waveOut.Stop();
                    _logger.LogInformation("Audio playback stopped.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop audio playback.");
            }
            finally
            {
                DisposePlayer();
            }
        }

        private void DisposePlayer()
        {
            if (_waveOut != null)
            {
                _waveOut.Dispose();
                _waveOut = null;
            }
            if (_audioFileReader != null)
            {
                _audioFileReader.Dispose();
                _audioFileReader = null;
            }
        }

        public void Dispose()
        {
            DisposePlayer();
            GC.SuppressFinalize(this);
        }
    }
}