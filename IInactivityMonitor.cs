using Microsoft.AspNetCore.Components;

namespace CircuitsInactivityMonitorApp
{
    public interface IInactivityMonitor
    {
        EventCallback OnInactivityTimeReached { get; set; }

        public EventHandler OnInactivityReached { get; set; }

        public int MaxResponseTime { get; }

        Task RegisterInactivityMonitor();
    }
}