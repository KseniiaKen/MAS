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
        public Result result;

        public ResultsMessage(Guid senderId, int suspectableCount, int recoveredCount, int infectiousCount, int funeralCount, int deadCount, int exposedCount, int time) : base(senderId, MessageType.Results)
        {
            this.result = new Result(suspectableCount, recoveredCount, exposedCount, infectiousCount, funeralCount, deadCount, time);
        }

        public ResultsMessage(Guid senderId, Result result) : base(senderId, MessageType.Results)
        {
            this.result = result;
        }
    }
}
