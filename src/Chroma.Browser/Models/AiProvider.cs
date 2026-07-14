namespace Chroma.Browser.Models;

public sealed record AiProvider(string Id, string Name, string Url)
{
    public static IReadOnlyList<AiProvider> BuiltIn { get; } =
    [
        new("mistral", "Mistral Le Chat", "https://chat.mistral.ai/chat"),
        new("qwen", "Qwen Chat", "https://chat.qwen.ai/"),
        new("deepseek", "DeepSeek", "https://chat.deepseek.com/"),
        new("openrouter", "OpenRouter", "https://openrouter.ai/chat")
    ];
}

