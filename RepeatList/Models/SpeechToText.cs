using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepeatList.Models
{
    public interface ISpeechToText
    {
        Task<string> RecognizeSpeechAsync();
    }
}
