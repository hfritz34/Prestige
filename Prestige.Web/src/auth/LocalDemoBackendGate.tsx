import { type ReactNode, useCallback, useEffect, useState } from 'react';

type BackendStatus = 'checking' | 'ready' | 'down';

const apiBaseUrl = (import.meta.env.VITE_API_ADDRESS || 'http://localhost:5249').replace(/\/$/, '');

export function LocalDemoBackendGate({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<BackendStatus>('checking');

  const checkBackend = useCallback(async () => {
    setStatus('checking');
    const controller = new AbortController();
    const timeout = window.setTimeout(() => controller.abort(), 2000);

    try {
      const response = await fetch(`${apiBaseUrl}/api/health`, {
        cache: 'no-store',
        signal: controller.signal
      });

      setStatus(response.ok ? 'ready' : 'down');
    } catch {
      setStatus('down');
    } finally {
      window.clearTimeout(timeout);
    }
  }, []);

  useEffect(() => {
    void checkBackend();
  }, [checkBackend]);

  if (status === 'ready') {
    return <>{children}</>;
  }

  return (
    <main className="min-h-screen bg-[#101820] text-[#f8f4ea] flex items-center justify-center px-6">
      <section className="max-w-xl rounded-3xl border border-[#f8f4ea]/15 bg-[#f8f4ea]/5 p-8 shadow-2xl">
        <p className="text-sm uppercase tracking-[0.28em] text-[#d7b56d]">Prestige Local Demo</p>
        <h1 className="mt-4 text-3xl font-semibold">Start the backend to continue.</h1>
        <p className="mt-4 text-[#f8f4ea]/75">
          The web app is in demo mode, but the local API is not reachable at{' '}
          <code className="rounded bg-black/30 px-1.5 py-0.5">{apiBaseUrl}</code>.
          Start the API in another terminal, then check again.
        </p>

        <pre className="mt-5 overflow-x-auto rounded-2xl bg-black/40 p-4 text-sm text-[#f8f4ea]">
{`dotnet run --project Prestige.Api/Prestige.Api.csproj --launch-profile "Local Demo"`}
        </pre>

        <button
          className="mt-6 rounded-full bg-[#d7b56d] px-5 py-2 font-semibold text-[#101820] transition hover:bg-[#f2d58a]"
          onClick={() => void checkBackend()}
          type="button"
        >
          {status === 'checking' ? 'Checking...' : 'Check again'}
        </button>
      </section>
    </main>
  );
}
