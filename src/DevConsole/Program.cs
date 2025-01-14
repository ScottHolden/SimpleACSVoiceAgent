using System.Net.WebSockets;
using System.Text;

using ClientWebSocket ws = new();
using AudioContext audio = new();
audio.DataAvailable += async (s, e) =>
{
    string json = MediaData.OutboundAudio(e).ToJson();
    byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
    await ws.SendAsync(jsonBytes, WebSocketMessageType.Text, endOfMessage: true, default);
};

await ws.ConnectAsync(new Uri("ws://localhost:5191/api/audio"), default);
audio.Start();

Console.WriteLine("Connected!");

ArraySegment<byte> buffer = new byte[4096];
StringBuilder dataBuilder = new();
while (true)
{
    var result = await ws.ReceiveAsync(buffer, default);
    string data = Encoding.UTF8.GetString(buffer.Slice(0, result.Count));
    dataBuilder.Append(data);
    if (!result.EndOfMessage) continue;

    var media = MediaData.Parse(dataBuilder.ToString());
    dataBuilder.Clear();
    if (media.Kind == MediaKind.AudioData && media.AudioData != null)
    {
        audio.EnqueueBytes(media.AudioData.Data);
    }
    else if (media.Kind == MediaKind.StopAudio)
    {
        audio.ClearBuffer();
    }
}