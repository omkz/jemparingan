using UnityEngine;

public struct JemparinganHandPose
{
    public bool BowPinched;
    public bool StringPinched;
    public Vector3 BowPosition;
    public Vector3 StringPosition;
    public Vector3 AimDirection;
}

public interface IJemparinganInput
{
    bool TryReadPose(out JemparinganHandPose pose);
}
