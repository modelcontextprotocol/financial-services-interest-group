# TODO - Code Style Alignment with MCP C# SDK

This document tracks remaining refactoring items to align the SEP-1763 C# extension code with the official MCP C# SDK coding style and patterns.

## Completed

- [x] **Add `Throw` helper class** - Use `Throw.IfNull()` for consistent null validation
  - Added `Common/Throw.cs` with `IfNull`, `IfNullOrWhiteSpace`, and `IfNegative` methods
  - Added `CallerArgumentExpressionAttribute` polyfill for netstandard2.0
  - Added `NullableAttributes` polyfill for netstandard2.0
  - Updated all files to use the new `Throw.IfNull()` pattern

## High Priority

- [ ] **Mark `InterceptorChainExecutor` as `sealed`**
  - File: `Client/InterceptorChainExecutor.cs`
  - Change `public class InterceptorChainExecutor` to `public sealed class InterceptorChainExecutor`
  - Same for `Server/ServerInterceptorChainExecutor.cs`

## Medium Priority

- [ ] **Review access modifiers for internal helper types**
  - `PayloadConverter` - Consider making `internal static class` if only used internally
  - `ReflectionMcpClientInterceptor` - Already `internal sealed`, verify this is appropriate
  - `ReflectionMcpServerInterceptor` - Already `internal sealed`, verify this is appropriate

- [ ] **Add `DebuggerDisplay` to more result types**
  - Files to update:
    - `Protocol/InterceptorChainResult.cs`
    - `Protocol/ValidationInterceptorResult.cs`
    - `Protocol/MutationInterceptorResult.cs`
    - `Protocol/ObservabilityInterceptorResult.cs`
  - Example pattern:
    ```csharp
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public sealed class InterceptorChainResult
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay => $"Status = {Status}, Results = {Results.Count}";
    }
    ```

- [ ] **Add `DebuggerDisplay` to `McpClientInterceptor`/`McpServerInterceptor`**
  - Show interceptor name and type in debugger

## Low Priority

- [ ] **Consider source-generated logging**
  - If logging is added, use `[LoggerMessage]` attributes for high-performance logging
  - Example:
    ```csharp
    [LoggerMessage(Level = LogLevel.Debug, Message = "Executing interceptor '{InterceptorName}' for event '{Event}'")]
    private partial void LogInterceptorExecution(string interceptorName, string @event);
    ```

- [ ] **Evaluate `ValueTask` vs `Task` for hot paths**
  - `InterceptorChainExecutor.ExecuteForSendingAsync` currently returns `Task<InterceptorChainResult>`
  - Consider changing to `ValueTask<InterceptorChainResult>` for reduced allocations
  - Same for `ExecuteForReceivingAsync`

- [ ] **Consider static field naming convention (`s_` prefix)**
  - SDK uses `s_` prefix for static private fields
  - Review codebase for any static fields that should follow this convention

- [ ] **Add `[EditorBrowsable(EditorBrowsableState.Never)]` for internal APIs**
  - Hide implementation details from IntelliSense
  - Example methods to consider:
    - Factory methods that are technically public but not intended for typical use

## Notes

### SDK Patterns Already Followed

The extension code already follows many SDK patterns:
- File-scoped namespaces
- Nullable reference types
- `ConfigureAwait(false)` on all awaits
- `sealed` on appropriate classes (e.g., `InterceptingMcpClient`)
- Factory methods via static `Create()` methods
- Comprehensive XML documentation
- `DebuggerDisplay` on `Interceptor` class
- `JsonPropertyName` attributes
- `ValueTask` for `InvokeAsync` methods
- Using declarations (`using var`)

### Reference

For comparison with the SDK, see:
- `/mnt/d/code/ai/mcp/csharp-sdk/src/ModelContextProtocol.Core/`
- `/mnt/d/code/ai/mcp/csharp-sdk/src/Common/Throw.cs`
