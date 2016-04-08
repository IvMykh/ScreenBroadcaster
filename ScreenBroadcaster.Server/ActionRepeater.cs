using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScreenBroadcaster.Server
{
    public class ActionRepeater
    {
        // Instance members.
        private int     _counter;
        private Timer   _timer;
        
        public Action   Callback            { get; private set; }
        public int      GenerationFrequency { get; private set; }
        public bool     IsDisposed          { get; private set; }

        public ActionRepeater(Action callback, int generationFrequency)
        {
            _counter            = 0;
            Callback            = callback;
            GenerationFrequency = generationFrequency;
            IsDisposed          = true;
        }

        public void Start()
        {
            if (_timer == null)
            {
                _timer = new Timer(new TimerCallback((obj) => Callback()), null, 0, GenerationFrequency);
                IsDisposed = false;
            }
        }

        public void Stop()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                Stop();
            }
        }
    }
}
