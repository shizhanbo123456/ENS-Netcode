using System;

public interface IrpcParam<T>
{
    public bool Serialize(T value, byte[] result, ref int indexStart);
    public T Deserialize(byte[] data, ref int indexStart, int invalidIndex);
}