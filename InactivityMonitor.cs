using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Timers;
using Timer = System.Timers.Timer;

namespace CircuitsInactivityMonitorApp
{
    public class InactivityMonitor : IInactivityMonitor
    {
        private readonly Timer _timer;
        private readonly IJSRuntime? _jsRuntime;
        private readonly int _maxIdleAlertResponseTime;

        public InactivityMonitor(IConfiguration configuration, IJSRuntime? jsRuntime)
        {
            _jsRuntime = jsRuntime;

            var inactivityMaxTime = configuration.GetValue<int>("MaxIdleTimeAllowed");
            _maxIdleAlertResponseTime = configuration.GetValue<int>("MaxIdleAlertResponseTime");

            _timer = new Timer(inactivityMaxTime);
            _timer.Elapsed += ShowInactivityAlert;
            _timer.AutoReset = false;
        }

        public EventCallback OnInactivityTimeReached { get; set; }

        public EventHandler OnInactivityReached { get; set; }

        public int MaxResponseTime => _maxIdleAlertResponseTime;

        public async Task RegisterInactivityMonitor()
        {
            if (_jsRuntime != null)
            {
                await _jsRuntime.InvokeVoidAsync("timeOutCall", DotNetObjectReference.Create(this));
            }
        }

        [JSInvokable]
        public void ResetTimerInterval()
        {
            _timer.Stop();
            _timer.Start();
        }

        private void ShowInactivityAlert(Object? source, ElapsedEventArgs e)
        {
            if (OnInactivityReached.Target != null)
            {
                OnInactivityReached.Invoke(source, e);
            }

            if (OnInactivityTimeReached.HasDelegate)
            {
                Task.Run(async () => await OnInactivityTimeReached.InvokeAsync());
            }
        }
    }
}
