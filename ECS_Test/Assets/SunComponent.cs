using System;
using Unity.Entities;

[Serializable]
public struct Sun : IComponentData { }

public class SunComponent : ComponentDataWrapper<Sun> { }