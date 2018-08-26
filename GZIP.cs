using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GZIPTasks
{
    public abstract class GZIP
    {
        protected FileInfo input, output;
        protected int blockSize = 10000000; 
        protected int countThreads = Environment.ProcessorCount;
        protected List<Task<MemoryBlock>> tasksForConsumer = new List<Task<MemoryBlock>>();

        protected bool errorFlag = false;

        public abstract void Start();

        protected abstract MemoryBlock Consume(object threadContext);
        protected abstract void PrepareData();
        protected abstract void DropData(object producerTask);

        public GZIP(string input, string output)
        {
            this.input = new FileInfo(input);
            this.output = new FileInfo(output);
        }
    }
}
