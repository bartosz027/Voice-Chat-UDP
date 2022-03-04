using System;
using System.Linq;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;

using OpusDotNet;

namespace Client1 {

    class AudioRecorder {
        public AudioRecorder() {
            // Opus
            _Encoder = new OpusEncoder(Application.VoIP, _SampleRate, _NumberOfChannels);
            _EncodedByteArray = new byte[_BufferMilliseconds * (_SampleRate * _BitsPerSample * _NumberOfChannels / 8000)];

            _Decoder = new OpusDecoder(_SampleRate, _NumberOfChannels);
            _DecodedByteArray = new byte[_BufferMilliseconds * (_SampleRate * _BitsPerSample * _NumberOfChannels / 8000)];

            // NAudio
            _AudioBuffer = new BufferedWaveProvider(new WaveFormat(_SampleRate, _BitsPerSample, _NumberOfChannels));
            _AudioBuffer.BufferLength = 250 * (_SampleRate * _BitsPerSample * _NumberOfChannels / 8000); // 250ms voice buffer
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
            _AudioSource.BufferMilliseconds = _BufferMilliseconds;

            _AudioSource.WaveFormat = new WaveFormat(_SampleRate, _BitsPerSample, _NumberOfChannels);
            _AudioSource.DataAvailable += callback;

            _AudioSource.StartRecording();
        }


        public byte[] EncodeAudio(byte[] data) {
            int encoded_length = _Encoder.Encode(data, data.Length, _EncodedByteArray, _EncodedByteArray.Length);
            return _EncodedByteArray.Take(encoded_length).ToArray();
        }

        public byte[] DecodeAudio(byte[] data) {
            _Decoder.Decode(data, data.Length, _DecodedByteArray, _DecodedByteArray.Length);
            return _DecodedByteArray;
        }


        public void PlayVoice(byte[] data) {
            _AudioBuffer.AddSamples(data, 0, data.Length);
        }


        // Opus
        private OpusEncoder _Encoder;
        private byte[] _EncodedByteArray;

        private OpusDecoder _Decoder;
        private byte[] _DecodedByteArray;

        // Record voice
        private WaveInEvent _AudioSource;
        private int _BufferMilliseconds = 40;

        // Play voice
        private WaveOut _AudioSink;
        private BufferedWaveProvider _AudioBuffer;

        // Settings
        private int _SampleRate = 48000;
        private int _BitsPerSample = 16;
        private int _NumberOfChannels = 1;
    }

}