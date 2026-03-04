// Plan (pseudocode):
// 1. Provide a small helper `Skip` class in the same test namespace so existing calls like
//    `Skip.If(condition, "reason")` compile.
// 2. Implement `If(bool condition, string reason)` to throw xUnit's skip exception when
//    condition is true. This causes xUnit to mark the test as skipped instead of failed.
// 3. Keep the helper internal and minimal to avoid changing other code.
// 4. Put detailed plan as comments here and then the implementation below.
//
// Detailed pseudocode:
// - namespace: EtabSharp.Test (matches tests' namespace)
// - using: Xunit (for SkipException)
// - define internal static class Skip
//   - public static void If(bool condition, string reason)
//     - if condition is true:
//         - throw new SkipException(reason)
//     - else: return (no-op)
// - This file will allow existing tests that call `Skip.If(...)` to compile and run,
//   and tests will be reported as skipped when the condition is true.

using Xunit;

namespace EtabSharp.Test
{
    /// <summary>
    /// Minimal helper to support calls like `Skip.If(condition, "reason")` in tests.
    /// Calls Assert.Skip() so the runner reports the test as Skipped.
    /// </summary>
    internal static class Skip
    {
        public static void If(bool condition, string reason)
        {
            if (condition)
            {
                Assert.Skip(reason);
            }
        }
    }
}