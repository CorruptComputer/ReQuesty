﻿namespace ReQuesty.Builder.CodeDOM;

/// <summary>
/// The base class for exclusion types. (one of the properties at a time)
/// </summary>
public class CodeIntersectionType : CodeComposedTypeBase, ICloneable
{
    public override object Clone()
    {
        return new CodeIntersectionType().BaseClone<CodeIntersectionType>(this);
    }
}
