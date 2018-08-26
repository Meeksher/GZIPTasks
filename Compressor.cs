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
    class Compressor : GZIP
    {
        public Compressor(string input, string output) : base(input, output)
        {

        }

        protected override MemoryBlock Consume(object contextMemoryBlock)
        {
            try
            {
                MemoryBlock memBlock = contextMemoryBlock as MemoryBlock;

                using (MemoryStream compressedMemoryStream = new MemoryStream())
                {
                    using (GZipStream gzip = new GZipStream(compressedMemoryStream, CompressionMode.Compress))
                    {
                        gzip.Write(memBlock.MemoryBuffer, 0, memBlock.MemoryBuffer.Length);
                    }

                    MemoryBlock compressed = new MemoryBlock(memBlock.Index, compressedMemoryStream.ToArray());
                    return compressed;
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
                        byte[] memoryRead;

                        if (fsIn.Length - fsIn.Position < blockSize)
                            memoryRead = new byte[fsIn.Length - fsIn.Position];
                        else
                            memoryRead = new byte[blockSize];

                        fsIn.Read(memoryRead, 0, memoryRead.Length);


                        MemoryBlock memoryProduced = new MemoryBlock(nextIndex++, memoryRead);
                        var task = Task.Factory.StartNew(Consume, memoryProduced);
                        tasksForConsumer.Add(task);

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
        
        protected override void DropData(object producerTask)
        {
            try
            {
                using (FileStream fsOut = new FileStream(output.FullName + ".gz", FileMode.OpenOrCreate))
                {
                    int lastBlock = 0;
                    var prepTask = producerTask as Task;

                    while (!errorFlag && !(lastBlock == tasksForConsumer.Count && prepTask.IsCompleted))
                    {
                        /* write memory blocks as struct:
                         *   int sizeOfBlock;
                         *   byte[] memory;
                        */
                        MemoryBlock memBlock;

                        for (; lastBlock < tasksForConsumer.Count; lastBlock++)
                        {
                            tasksForConsumer[lastBlock].Wait();
                            memBlock = tasksForConsumer[lastBlock].Result;
                            
                            var bytesLength = BitConverter.GetBytes(memBlock.MemoryBuffer.Length);
                            fsOut.Write(bytesLength, 0, bytesLength.Length);

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
            else Console.WriteLine("Compressing finished.");
        }

    }
}
