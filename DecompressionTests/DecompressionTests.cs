namespace DecompressionTests
{
    using System;
    using System.IO;
    using System.Text;
    using LZ4;
    using LZ4Decompressor;
    using NUnit.Framework;

    [TestFixture]
    public class DecompressionTests
    {
        [TestCase(512)]
        [TestCase(1024)]
        [TestCase(2048)]
        [TestCase(4096)]
        [TestCase(8192)]
        [TestCase(16384)]
        [TestCase(32768)]
        [TestCase(65536)]
        public void RandomBytesHighCompression(int blockSize)
        {
            // 64kb byte array
            var randomBytes = new byte[65536];

            // create random bytes
            var random = new Random();
            random.NextBytes(randomBytes);

            byte[] compressedBytes = CompressByteArray(LZ4StreamFlags.HighCompression, blockSize, randomBytes);
            byte[] decompressedBytes = DecompressByteArray(LZ4StreamFlags.HighCompression, blockSize, compressedBytes);

            ////File.WriteAllBytes(@"d:\test.lz4", compressedBytes);

            // check my own decompressor
            var lz4Decompressor = new LZ4Decompressor();
            var ownDecompressedBytes = lz4Decompressor.DecompressLZ4(new MemoryStream(compressedBytes));

            ////File.WriteAllText(@"d:\test.txt", asciiText);

            Assert.That(decompressedBytes, Is.EqualTo(randomBytes));
            Assert.That(ownDecompressedBytes, Is.EqualTo(randomBytes), "Own decompressor file mismatch");

            Console.WriteLine("Blocksize: {0}, Compressed size: {1}, Compression ratio: {2}", blockSize, compressedBytes.Length, (float)compressedBytes.Length / (float)randomBytes.Length);
        }

        [TestCase(512)]
        [TestCase(1024)]
        [TestCase(2048)]
        [TestCase(4096)]
        [TestCase(8192)]
        [TestCase(16384)]
        [TestCase(32768)]
        [TestCase(65536)]
        public void LoremIpsumHighCompression(int blockSize)
        {
            var loremIpsumBytes = Encoding.ASCII.GetBytes(LZ4DecompressionTests.Properties.Resources.LoremIpsum);

            byte[] compressedBytes = CompressByteArray(LZ4StreamFlags.HighCompression, blockSize, loremIpsumBytes);
            byte[] decompressedBytes = DecompressByteArray(LZ4StreamFlags.HighCompression, blockSize, compressedBytes);

            ////File.WriteAllBytes(@"d:\test.lz4", compressedBytes);

            // check my own decompressor
            var lz4Decompressor = new LZ4Decompressor();
            var ownDecompressedBytes = lz4Decompressor.DecompressLZ4(new MemoryStream(compressedBytes));
            var asciiText = Encoding.ASCII.GetString(ownDecompressedBytes);

            ////File.WriteAllText(@"d:\test.txt", asciiText);

            Assert.That(decompressedBytes, Is.EqualTo(loremIpsumBytes));
            Assert.That(ownDecompressedBytes, Is.EqualTo(loremIpsumBytes), "Own decompressor file mismatch");

            Console.WriteLine("Blocksize: {0}, Compressed size: {1}, Compression ratio: {2}", blockSize, compressedBytes.Length, (float)compressedBytes.Length / (float)loremIpsumBytes.Length);
        }

        private static byte[] CompressByteArray(LZ4StreamFlags streamFlags, int blockSize, byte[] bytesToCompress)
        {
            // compress with LZ4 reference compressor
            var outputStream = new MemoryStream();
            var lz4compressor = new LZ4Stream(outputStream, LZ4StreamMode.Compress, streamFlags, blockSize);
            lz4compressor.Write(bytesToCompress, 0, bytesToCompress.Length);
            lz4compressor.Flush();

            var compressedBytes = outputStream.ToArray();

            return compressedBytes;
        }

        private static byte[] DecompressByteArray(LZ4StreamFlags streamFlags, int blockSize, byte[] bytesToDecompress)
        {
            // decompress with LZ4 reference compressor
            var inputStream = new MemoryStream(bytesToDecompress);
            var lz4compressor = new LZ4Stream(inputStream, LZ4StreamMode.Decompress);

            var decompressedStream = new MemoryStream();

            var buffer = new byte[512];
            int length = 0;
            do
            {
                length = lz4compressor.Read(buffer, 0, buffer.Length);
                decompressedStream.Write(buffer, 0, length);
            }
            while (length > 0);

            return decompressedStream.ToArray();
        }
    }
}
