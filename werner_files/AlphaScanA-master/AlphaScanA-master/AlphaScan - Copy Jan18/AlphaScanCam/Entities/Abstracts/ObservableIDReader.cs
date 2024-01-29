using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlphaScanCam.Entities;

namespace AlphaScanCam.Entities
{
    /// <summary>
    /// An abstraction of a device that can receive ID data, and message other classes using the Obvserver Design Patern 
    /// </summary>
    /// <typeparam name="IDResponse"></typeparam>
    public abstract class ObservableIDReader<IDResponse> : IObservable<IDResponse>
    {
       protected static List<IObserver<IDResponse>> observers = new List<IObserver<IDResponse>>();

        public IDisposable Subscribe(IObserver<IDResponse> observer)
        {
            if (!observers.Contains(observer)) observers.Add(observer);
            return new Unsubscriber<IDResponse>(observers, observer);
        }
        
       
    }
    internal class Unsubscriber<IDResponse> : IDisposable
    {
        private List<IObserver<IDResponse>> _observers;
        private IObserver<IDResponse> _observer;
        internal Unsubscriber(List<IObserver<IDResponse>> observers, IObserver<IDResponse> observer)
        {
            _observer = observer;
            _observers = observers;
        }
        public void Dispose()
        {
            if (_observers.Contains(_observer))
                _observers.Remove(_observer);

        }
    }

}
