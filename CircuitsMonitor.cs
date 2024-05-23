namespace CircuitsInactivityMonitorApp
{
    using Microsoft.AspNetCore.Components.Server.Circuits;
    using Microsoft.Extensions.Options;
    using Timer = System.Timers.Timer;

    public sealed class IdleCircuitHandler : CircuitHandler, IDisposable
    {
        readonly Timer timer;
        readonly ILogger logger;

        public IdleCircuitHandler(IOptions<IdleCircuitOptions> options, ILogger<IdleCircuitHandler> logger)
        {
            timer = new Timer();
            timer.Interval = options.Value.IdleTimeout.TotalMilliseconds;
            timer.AutoReset = false;
            timer.Elapsed += CircuitIdle;
            this.logger = logger;
        }

        private void CircuitIdle(object? sender, System.Timers.ElapsedEventArgs e)
        {
            logger.LogWarning(nameof(CircuitIdle));
        }

        public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            return base.OnCircuitOpenedAsync(circuit, cancellationToken);
        }

        public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(Func<CircuitInboundActivityContext, Task> next)
        {
            return context =>
            {
                timer.Stop();
                timer.Start();
                return next(context);
            };
        }

        public void Dispose()
        {
            timer.Dispose();
        }
    }

    public class IdleCircuitOptions
    {
        public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(5);
    }

    public static class IdleCircuitHandlerServiceCollectionExtensions
    {
        public static IServiceCollection AddIdleCircuitHandler(this IServiceCollection services, Action<IdleCircuitOptions> configureOptions)
        {
            services.Configure(configureOptions);
            services.AddIdleCircuitHandler();
            return services;
        }

        public static IServiceCollection AddIdleCircuitHandler(this IServiceCollection services)
        {
            services.AddScoped<CircuitHandler, IdleCircuitHandler>();
            return services;
        }
    }

    public class CircuitServicesAccessor
    {
        static readonly AsyncLocal<IServiceProvider> blazorServices = new();

        public IServiceProvider? Services
        {
            get => blazorServices.Value;
            set => blazorServices.Value = value;
        }
    }

    public class ServicesAccessorCircuitHandler : CircuitHandler
    {
        readonly IServiceProvider services;
        readonly CircuitServicesAccessor circuitServicesAccessor;

        public ServicesAccessorCircuitHandler(IServiceProvider services, CircuitServicesAccessor servicesAccessor)
        {
            this.services = services;
            this.circuitServicesAccessor = servicesAccessor;
        }

        public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(Func<CircuitInboundActivityContext, Task> next)
        {
            return async context =>
            {
                circuitServicesAccessor.Services = services;
                await next(context);
                circuitServicesAccessor.Services = null;
            };
        }
    }

    public static class CircuitServicesServiceCollectionExtensions
    {
        public static IServiceCollection AddCircuitServicesAccessor(this IServiceCollection services)
        {
            services.AddSingleton<CircuitServicesAccessor>();
            services.AddScoped<CircuitHandler, ServicesAccessorCircuitHandler>();
            return services;
        }
    }

}
