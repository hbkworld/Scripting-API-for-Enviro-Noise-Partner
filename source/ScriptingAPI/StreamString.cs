using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScriptingAPI
{
    internal class StreamString
    {
        private readonly Stream _ioStream;

        public StreamString(Stream ioStream)
        {
            _ioStream = ioStream;
        }

        public string ReadString()
        {
            var len = _ioStream.ReadByte() * 256;
            len += _ioStream.ReadByte();
            var inBuffer = new byte[len];
            _ioStream.Read(inBuffer, 0, len);

            return Encoding.Unicode.GetString(inBuffer);
        }

        public async Task<string> ReadStringAsync(CancellationToken cancellation = default(CancellationToken))
        {
            var toRead = 2;
            var read = 0;

            byte[] lenBuffer = new byte[toRead];
            while (read < toRead)
                read += await _ioStream.ReadAsync(lenBuffer, read, toRead - read, cancellation);

            var len = lenBuffer[0] * 256 + lenBuffer[1];
            var inBuffer = new byte[len];

            toRead = len;
            read = 0;
            while (read < toRead)
                read += await _ioStream.ReadAsync(inBuffer, read, toRead - read, cancellation);

            return Encoding.Unicode.GetString(inBuffer);
        }

        public int WriteString(string outString)
        {
            if (outString == null)
                outString = "";

            var outBuffer = Encoding.Unicode.GetBytes(outString);

            var len = outBuffer.Length;
            if (len > ushort.MaxValue)
                len = ushort.MaxValue;
            _ioStream.WriteByte((byte) (len / 256));
            _ioStream.WriteByte((byte) (len & 255));
            _ioStream.Write(outBuffer, 0, len);
            _ioStream.Flush();

            return outBuffer.Length + 2;
        }
    }
}