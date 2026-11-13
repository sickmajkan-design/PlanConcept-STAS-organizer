using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Construction.API.Json;

/// <summary>
/// Refuses NUL characters in text that arrives in a form or a query string, the way
/// <see cref="NoNulCharacterStringConverter"/> does for JSON: a 400, not a 500.
/// </summary>
/// <remarks>
/// PostgreSQL cannot store U+0000 in text, so one in a form field (an upload's title, say) became a 500 from the
/// database. JSON bodies were already covered; multipart forms and query strings bypass the JSON converter, so they
/// get the same treatment here.
/// </remarks>
public sealed class NoNulStringModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context.Metadata.ModelType != typeof(string))
        {
            return null;
        }

        var inner = new SimpleTypeModelBinder(
            typeof(string), context.Services.GetRequiredService<ILoggerFactory>());

        return new NoNulStringModelBinder(inner);
    }

    private sealed class NoNulStringModelBinder : IModelBinder
    {
        private readonly IModelBinder _inner;

        public NoNulStringModelBinder(IModelBinder inner) => _inner = inner;

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            await _inner.BindModelAsync(bindingContext);

            if (bindingContext.Result is { IsModelSet: true, Model: string text } && text.Contains('\0'))
            {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName, "Text cannot contain a NUL character.");
                bindingContext.Result = ModelBindingResult.Failed();
            }
        }
    }
}
