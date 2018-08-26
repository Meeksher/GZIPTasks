using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GZIPTasks
{
    class Decompressor : GZIP
    {
        public Decompressor(string input, string output) : base(input, output)
        {

        }

        protected override MemoryBlock Consume(object contextMemoryBlock)
        {
            try
            {
                MemoryBlock memBlock = contextMemoryBlock as MemoryBlock;

                byte[] decompressedMemoryRaw = new byte[blockSize];

                using (MemoryStream compressedMemoryStream = new MemoryStream(memBlock.MemoryBuffer))
                {
                    using (GZipStream gzip = new GZipStream(compressedMemoryStream, CompressionMode.Decompress))
                    {
                        int read = gzip.Read(decompressedMemoryRaw, 0, decompressedMemoryRaw.Length);
                        byte[] decompressedMemory = new byte[read];
                        Array.Copy(decompressedMemoryRaw, 0, decompressedMemory, 0, read);

                        MemoryBlock decompressed = new MemoryBlock(memBlock.Index, decompressedMemory);
                        return decompressed;
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                errorFlag = true;
                throw;
            }
        }
        
        protected override void PrepareData()
        {
            try
            {
                int nextIndex = 0;

                using (FileStream fsIn = input.OpenRead())
                {
                    while (fsIn.Position != fsIn.Length && !errorFlag)
                    {
                        byte[] sizeBytes = new byte[sizeof(int)];
                        fsIn.Read(sizeBytes, 0, sizeBytes.Length);
                        int sizeMemoryBlock = BitConverter.ToInt32(sizeBytes, 0);

                        byte[] memoryRead = new byte[sizeMemoryBlock];

                        fsIn.Read(memoryRead, 0, memoryRead.Length);

                        MemoryBlock memoryProduced = new MemoryBlock(nextIndex++, memoryRead);
                        Task<MemoryBlock> task = new Task<MemoryBlock>(Consume, memoryProduced);

                        tasksForConsumer.Add(task);
                        task.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                errorFlag = true;
                throw;
            }
        }
        

        public override void Start()
        {
            try
            {
                var prepareTask = Task.Factory.StartNew(PrepareData);
                var dropTask = Task.Factory.StartNew(DropData, prepareTask);

                dropTask.Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                errorFlag = true;
            }

            if (errorFlag) Console.WriteLine("Error occured, the program has not finished correctly.");
            else Console.WriteLine("Decompressing finished.");
        }



        protected override void DropData(object producerTask)
        {
            try
            {
                using (FileStream fsOut = new FileStream(output.FullName, FileMode.Create))
                {
                    int lastBlock = 0;
                    var prepTask = producerTask as Task;

                    while (!errorFlag && !(lastBlock == tasksForConsumer.Count && prepTask.IsCompleted))
                    {
                        MemoryBlock memBlock;

                        for (; lastBlock < tasksForConsumer.Count; lastBlock++)
                        {
                            tasksForConsumer[lastBlock].Wait();
                            memBlock = tasksForConsumer[lastBlock].Result;

                            fsOut.Write(memBlock.MemoryBuffer, 0, memBlock.MemoryBuffer.Length);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                errorFlag = true;
            }
        }
    }
}
