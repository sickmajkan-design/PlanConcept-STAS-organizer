namespace Construction.Application.Common.Interfaces;

/// <summary>
/// The tools this caller is allowed to use, and the only way to run one.
/// </summary>
/// <remarks>
/// <para>
/// Implemented in the API layer rather than here, because the thing it has to
/// enforce lives there: <c>[Authorize(Policy = …)]</c> is on controller
/// actions, and there is no authorization behaviour in the MediatR pipeline.
/// A tool that sent a query straight to <c>IMediator</c> would therefore skip
/// the role check completely — the implementation puts it back.
/// </para>
/// <para>
/// <see cref="DescribeAsync"/> returns only what the caller may run, so the
/// model is never shown a tool it would be refused. <see cref="InvokeAsync"/>
/// checks again anyway: the first is a convenience, the second is the control.
/// </para>
/// </remarks>
public interface IAssistantToolset
{
    Task<IReadOnlyList<AssistantToolDescriptor>> DescribeAsync(
        CancellationToken cancellationToken = default);

    Task<AssistantToolOutcome> InvokeAsync(
        AssistantToolCall call,
        CancellationToken cancellationToken = default);
}
