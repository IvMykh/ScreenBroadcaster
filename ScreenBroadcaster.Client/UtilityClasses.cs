using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using System.Threading.Tasks;

namespace ScreenBroadcaster.Client
{
    public class NumberGenerator
    {
        private int _counter = 0;

        public int GenerateNextInt()
        {
            return ++_counter;
        }

        public float GenerateNextHalfInt()
        {
            return _counter++ + 0.5f;
        }
    }

    public class NewPictureEventArgs
        : EventArgs
    {
        public int NewPicture { get; set; }
    }

    public class IntStriker
        : IDisposable
    {
        // Instance members.
        private int     _counter;
        private Timer   _timer;
        
        public int      GenerationFrequency { get; private set; }
        public bool     IsDisposed          { get; private set; }

        public IntStriker(int generationFrequency)
        {
            _counter            = 0;
            IsDisposed          = true;
            GenerationFrequency = generationFrequency;
        }

        public void Start()
        {
            if (_timer == null)
            {
                _timer = new Timer(new TimerCallback((obj) =>
                    {
                        OnNewPictureGenerated(new NewPictureEventArgs { NewPicture = ++_counter });
                    }), null, 0, GenerationFrequency);
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

        public event EventHandler<NewPictureEventArgs> NewPictureGenerated;

        protected virtual void OnNewPictureGenerated(NewPictureEventArgs e)
        {
            if (NewPictureGenerated != null)
            {
                NewPictureGenerated(this, e);
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
