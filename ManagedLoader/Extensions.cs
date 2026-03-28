using System.Numerics;
using UndertaleModLib.Models;

namespace ManagedLoader;

public static class Extensions
{
    // Because for whatever reason there isn't a generic version of the LINQ Sum method
    public static TSum Sum<TSource, TSum>(this IEnumerable<TSource> enumerable, Func<TSource, TSum> selector) where TSum : struct, IAdditionOperators<TSum, TSum, TSum>
    {
        var acc = default(TSum);
        return enumerable.Aggregate(acc, (current, el) => current + selector(el));
    }

    public static bool IsOfType(this UndertaleInstruction instruction, UndertaleInstruction.InstructionType type)
    {
        return UndertaleInstruction.GetInstructionType(instruction.Kind) == type;
    }
}