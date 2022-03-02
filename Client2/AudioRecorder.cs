using System;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Client2 {

    class AudioRecorder {
        public AudioRecorder() {
            _AudioBuffer = new BufferedWaveProvider(new WaveFormat(_SampleRate, _BitsPerSample, _NumberOfChannels));
            _AudioBuffer.BufferLength = 250 * ((_SampleRate * _BitsPerSample * _NumberOfChannels) / 8000); // 250ms voice buffer
            _AudioBuffer.DiscardOnBufferOverflow = true;

            var volumeSampleProvider = new VolumeSampleProvider(_AudioBuffer.ToSampleProvider());
            volumeSampleProvider.Volume = 2.0f;

            _AudioSink = new WaveOut();
            _AudioSink.Init(volumeSampleProvider);

            _AudioSink.Volume = 0.5f; // 0.0f - 0% volume, 0.5f - 100% volume, 1.0f - 200% volume
            _AudioSink.Play();
        }

        public void StartRecording(EventHandler<WaveInEventArgs> callback) {
            _AudioSource = new WaveInEvent();
            _AudioSource.BufferMilliseconds = 50;

            _AudioSource.WaveFormat = new WaveFormat(_SampleRate, _BitsPerSample, _NumberOfChannels);
            _AudioSource.DataAvailable += callback;

            _AudioSource.StartRecording();
        }


        public void AddDataToBuffer(byte[] data) {
            _AudioBuffer.AddSamples(data, 0, data.Length);
        }


        // Play and record
        private WaveOut _AudioSink = null;
        private WaveInEvent _AudioSource = null;

        // Play voice from this buffer
        private BufferedWaveProvider _AudioBuffer;

        // Settings
        private int _SampleRate = 48000;
        private int _BitsPerSample = 16;
        private int _NumberOfChannels = 2;
    }

}