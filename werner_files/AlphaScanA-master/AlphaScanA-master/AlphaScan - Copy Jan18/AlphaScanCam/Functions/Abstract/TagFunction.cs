using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AlphaScanCam.Entities;

namespace AlphaScanCam.Functions
{
    /// <summary>
    /// Using the Observer Design Pattern, a tag function subscribes to a IDreader to receive and process the ID.
    /// The Display Usercontrol  subscribes to the Function in order to recieve events that need to be displayed
    /// </summary>
    public abstract class TagFunction : IObserver<IDResponse>, IObservable<IDResponse>

    {
        
        
        public List<IObserver<IDResponse>> AlertObservers = new List<IObserver<IDResponse>>();
        public abstract void OnCompleted();
        public abstract void OnError(Exception error);
        public abstract void OnNext(IDResponse value);


        public IDisposable Subscribe(IObserver<IDResponse> observer)
        {
            AlertObservers.Add(observer);
            return new UnsubscribeTagFunction<IDResponse>(AlertObservers, observer);
        }

        private UserControl _DisplayControl;

        public UserControl DisplayControl { get => _DisplayControl; set => _DisplayControl = value; }
        private UserControl _SettingsControl;

        public UserControl SettingsControl { get => _SettingsControl; set => _SettingsControl = value; }
    }
    public class UnsubscribeTagFunction<IDResponse> :IDisposable
    {
        List<IObserver<IDResponse>> _functions;
        IObserver<IDResponse> _function;
        public UnsubscribeTagFunction(List<IObserver<IDResponse>> FunctionsList, IObserver<IDResponse> Function)
        {
            _functions = FunctionsList;
            _function = Function;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                  if (_functions.Contains(_function))
                    {
                        _functions.Remove(_function);
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~UnsubscribeTagFunction() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }


   
}
