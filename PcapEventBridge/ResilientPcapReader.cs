using System;
using System.IO;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;

namespace PcapEventBridge
{
    /// <summary>
    /// A defensive, resync-capable replacement for SharpPcap's CaptureFileReaderDevice,
    /// intended for offline PCAP inspection/diagnostics only (not live capture).
    ///
    /// Root cause this addresses: a torn/partial disk write can corrupt a single PCAP
    /// record header (ts/incl_len/orig_len) in the middle of an otherwise healthy multi-GB
    /// capture file. SharpPcap's own reader has no way to recover from this — it treats
    /// the first bad record header as EOF, silently discarding every packet after it
    /// (observed: a 5GB / ~5.5M-packet file reporting only ~1,835 packets read).
    ///
    /// This reader validates each 16-byte record header (ts_sec, incl_len, orig_len must
    /// be plausible, and the *next* record — found by skipping incl_len bytes — must also
    /// look like a valid header). If validation fails, it scans forward byte-by-byte for
    /// the next position where both the corrupted record's header and the header that
    /// follows it look sane, then resumes reading from there. Every resync is counted and
    /// reported so operators know how much (if any) data around the corruption was lost.
    /// </summary>
    public sealed class ResilientPcapReader : IDisposable
    {
        private const int GlobalHeaderSize = 24;
        private const int RecordHeaderSize = 16;
        private const int MaxScanWindowBytes = 8 * 1024 * 1024; // give up resyncing after 8MB of garbage

        private const uint MagicUsecNative = 0xA1B2C3D4;
        private const uint MagicUsecSwapped = 0xD4C3B2A1;
        private const uint MagicNsecNative = 0xA1B23C4D;
        private const uint MagicNsecSwapped = 0x4D3CB2A1;

        private FileStream _fs;
        private bool _swapEndian;
        private bool _isNsec;
        private uint _snaplen;
        private uint _lastTsSec;
        private bool _haveLastTs;

        public long ResyncCount { get; private set; }
        public long BytesSkippedTotal { get; private set; }

        public void Open(string path)
        {
            _fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1 * 1024 * 1024, FileOptions.SequentialScan);

            Span<byte> header = stackalloc byte[GlobalHeaderSize];
            if (_fs.Read(header) != GlobalHeaderSize)
                throw new InvalidDataException("File too small to contain a PCAP global header.");

            uint magic = ReadU32(header, 0, swap: false);
            switch (magic)
            {
                case MagicUsecNative:
                    _swapEndian = false; _isNsec = false; break;
                case MagicUsecSwapped:
                    _swapEndian = true; _isNsec = false; break;
                case MagicNsecNative:
                    _swapEndian = false; _isNsec = true; break;
                case MagicNsecSwapped:
                    _swapEndian = true; _isNsec = true; break;
                default:
                    throw new InvalidDataException($"Not a recognized PCAP file (magic=0x{magic:X8}).");
            }

            _snaplen = ReadU32(header, 16, _swapEndian);
            if (_snaplen == 0 || _snaplen > 262144) _snaplen = 65535; // sane fallback
        }

        /// <summary>
        /// Reads the next record, transparently resyncing past any corrupted record
        /// header. Returns false only at genuine end-of-file or unrecoverable corruption
        /// (more than MaxScanWindowBytes of unparseable data).
        /// </summary>
        public bool TryReadNext(out RawCapture rawPacket, out bool wasResync, out int bytesSkipped)
        {
            rawPacket = null;
            wasResync = false;
            bytesSkipped = 0;
            byte[] rh = new byte[RecordHeaderSize];

            while (true)
            {
                long recordStart = _fs.Position;
                if (recordStart + RecordHeaderSize > _fs.Length) return false;

                if (_fs.Read(rh, 0, RecordHeaderSize) != RecordHeaderSize) return false;

                uint tsSec = ReadU32(rh, 0, _swapEndian);
                uint tsSub = ReadU32(rh, 4, _swapEndian);
                uint inclLen = ReadU32(rh, 8, _swapEndian);
                uint origLen = ReadU32(rh, 12, _swapEndian);

                if (IsPlausibleHeader(tsSec, inclLen, origLen, recordStart + RecordHeaderSize) &&
                    LooksLikeValidNextRecord(recordStart + RecordHeaderSize + inclLen, tsSec))
                {
                    var payload = new byte[inclLen];
                    if (inclLen > 0 && _fs.Read(payload, 0, (int)inclLen) != inclLen) return false;

                    _lastTsSec = tsSec;
                    _haveLastTs = true;

                    var timeval = _isNsec
                        ? new PosixTimeval((ulong)tsSec, (ulong)(tsSub / 1000UL))
                        : new PosixTimeval((ulong)tsSec, (ulong)tsSub);

                    rawPacket = new RawCapture(LinkLayers.Ethernet, timeval, payload);
                    return true;
                }

                // Corrupted record header — resync forward.
                wasResync = true;
                int skipped = ScanForwardForValidHeader(recordStart);
                if (skipped < 0) return false; // gave up — unrecoverable corruption

                bytesSkipped += skipped;
                BytesSkippedTotal += skipped;
                ResyncCount++;
                // _fs.Position already repositioned to the candidate header by ScanForwardForValidHeader.
            }
        }

