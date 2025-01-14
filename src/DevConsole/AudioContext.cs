using NAudio.Wave;

public sealed class AudioContext : IDisposable
{
    private readonly WaveInEvent _waveIn;
    private readonly WaveOutEvent _waveOut;
    private readonly BufferedWaveProvider _waveOutProvider;
    public event EventHandler<byte[]>? DataAvailable;
    public AudioContext()
    {
        WaveFormat waveFormat = new(16000, 16, 1);
        _waveIn = new(){
            WaveFormat = waveFormat,
            BufferMilliseconds = 20
        };
        _waveOutProvider = new(waveFormat)
        {
            BufferLength = waveFormat.AverageBytesPerSecond * 120
        };
        _waveOut = new();
        _waveOut.Init(_waveOutProvider);

        _waveIn.DataAvailable += (s, e) => {
            if (DataAvailable != null)
            {
                byte[] buffer = new byte[e.BytesRecorded];
                Buffer.BlockCopy(e.Buffer, 0, buffer, 0, e.BytesRecorded);
                DataAvailable.Invoke(this, buffer);
            }
        };
    }
    public void EnqueueBytes(byte[] buffer)
    {
        _waveOutProvider.AddSamples(buffer, 0, buffer.Length);
    }
    public void Start()
    {
        _waveOutProvider.ClearBuffer();
        _waveOut.Play();
        _waveIn.StartRecording();
    }
    public void ClearBuffer()
    {
        _waveOutProvider.ClearBuffer();
    }
    public void Stop()
    {
        _waveIn.StopRecording();
        _waveOut.Stop();
        _waveOutProvider.ClearBuffer();
    }
    public void Dispose()
    {
        Stop();
        _waveIn.Dispose();
        _waveOut.Dispose();
    }
}