namespace FitTrack.Copilot.Abstractions;

/// <summary>
/// Marker and base contract for all Copilot plug-ins.
/// A Plugin is a logical grouping of one or more Functions (AI or deterministic).
/// Each Function has its own <see cref="FunctionDescriptor"/>.
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Returns a descriptor describing the primary or default Function
    /// this plugin provides.  If the plugin hosts multiple functions,
    /// it can expose one "default" descriptor here.
    /// </summary>
    FunctionDescriptor Describe();

    /// <summary>
    /// Invoke the plugin’s function asynchronously.
    /// Implementations should interpret <paramref name="context"/>
    /// (inputs, files, user, correlation id) and return a result object
    /// that is either a DTO or a primitive type.
    /// </summary>
    /// <param name="context">Invocation context (inputs, files, etc.)</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result object or <c>null</c>.</returns>
    Task<object?> InvokeAsync(FunctionContext context, CancellationToken ct = default);
}