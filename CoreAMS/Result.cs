using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreAMS
{
    [Serializable]
    public class Result
    {
        public int suspectableCount;
        public int recoveredCount;
        public int exposedCount;
        public int infectiousCount;
        public int funeralCount;
        public int deadCount;
        public int executionTime;
        public int realTime;

        public Result(int suspectableCount, int recoveredCount, int exposedCount, int infectiousCount, int funeralCount, int deadCount, int executionTime, int realTime)
        {
            this.suspectableCount = suspectableCount;
            this.recoveredCount = recoveredCount;
            this.exposedCount = exposedCount;
            this.infectiousCount = infectiousCount;
            this.funeralCount = funeralCount;
            this.deadCount = deadCount;
            this.executionTime = executionTime;
            this.realTime = realTime;
        }

        public Result(int suspectableCount, int recoveredCount, int exposedCount, int infectiousCount, int funeralCount, int deadCount, int executionTime)
        {
            this.suspectableCount = suspectableCount;
            this.recoveredCount = recoveredCount;
            this.exposedCount = exposedCount;
            this.infectiousCount = infectiousCount;
            this.funeralCount = funeralCount;
            this.deadCount = deadCount;
            this.executionTime = executionTime;
            this.realTime = 0;
        }
    }
}
