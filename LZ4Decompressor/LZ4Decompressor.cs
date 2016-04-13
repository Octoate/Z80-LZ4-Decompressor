namespace LZ4Decompressor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Example Decompressor to verfiy my understanding of the LZ4 compressor / decompressor
    /// </summary>
    public class LZ4Decompressor
    {
        private List<byte> decompressedData = new List<byte>(65536);

        public int ChunkType { get; private set; }

        public long CompressedLength { get; private set; }

        public long OriginalLength { get; private set; }

        public byte[] DecompressLZ4(Stream compressedData)
        {
            var binaryReader = new BinaryReader(compressedData);

            // contains the full decompressed data (e.g. chunk + chunk + chunk...)
            long expectedLength = 0;

            // read chunk header
            while (this.ReadChunkHeader(binaryReader))
            {
                expectedLength += this.OriginalLength;

                // check if it is an uncompressed chunk
                if ((this.ChunkType & 0x01) == 0x00)
                {
                    // uncompressed -> just copy the data to the decompression buffer
                    var uncompressedBytes = new byte[this.OriginalLength];
                    binaryReader.Read(uncompressedBytes, 0, uncompressedBytes.Length);
                    this.decompressedData.AddRange(uncompressedBytes);
                }
                else
                {
                    // compressed chunk
                    do
                    {
                        // read token
                        int token = binaryReader.ReadByte();
                        if (token == -1)
                        {
                            // end of stream
                            break;
                        }

                        int literalLength = (token & 0xF0) >> 4;
                        int matchingLength = token & 0x0F;

                        // check for additional length information
                        if (literalLength == 0x0F)
                        {
                            // repeat length information until the next byte is not 0xFF
                            int additionalLength = 0;
                            do
                            {
                                additionalLength = binaryReader.ReadByte();
                                literalLength += additionalLength;
                            }
                            while (additionalLength == 0xFF);
                        }

                        // read the literal...
                        var literal = new byte[literalLength];
                        binaryReader.Read(literal, 0, literal.Length);

                        // ...and copy it to the output byte list
                        this.decompressedData.AddRange(literal);

                        // check if we reached the end of a data chunk
                        if (this.decompressedData.Count >= (int)expectedLength)
                        {
                            // skip the rest and try to read the next data chunk
                            break;
                        }

                        // get matching offset
                        int matchingOffset = binaryReader.ReadUInt16();

                        // calculate matching length
                        if (matchingLength == 0x0F)
                        {
                            // repeat length information until the next byte is not 0xFF
                            int additionalMatchingLength = 0;
                            do
                            {
                                additionalMatchingLength = binaryReader.ReadByte();
                                matchingLength += additionalMatchingLength;
                            }
                            while (additionalMatchingLength == 0xFF);
                        }

                        // always add '4' to the matching length (minimum size)
                        matchingLength += 4;

                        string tempResult = Encoding.ASCII.GetString(this.decompressedData.ToArray());

                        // copy the matching bytes
                        do
                        {
                            var currentDataCount = this.decompressedData.Count;
                            List<byte> matchingBytes;
                            if (currentDataCount - matchingOffset + matchingLength > currentDataCount)
                            {
                                matchingBytes = this.decompressedData.GetRange(currentDataCount - matchingOffset, matchingOffset);
                                matchingLength -= matchingOffset;
                                matchingOffset += matchingBytes.Count;
                            }
                            else
                            {
                                matchingBytes = this.decompressedData.GetRange(currentDataCount - matchingOffset, matchingLength);
                                matchingLength = 0;
                            }

                            this.decompressedData.AddRange(matchingBytes);
                        }
                        while (matchingLength > 0);
                    } while (true);
                }
            }

            return this.decompressedData.ToArray();
        }

        private bool ReadChunkHeader(BinaryReader binaryReader)
        {
            bool ok;

            try
            {
                // 1st byte: ChunkType (should be 0x03 -> Compressed | HighCompression)
                this.ChunkType = binaryReader.ReadByte();

                // first "varint" decoding - original length
                this.OriginalLength = this.ReadVarInt(binaryReader);

                if (this.IsChunkCompressed(this.ChunkType))
                {
                    // second "varint" decoding - compressed length
                    this.CompressedLength = this.ReadVarInt(binaryReader);

                    Console.WriteLine("Compressed chunk data: chunkType = 0x{0:X2}, original length = 0x{1:X8}, compressed length = {2:X8}, ratio = {3}", this.ChunkType, this.OriginalLength, this.CompressedLength, (float)this.CompressedLength / (float)this.OriginalLength);
                }
                else
                {
                    Console.WriteLine("Uncompressed chunk data: chunkType = 0x{0:X2}, length = 0x{1:X8}", this.ChunkType, this.OriginalLength);
                }

                ok = true;
            }
            catch (EndOfStreamException)
            {
                ok = false;
            }

            return ok;
        }

        private bool IsChunkCompressed(int chunkType)
        {
            return (chunkType & 0x01) == 0x01;
        }

        private long ReadVarInt(BinaryReader binaryReader)
        {
            long result = 0x00;
            byte readByte = 0x00;
            var counter = 0x00;

            do
            {
                readByte = binaryReader.ReadByte();
                result = result + ((long)(readByte & 0x7F) << counter);
                counter += 7;
            }
            while ((readByte & 0x80) > 0x00);

            return result;
        }
    }
}
