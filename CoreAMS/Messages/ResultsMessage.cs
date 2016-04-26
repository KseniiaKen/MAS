using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreAMS.Messages
{
    [Serializable]
    public class ResultsMessage: Message
    {
        public int suspectableCount;
        public int recoveredCount;
        public int infectiousCount;
        public int funeralCount;
        public int deadCount;
        public int exposedCount;
        public int time;

        public ResultsMessage(Guid senderId, int suspectableCount, int recoveredCount, int infectiousCount, int funeralCount, int deadCount, int exposedCount, int time) : base(senderId, MessageType.Results)
        {
            this.suspectableCount = suspectableCount;
            this.recoveredCount = recoveredCount;
            this.infectiousCount = infectiousCount;
            this.funeralCount = funeralCount;
            this.deadCount = deadCount;
            this.exposedCount = exposedCount;
            this.time = time;
        }
    }
}