        /// <summary>
        /// Scans forward byte-by-byte from just past <paramref name="recordStart"/> looking
        /// for the next offset whose 16 bytes form a plausible record header AND whose
        /// implied follow-on record also looks plausible. Leaves the stream positioned at
        /// the start of that header on success. Returns the number of bytes skipped, or -1
        /// if nothing plausible was found within MaxScanWindowBytes.
        /// </summary>
        private int ScanForwardForValidHeader(long recordStart)
        {
            long searchStart = recordStart + 1;
            long searchLimit = Math.Min(searchStart + MaxScanWindowBytes, _fs.Length - RecordHeaderSize);
            if (searchLimit < searchStart) { _fs.Position = _fs.Length; return -1; }

            const int chunkSize = 1024 * 1024;
            long pos = searchStart;

            while (pos <= searchLimit)
            {
                long thisChunkLen = Math.Min(chunkSize, searchLimit - pos + RecordHeaderSize);
                if (thisChunkLen < RecordHeaderSize) break;

                _fs.Position = pos;
                var buffer = new byte[thisChunkLen];
                int read = _fs.Read(buffer, 0, (int)thisChunkLen);
                if (read < RecordHeaderSize) break;

                for (int offset = 0; offset <= read - RecordHeaderSize; offset++)
                {
                    long candidateStart = pos + offset;
                    uint tsSec = ReadU32(buffer, offset, _swapEndian);
                    uint tsSub = ReadU32(buffer, offset + 4, _swapEndian);
                    uint inclLen = ReadU32(buffer, offset + 8, _swapEndian);
                    uint origLen = ReadU32(buffer, offset + 12, _swapEndian);

                    if (!IsPlausibleHeader(tsSec, inclLen, origLen, candidateStart + RecordHeaderSize)) continue;
                    if (!LooksLikeValidNextRecord(candidateStart + RecordHeaderSize + inclLen, tsSec)) continue;

                    _fs.Position = candidateStart;
                    return (int)(candidateStart - recordStart);
                }

                pos += read - RecordHeaderSize + 1; // overlap by RecordHeaderSize-1 so no boundary-straddling header is missed
            }

            _fs.Position = _fs.Length;
            return -1;
        }

        private bool IsPlausibleHeader(uint tsSec, uint inclLen, uint origLen, long payloadEndPos)
        {
            if (inclLen == 0 || inclLen > _snaplen) return false;
            if (origLen < inclLen || origLen > _snaplen) return false;
            if (payloadEndPos > _fs.Length) return false;

            if (_haveLastTs)
            {
                // Allow small backward jitter (clock resync / DST-like anomalies are not
                // expected mid-capture, but be lenient) and forward gaps up to 30s.
                if (tsSec + 2 < _lastTsSec) return false;
                if (tsSec > _lastTsSec + 30) return false;
            }

            return true;
        }

        private bool LooksLikeValidNextRecord(long nextPos, uint baselineTsSec)
        {
            if (nextPos == _fs.Length) return true; // clean EOF right after this record
            if (nextPos + RecordHeaderSize > _fs.Length) return false; // dangling partial bytes — suspicious

            long savedPos = _fs.Position;
            try
            {
                _fs.Position = nextPos;
                Span<byte> peek = stackalloc byte[RecordHeaderSize];
                if (_fs.Read(peek) != RecordHeaderSize) return false;

                uint tsSec = ReadU32(peek, 0, _swapEndian);
                uint inclLen = ReadU32(peek, 8, _swapEndian);
                uint origLen = ReadU32(peek, 12, _swapEndian);

                if (inclLen == 0 || inclLen > _snaplen) return false;
                if (origLen < inclLen || origLen > _snaplen) return false;
                if (nextPos + RecordHeaderSize + inclLen > _fs.Length) return false;
                if (tsSec + 2 < baselineTsSec || tsSec > baselineTsSec + 30) return false;

                return true;
            }
            finally
            {
                _fs.Position = savedPos;
            }
        }

        private static uint ReadU32(ReadOnlySpan<byte> data, int offset, bool swap)
        {
            uint v = (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
            if (!swap) return v;
            return ((v & 0x000000FFU) << 24) | ((v & 0x0000FF00U) << 8) |
                   ((v & 0x00FF0000U) >> 8) | ((v & 0xFF000000U) >> 24);
        }

        public void Dispose()
        {
            _fs?.Dispose();
            _fs = null;
        }
    }
}
