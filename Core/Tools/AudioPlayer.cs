// -------------------------------
// Project: AudioPlayer
// File: AudioPlayer.cs
// -------------------------------

#nullable enable // 確保可為 Null 的參考型別是啟用的，或者在專案設定中啟用

using System.IO;
using WMPLib;
using SANJET.Core.Interfaces;


namespace SANJET.Core.Tools
{
    /// <summary>
    /// Implements IAudioPlayer using Windows Media Player COM for broad format support.
    /// </summary>
    public class AudioPlayer : IAudioPlayerService, IDisposable
    {
        private WindowsMediaPlayer? _player; // <--- 修改點：設為可為 Null
        private bool _disposed;

        public AudioPlayer()
        {
            _player = new WindowsMediaPlayer();
        }

        // IsPlaying 的判斷需要考慮 _player 可能為 null 的情況 (如果它在 Dispose 後被查詢)
        // 但通常在 Dispose 後不應該再使用該物件的屬性或方法。
        // 如果 _player 只在 Dispose 時設為 null，那麼在這之前它總是有值的。
        // 但為了更安全，可以加上 null 條件運算子 ?.
        public bool IsPlaying => _player?.playState == WMPPlayState.wmppsPlaying;

        public void Play(string filePath)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AudioPlayer));
            if (_player == null) // 確保 _player 實例存在 (理論上建構函式會初始化)
                throw new InvalidOperationException("Player has not been initialized or has been disposed.");

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Audio file not found.", filePath);

            _player.settings.volume = 100; // 強制設為最大音量
            _player.URL = filePath;
            _player.controls.play();
        }

        public void Stop()
        {
            if (_disposed)
                return; // 或者拋出 ObjectDisposedException，取決於設計

            // 只有當 _player 存在且正在播放時才停止
            if (_player != null && _player.playState == WMPPlayState.wmppsPlaying)
            {
                _player.controls.stop();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // 如果有 Finalizer (~AudioPlayer()) 的話
        }

        // 可選的 Dispose 模式，更完整
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 釋放受控資源
                    if (_player != null)
                    {
                        try
                        {
                            _player.controls.stop();
                            _player.close();
                        }
                        catch (System.Runtime.InteropServices.COMException)
                        {
                            // 處理 WMP COM 物件可能已經失效的情況
                        }
                        // 考慮釋放 COM 物件
                        // System.Runtime.InteropServices.Marshal.ReleaseComObject(_player);
                        _player = null;
                    }
                }

                // 釋放非受控資源 (如果有的話)

                _disposed = true;
            }
        }

        // 如果需要 Finalizer (解構函式)，但通常對於只有受控資源的類別來說不是必須的
        // ~AudioPlayer()
        // {
        //     Dispose(false);
        // }
    }
}