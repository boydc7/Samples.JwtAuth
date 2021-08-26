using System.Threading;

namespace Samples.JwtAuth.Api.Services
{
    public class ApplicationShutdownCancellationSource
    {
        private ApplicationShutdownCancellationSource() { }

        public static readonly ApplicationShutdownCancellationSource Instance = new ApplicationShutdownCancellationSource();

        public void TryCancel()
        {
            try
            {
                CancellationTokenSource?.Cancel();
            }
            catch
            {
                // Ignore - shuting down
            }
        }

        public CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();
        public CancellationToken Token => CancellationTokenSource.Token;
    }
}
