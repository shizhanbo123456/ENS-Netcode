using System;

public abstract class Disposable:IDisposable
{
    protected bool _disposed = false;
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            ReleaseManagedMenory();
        }
        ReleaseUnmanagedMenory();
        _disposed = true;
    }
    protected virtual void ReleaseManagedMenory()
    {

    }
    protected virtual void ReleaseUnmanagedMenory()
    {

    }
}